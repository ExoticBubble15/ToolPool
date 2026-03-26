using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToolPool.Models;
using ToolPool.Services;

namespace ToolPool
{
    [Route("api")]
    [ApiController]
    public class api : ControllerBase
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
        public api(IConfiguration config, SupabaseDemoService supabase)
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

        [HttpGet("demo-items")]
        public async Task<ActionResult<List<DemoItem>>> GetDemoItems()
        {
            var items = await _supabase.GetDemoItemsAsync();
            return Ok(items);
        }

        [HttpPost("submissions")]
        public async Task<IActionResult> InsertSubmission([FromBody] CreateDemoItemRequest request)
        {
            await _supabase.InsertSubmissionAsync(request.Name, request.Description, request.Price);
            return Ok();
        }

        [HttpPost("demo-items")]
        public async Task<ActionResult<DemoItem>> InsertDemoItem([FromBody] CreateDemoItemRequest request)
        {
            var item = await _supabase.InsertDemoItemAsync(request.Name, request.Description, request.Price);
            return Ok(item);
        }

        public record CreateDemoItemRequest(string Name, string Description, decimal Price);
    }
}
