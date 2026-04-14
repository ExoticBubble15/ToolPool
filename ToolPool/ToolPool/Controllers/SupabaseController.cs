using Microsoft.AspNetCore.Mvc;
using ToolPool.Models;
using ToolPool.Services;

namespace ToolPool.Controllers
{
    [Route("api")]
    [ApiController]
    public class SupabaseController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly SupabaseDemoService _supabase;
        private readonly SendbirdService _sendbird;

        public SupabaseController(IConfiguration config, SupabaseDemoService supabase, SendbirdService sendbird)
        {
            _config = config;
            _supabase = supabase;
            _sendbird = sendbird;
        }

        [HttpGet]
        public string Test()
        {
            Console.WriteLine("success: \"test\"");
            return "good";
        }

        [HttpGet("getSecret/{key}")]
        public string GetSecret(string key)
        {
            string? val = _config[key];
            Console.WriteLine(val != null ? $"success: \"getSecret/{key}\"" : $"failure: \"getSecret/{key}\"");
            return val ?? string.Empty;
        }

        [HttpGet("Tools")]
        public async Task<ActionResult<List<Tool>>> GetTools()
        {
            var items = await _supabase.GetToolsAsync();
            return Ok(items);
        }

        [HttpGet("Tools/{id}")]
        public async Task<ActionResult<Tool>> GetToolById(Guid id)
        {
            var tool = await _supabase.GetToolByIdAsync(id);
            if (tool is null) return NotFound();
            return Ok(tool);
        }

        [HttpPost("submissions")]
        public async Task<IActionResult> InsertSubmission([FromBody] CreateToolRequest request)
        {
            await _supabase.InsertSubmissionAsync(request.Name, request.Description, request.Price);
            return Ok();
        }

        [HttpPost("Tools")]
        public async Task<ActionResult<Tool>> InsertTool([FromBody] CreateToolRequest request)
        {
            var item = await _supabase.InsertToolAsync(request.Name, request.Description, request.Price);
            return Ok(item);
        }

        [HttpDelete("Tools/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _supabase.DeleteToolAsync(id);
            return NoContent();
        }

        [HttpPost("interests")]
        public async Task<ActionResult<InterestResponse>> SubmitInterest([FromBody] InterestRequest request)
        {
            var channel = await _sendbird.CreateGroupChannelAsync(
                request.RenterId, request.OwnerId, request.ToolName);

            var interest = new InterestSubmission
            {
                ToolId = request.ToolId,
                ToolName = request.ToolName,
                RenterId = request.RenterId,
                OwnerId = request.OwnerId,
                Message = request.Message,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ChannelUrl = channel.ChannelUrl,
                Status = "pending"
            };

            var saved = await _supabase.InsertInterestAsync(interest);

            return Ok(new InterestResponse
            {
                Success = true,
                ChannelUrl = channel.ChannelUrl,
                InterestId = saved.Id
            });
        }

        [HttpPost("Tools/seed")]
        public async Task<IActionResult> SeedTools()
        {
            await _supabase.SeedToolsAsync();
            return Ok("Seeded");
        }

        public record CreateToolRequest(string Name, string Description, decimal Price);

        public class InterestRequest
        {
            public Guid ToolId { get; set; }
            public string ToolName { get; set; } = "";
            public string RenterId { get; set; } = "";
            public string OwnerId { get; set; } = "";
            public string Message { get; set; } = "";
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
        }

        public class InterestResponse
        {
            public bool Success { get; set; }
            public string? ChannelUrl { get; set; }
            public Guid? InterestId { get; set; }
        }
    }
}
