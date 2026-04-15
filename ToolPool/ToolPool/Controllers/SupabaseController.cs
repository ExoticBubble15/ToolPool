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
        private readonly UserService _userService;
        public SupabaseController(IConfiguration config, SupabaseDemoService supabase, UserService userService)
        {
            _config = config;
            _supabase = supabase;
            _userService = userService;
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

        [HttpDelete("demo-items/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _supabase.DeleteDemoItemAsync(id);
            return NoContent();
        }

        [HttpPost("users/register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _userService.RegisterUserAsync(request);

            if (!result.IsValid)
            {
                return BadRequest(new
                {
                    error = result.ErrorMessage
                });
            }

            return Ok(result);
        }
    }
}
