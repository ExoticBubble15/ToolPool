using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToolPool.Models;
using ToolPool.Services;

namespace ToolPool.Controllers
{
    [Route("api")]
    [ApiController]
    public class SupabaseController : ControllerBase
    {
        //simple test if api is working
        //".../api/test"
        [HttpGet]
        public string Test()
        {
            Console.WriteLine("success: \"test\"");
            return "good";
        }

        //fetches local user secret by key
        //".../api/getSecret/{key}"
        private readonly IConfiguration _config;
        private readonly SupabaseDemoService _supabase;
        public SupabaseController(IConfiguration config, SupabaseDemoService supabase)
        {
            _config = config;
            _supabase = supabase;
        }
        [HttpGet("getSecret/{key}")]
        public string GetSecret(string key)
        {
            string? val = _config[key];
            if(val != null)
            {
                Console.WriteLine($"success: \"getSecret/{key}\"");
            }
            else
            {
                Console.WriteLine($"failure: \"getSecret/{key}\"");
            }
            return val ?? string.Empty;
        }

        [HttpGet("Tools")]
        public async Task<ActionResult<List<Tool>>> GetTools()
        {
            var items = await _supabase.GetToolsAsync();
            return Ok(items);
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

        public record CreateToolRequest(string Name, string Description, decimal Price);

        [HttpDelete("Tools/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _supabase.DeleteToolAsync(id);
            return NoContent();
        }
    }
}
