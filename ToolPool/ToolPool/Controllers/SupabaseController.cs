using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Stripe.Forwarding;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json.Serialization;
using ToolPool.Models;
using ToolPool.Services;
using System.Text.Json;

namespace ToolPool.Controllers
{
    [Route("api")]
    [ApiController]
    public class SupabaseController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly SupabaseDemoService _supabase;
        private readonly SendbirdService _sendbird;
        private readonly IHttpClientFactory _httpClientFactory;

        public SupabaseController(IConfiguration config, SupabaseDemoService supabase, SendbirdService sendbird, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _supabase = supabase;
            _sendbird = sendbird;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public string Test()
        {
            Console.WriteLine("success: \"test\"");
            return "good";
        }
        
        [HttpGet("reverseGeocode/{lat}/{lng}")]
        public async Task<String> ReverseGeocode(string lat, string lng)
        {
            var client = _httpClientFactory.CreateClient();

            //geocode api: gets detailed location info from latitude, longitude
            var geocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={_config["Google:Maps"]}";

            var geocodeReq = new HttpRequestMessage(HttpMethod.Get, geocodeUrl);
            var geocodeResp = await client.SendAsync(geocodeReq);
            var geocodeContent = await geocodeResp.Content.ReadAsStringAsync();
            var geocodeJson = JsonDocument.Parse(geocodeContent);
            var f = geocodeJson.RootElement.GetProperty("results")[0];
            var address = f.GetProperty("formatted_address").GetString();

            Console.WriteLine($"reverseGeocode/{lat}/{lng}: {address}");
            return address;
        }

        [HttpGet("suggestLocations/{location}")]
        public async Task<List<AddressPair>> GetLocation(string location)
        {
            var client = _httpClientFactory.CreateClient();
            List<AddressPair> tuples = new List<AddressPair>();

            //'places:autocomplete' api: gets placeId and text suggestions from input
            var autocompleteUrl = $"https://places.googleapis.com/v1/places:autocomplete?input={location}";

            var autocompleteReq = new HttpRequestMessage(HttpMethod.Post, autocompleteUrl);
            autocompleteReq.Headers.Add("X-Goog-Api-Key", _config["Google:Maps"]);
            autocompleteReq.Headers.Add("X-Goog-FieldMask", "suggestions.placePrediction.placeId,suggestions.placePrediction.text.text");

            //bullshit converting to enumerable
            var autocompleteResp = await client.SendAsync(autocompleteReq);
            var autocompleteContent = await autocompleteResp.Content.ReadAsStringAsync();
            var autocompleteJson = JsonDocument.Parse(autocompleteContent);
            //cringe nesting
            try
            {
                foreach (var p in autocompleteJson.RootElement.GetProperty("suggestions").EnumerateArray())
                {
                    var placePrediction = p.GetProperty("placePrediction");
                    //Console.WriteLine(placePrediction.GetProperty("text").GetProperty("text").GetString());
                    //Console.WriteLine(placePrediction.GetProperty("placeId").GetString());
                    tuples.Add(new AddressPair
                    {
                        Address = placePrediction.GetProperty("text").GetProperty("text").GetString(),
                        PlaceId = placePrediction.GetProperty("placeId").GetString()
                    });
                }
            }
            catch (Exception)
            {
            }
            Console.WriteLine($"suggestLocations/{location}: {tuples.Count} suggestions");
            return tuples;
        }

        [HttpGet("getCoordinates/{placeId}")]
        public async Task<Location> GetCoordinates(string placeId)
        {
            var client = _httpClientFactory.CreateClient();

            //places api: gets latitude, longitude from placeId
            var placesUrl = $"https://places.googleapis.com/v1/places/{placeId}";

            var placesReq = new HttpRequestMessage(HttpMethod.Get, placesUrl);
            placesReq.Headers.Add("X-Goog-Api-Key", _config["Google:Maps"]);
            placesReq.Headers.Add("X-Goog-FieldMask", "location");

            var placesResp = await client.SendAsync(placesReq);
            var placesContent = await placesResp.Content.ReadAsStringAsync();
            var placesJson = JsonDocument.Parse(placesContent);
            var locationCoords = placesJson.RootElement.GetProperty("location");
            return new Location
            {
                Latitude = locationCoords.GetProperty("latitude").GetDouble(),
                Longitude = locationCoords.GetProperty("longitude").GetDouble(),
            };
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
            categories = categories.Distinct().ToList();
            Console.WriteLine($"\"categories\": {categories.Count}");
            return categories;
        }

        [HttpGet("neighborhoods")]
        public async Task<List<String>> Neighborhoods()
        {
            List<String> neighborhoods = new List<String>();
            foreach (var c in (await _supabase.GetNeighborhoods()))
            {
                neighborhoods.Add(c.Neighborhood);
            }
            neighborhoods = neighborhoods.Distinct().ToList();
            Console.WriteLine($"\"neighborhoods\": {neighborhoods.Count}");
            return neighborhoods;
        }

        ////key = city, value = list of neighborhoods
        //[HttpGet("cityNeighborhoods")]
        //public async Task<Dictionary<String, List<String>>> CityNeighborhoods()
        //{
        //    Dictionary<String, List<String>> cityNeighborhoods = new Dictionary<String, List<String>>();
        //    foreach(var t in (await _supabase.GetCityNeighborhoods()))
        //    {
        //        var c = t.city;
        //        var n = t.neighborhood;
        //        if(cityNeighborhoods.ContainsKey(c))
        //        {
        //            cityNeighborhoods[c].Add(n);
        //        }
        //        else
        //        {
        //            cityNeighborhoods[c] = new List<String>{n};
        //        }
        //    }
        //    Console.WriteLine(cityNeighborhoods.Count > 0 ? "success: \"cityNeighborhoods\"" : "failure: \"cityNeighborhoods\"");
        //    return cityNeighborhoods;
        //}

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
            // Resolve renter from authenticated session
            var renterIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(renterIdClaim) || !Guid.TryParse(renterIdClaim, out var renterId))
                return Unauthorized(new { error = "Not authenticated" });

