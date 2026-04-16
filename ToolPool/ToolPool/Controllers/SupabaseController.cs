using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
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
            if (request == null)
                return BadRequest("Request body is null");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required");

            if (string.IsNullOrWhiteSpace(request.OwnerId))
                return BadRequest("OwnerId is required");

            if (string.IsNullOrWhiteSpace(request.OwnerName))
                return BadRequest("OwnerName is required");

            await _supabase.InsertSubmissionAsync(
                request.Name,
                request.Description,
                request.Price,
                request.OwnerId,
                request.Category,
                request.OwnerName,
                request.Neighborhood,
                request.ImageUrl
            );

            return Ok();
        }

        [HttpPost("demo-items")]
        public async Task<ActionResult<DemoItem>> InsertDemoItem([FromBody] CreateDemoItemRequest request)
        {
            var item = await _supabase.InsertDemoItemAsync(request.Name, request.Description, request.Price);
            return Ok(item);
        }

        public class CreateDemoItemRequest
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }

            public string OwnerId { get; set; }
            public string Category { get; set; }

            [JsonPropertyName("owner_name")]
            public string OwnerName { get; set; }

            public string Neighborhood { get; set; }

            [JsonPropertyName("imageUrl")]
            public string ImageUrl { get; set; }
        }
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
