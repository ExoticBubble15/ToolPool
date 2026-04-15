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

        [HttpGet("categories")]
        public async Task<List<String>> Categories()
        {
            List<String> categories = new List<String>();
            foreach(var c in (await _supabase.GetCategories()))
            {
                categories.Add(c.Category);
            }
            Console.WriteLine(categories.Count > 0 ? "success: \"categories\"" : "failure: \"categories\"");
            return categories;
        }

        //key = city, value = list of neighborhoods
        [HttpGet("cityNeighborhoods")]
        public async Task<Dictionary<String, List<String>>> CityNeighborhoods()
        {
            Dictionary<String, List<String>> cityNeighborhoods = new Dictionary<String, List<String>>();
            foreach(var t in (await _supabase.GetCityNeighborhoods()))
            {
                var c = t.city;
                var n = t.neighborhood;
                if(cityNeighborhoods.ContainsKey(c))
                {
                    cityNeighborhoods[c].Add(n);
                }
                else
                {
                    cityNeighborhoods[c] = new List<String>{n};
                }
            }
            Console.WriteLine(cityNeighborhoods.Count > 0 ? "success: \"cityNeighborhoods\"" : "failure: \"cityNeighborhoods\"");
            return cityNeighborhoods;
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
            string? channelUrl = null;

            try
            {
                var ownerId = request.OwnerId?.ToString() ?? "owner-unknown";
                var channel = await _sendbird.CreateGroupChannelAsync(
                    request.RenterId, ownerId, request.ToolName);
                channelUrl = channel.ChannelUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sendbird error (non-fatal): {ex.Message}");
            }

            var interest = new InterestSubmission
            {
                ToolId = request.ToolId,
                ToolName = request.ToolName,
                RenterId = request.RenterId,
                OwnerId = request.OwnerId,
                Message = request.Message,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ChannelUrl = channelUrl,
                Status = "pending"
            };

            try
            {
                var saved = await _supabase.InsertInterestAsync(interest);
                return Ok(new InterestResponse
                {
                    Success = true,
                    ChannelUrl = channelUrl,
                    InterestId = saved.Id
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Supabase interest insert error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
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
            public Guid? OwnerId { get; set; }
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