            var renter = await _supabase.GetUserByIdAsync(renterId);
            if (renter is null)
                return Unauthorized(new { error = "User account not found" });

            // Resolve tool and owner
            var tool = await _supabase.GetToolByIdAsync(request.ToolId);
            if (tool is null)
                return NotFound(new { error = "Tool not found" });

            if (tool.OwnerId is null)
                return BadRequest(new { error = "This tool has no owner and cannot accept interest" });

            if (tool.OwnerId == renter.Id)
                return BadRequest(new { error = "You cannot rent your own tool" });

            var owner = await _supabase.GetUserByIdAsync(tool.OwnerId.Value);
            if (owner is null)
                return BadRequest(new { error = "Tool owner account not found" });

            // Ensure both users have sendbird_user_id (backfill from Users.id if null)
            if (string.IsNullOrEmpty(renter.SendbirdUserId))
            {
                renter.SendbirdUserId = renter.Id.ToString();
                await _supabase.UpdateUserSendbirdIdAsync(renter.Id, renter.SendbirdUserId);
            }
            if (string.IsNullOrEmpty(owner.SendbirdUserId))
            {
                owner.SendbirdUserId = owner.Id.ToString();
                await _supabase.UpdateUserSendbirdIdAsync(owner.Id, owner.SendbirdUserId);
            }

            // Create or reuse Sendbird channel using sendbird_user_id values
            string? channelUrl = null;
            try
            {
                var channel = await _sendbird.CreateGroupChannelAsync(
                    renter.SendbirdUserId, owner.SendbirdUserId, request.ToolName);
                channelUrl = channel.ChannelUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sendbird error (non-fatal): {ex.Message}");
            }

            // renter_id stored as Users.id.ToString() for schema compatibility (column is text)
            var interest = new InterestSubmission
            {
                ToolId = request.ToolId,
                ToolName = request.ToolName,
                RenterId = renter.Id.ToString(),
                OwnerId = tool.OwnerId,
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

        [HttpGet("my-interests")]
        public async Task<ActionResult<List<MyInterestItem>>> GetMyInterests()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
                return Unauthorized(new { error = "Not authenticated" });

            var asRenter = await _supabase.GetInterestsByRenterAsync(userId.ToString());
            var asOwner = await _supabase.GetInterestsByOwnerAsync(userId);

            var results = new List<MyInterestItem>();

            foreach (var i in asRenter)
            {
                string counterpartName = "Owner";
                if (i.OwnerId.HasValue)
                {
                    var owner = await _supabase.GetUserByIdAsync(i.OwnerId.Value);
                    counterpartName = owner?.Username ?? owner?.Email ?? "Owner";
                }
                results.Add(new MyInterestItem
                {
                    Id = i.Id,
                    ToolName = i.ToolName,
                    ChannelUrl = i.ChannelUrl,
                    Status = i.Status,
                    CounterpartName = counterpartName,
                    Role = "renter",
                    CreatedAt = i.CreatedAt
                });
            }

            foreach (var i in asOwner)
            {
                // Skip duplicates (if user is both renter and owner — shouldn't happen but guard)
                if (results.Any(r => r.Id == i.Id)) continue;

                string counterpartName = "Renter";
                if (Guid.TryParse(i.RenterId, out var renterGuid))
                {
                    var renter = await _supabase.GetUserByIdAsync(renterGuid);
                    counterpartName = renter?.Username ?? renter?.Email ?? "Renter";
                }
                results.Add(new MyInterestItem
                {
                    Id = i.Id,
                    ToolName = i.ToolName,
                    ChannelUrl = i.ChannelUrl,
                    Status = i.Status,
                    CounterpartName = counterpartName,
                    Role = "owner",
                    CreatedAt = i.CreatedAt
                });
            }

            return Ok(results.OrderByDescending(r => r.CreatedAt).ToList());
        }

        public record CreateToolRequest(string Name, string Description, decimal Price);

        public class InterestRequest
        {
            [JsonPropertyName("tool_id")]
            public Guid ToolId { get; set; }

            [JsonPropertyName("tool_name")]
            public string ToolName { get; set; } = "";

            public string Message { get; set; } = "";

            [JsonPropertyName("start_date")]
            public string? StartDate { get; set; }

            [JsonPropertyName("end_date")]
            public string? EndDate { get; set; }
        }

        public class InterestResponse
        {
            public bool Success { get; set; }

            [JsonPropertyName("channel_url")]
            public string? ChannelUrl { get; set; }

            [JsonPropertyName("interest_id")]
            public Guid? InterestId { get; set; }
        }

        public class MyInterestItem
        {
            public Guid Id { get; set; }

            [JsonPropertyName("tool_name")]
            public string ToolName { get; set; } = "";

            [JsonPropertyName("channel_url")]
            public string? ChannelUrl { get; set; }

            public string Status { get; set; } = "pending";

            [JsonPropertyName("counterpart_name")]
            public string CounterpartName { get; set; } = "";

            public string Role { get; set; } = "";

            [JsonPropertyName("created_at")]
            public DateTimeOffset? CreatedAt { get; set; }
        }
    }
}
