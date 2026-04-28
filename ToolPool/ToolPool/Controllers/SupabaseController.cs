using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Stripe.Forwarding;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json.Serialization;
using ToolPool.Models;
using ToolPool.Services;
using System.Text.Json;
using ToolPool.Client.Models;

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
        private readonly UserService _userService;
        private readonly ILogger<SupabaseController> _logger;
        HttpClient client;

        public SupabaseController(IConfiguration config, SupabaseDemoService supabase, SendbirdService sendbird, IHttpClientFactory httpClientFactory, UserService userService, ILogger<SupabaseController> logger)
        {
            _config = config;
            _supabase = supabase;
            _sendbird = sendbird;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _logger = logger;
            client = _httpClientFactory.CreateClient();
        }

        [HttpGet]
        public string Test()
        {
            Console.WriteLine("success: \"test\"");
            return "good";
        }

        [HttpGet("markerDetails")]
        public async Task<List<MarkerDetails>> markerDetails()
        {
            var res = await _supabase.GetMarkerDetails();
            Console.WriteLine($"\"markerDetails\": {res.Count}");
            return res;
        }

        [HttpGet("neighborhoodTriples")]
        public async Task<List<NeighborhoodTriple>> neighborhoodTriples()
        {
            var res = await _supabase.GetNeighborhoodTriples();
            Console.WriteLine($"\"neighborhoodCoords\": {res.Count}");
            return res;
        }
        
        [HttpGet("reverseGeocode/{lat}/{lng}")]
        public async Task<String> ReverseGeocode(string lat, string lng)
        {
            var address = await ReverseGeocodeExactAddressAsync(lat, lng);

            Console.WriteLine($"reverseGeocode/{lat}/{lng}: {address}");
            return address;
        }

        [HttpGet("suggestLocations/{location}")]
        public async Task<List<Models.AddressPair>> GetLocation(string location)
        {
            //var client = _httpClientFactory.CreateClient();
            List<Models.AddressPair> tuples = new List<Models.AddressPair>();

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
                    tuples.Add(new Models.AddressPair
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
        public async Task<Models.Location> GetCoordinates(string placeId)
        {
            //var client = _httpClientFactory.CreateClient();

            //places api: gets latitude, longitude from placeId
            var placesUrl = $"https://places.googleapis.com/v1/places/{placeId}";

            var placesReq = new HttpRequestMessage(HttpMethod.Get, placesUrl);
            placesReq.Headers.Add("X-Goog-Api-Key", _config["Google:Maps"]);
            placesReq.Headers.Add("X-Goog-FieldMask", "location");

            var placesResp = await client.SendAsync(placesReq);
            var placesContent = await placesResp.Content.ReadAsStringAsync();
            var placesJson = JsonDocument.Parse(placesContent);
            var locationCoords = placesJson.RootElement.GetProperty("location");
            return new Models.Location
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
            //always put 'other' at the end
            if(categories.Contains("Other"))
            {
                categories.Remove("Other");
                categories.Add("Other");
            }
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
        public async Task<ActionResult<List<Models.Tool>>> GetTools()
        {
            var items = await _supabase.GetToolsAsync();
            return Ok(items);
        }

        [HttpGet("Tools/{id}")]
        public async Task<ActionResult<Models.Tool>> GetToolById(Guid id)
        {
            var tool = await _supabase.GetToolByIdAsync(id);
            if (tool is null) return NotFound();

            if (tool.OwnerId is Guid ownerId)
            {
                try
                {
                    var rating = await _supabase.GetOwnerRatingAsync(ownerId);
                    if (rating is not null)
                    {
                        tool.OwnerAvgRating = rating.AvgRating;
                        tool.OwnerTotalRatings = rating.TotalRatings;
                    }
                }
                catch
                {
                    // Rating enrichment is best-effort — leave fields null on failure.
                }
            }

            return Ok(tool);
        }

        [HttpPost("Tools")]
        public async Task<ActionResult<Models.Tool>> InsertTool([FromBody] Models.Tool t)
        {
            var item = await _supabase.InsertToolAsync(t);
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
            // #region agent log
            var _dbgLogPath = "/Users/yitaowang/Downloads/ToolPool/.cursor/debug-dbdcbc.log";
            void _dbgLog(string msg, object? data = null) { try { System.IO.File.AppendAllText(_dbgLogPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "dbdcbc", location = "SupabaseController:SubmitInterest", message = msg, data, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { } }
            // #endregion
            // #region agent log
            _dbgLog("entry", new { requestToolId = request.ToolId, requestToolName = request.ToolName, hasUser = User.Identity?.IsAuthenticated });
            // #endregion

            // Resolve renter from authenticated session
            var renterIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // #region agent log
            _dbgLog("auth-check", new { renterIdClaim, isAuthenticated = User.Identity?.IsAuthenticated });
            // #endregion
            if (string.IsNullOrEmpty(renterIdClaim) || !Guid.TryParse(renterIdClaim, out var renterId))
            {
                // #region agent log
                _dbgLog("H1-rejected-unauth", new { renterIdClaim });
                // #endregion
                return Unauthorized(new { error = "Not authenticated" });
            }

            var renter = await _supabase.GetUserByIdAsync(renterId);
            if (renter is null)
            {
                // #region agent log
                _dbgLog("renter-not-found", new { renterId });
                // #endregion
                return Unauthorized(new { error = "User account not found" });
            }

            // Resolve tool and owner
            var tool = await _supabase.GetToolByIdAsync(request.ToolId);
            if (tool is null)
            {
                // #region agent log
                _dbgLog("H5-tool-not-found", new { requestToolId = request.ToolId });
                // #endregion
                return NotFound(new { error = "Tool not found" });
            }

            if (tool.OwnerId is null)
            {
                // #region agent log
                _dbgLog("H2-no-owner", new { toolId = tool.Id, toolName = tool.Name });
                // #endregion
                return BadRequest(new { error = "This tool has no owner and cannot accept interest" });
            }

            if (tool.OwnerId == renter.Id)
            {
                // #region agent log
                _dbgLog("H3-self-rent", new { renterId = renter.Id, ownerId = tool.OwnerId, toolName = tool.Name });
                // #endregion
                return BadRequest(new { error = "You cannot rent your own tool" });
            }

            var owner = await _supabase.GetUserByIdAsync(tool.OwnerId.Value);
            if (owner is null)
            {
                // #region agent log
                _dbgLog("H4-owner-not-found", new { ownerId = tool.OwnerId });
                // #endregion
                return BadRequest(new { error = "Tool owner account not found" });
            }

            // Ensure both users have a real Sendbird account (provision + persist id).
            // Hard-fail with 502 if Sendbird provisioning fails — never persist an
            // interest row whose sendbird_user_id refers to a non-existent account.
            string renterSbId, ownerSbId;
            try
            {
                renterSbId = await EnsureSendbirdUserAsync(renter);
                ownerSbId = await EnsureSendbirdUserAsync(owner);
            }
            catch (SendbirdException ex)
            {
                _logger.LogError(ex, "Sendbird user provisioning failed in SubmitInterest (renter={RenterId}, owner={OwnerId})", renter.Id, owner.Id);
                _dbgLog("sendbird-fail", new { stage = "provision", status = (int)ex.Status, body = ex.ResponseBody });
                return StatusCode(502, new { error = "Chat provisioning failed" });
            }

            // App-level dedupe on (renter, tool). Sendbird is no longer is_distinct,
            // so the interest row is the source of truth for "one chat per rental".
            InterestSubmission? existing;
            try
            {
                existing = await _supabase.GetInterestByRenterAndToolAsync(renter.Id.ToString(), tool.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Existing-interest lookup failed for renter={RenterId} tool={ToolId}", renter.Id, tool.Id);
                return StatusCode(500, new { error = "Interest lookup failed" });
            }

            if (existing is not null)
            {
                string reusedChannelUrl;
                try
                {
                    if (!string.IsNullOrEmpty(existing.ChannelUrl)
                        && await _sendbird.VerifyChannelMembersAsync(existing.ChannelUrl, renterSbId, ownerSbId))
                    {
                        reusedChannelUrl = existing.ChannelUrl;
                    }
                    else
                    {
                        var refreshed = await _sendbird.CreateGroupChannelAsync(
                            renterSbId, ownerSbId, request.ToolName, interestId: existing.Id.ToString());
                        reusedChannelUrl = refreshed.ChannelUrl;
                        await _supabase.UpdateInterestChannelUrlAsync(existing.Id, reusedChannelUrl);
                    }
                }
                catch (SendbirdException ex)
                {
                    _logger.LogError(ex, "Sendbird channel verify/create failed for existing interest {InterestId}", existing.Id);
                    _dbgLog("sendbird-fail", new { stage = "existing-interest", interestId = existing.Id, status = (int)ex.Status });
                    return StatusCode(502, new { error = "Chat provisioning failed" });
                }

                return Ok(new InterestResponse
                {
                    Success = true,
                    ChannelUrl = reusedChannelUrl,
                    InterestId = existing.Id
                });
            }

            // No existing interest — create a fresh per-interest channel, then insert the row.
            string channelUrl;
            try
            {
                var channel = await _sendbird.CreateGroupChannelAsync(
                    renterSbId, ownerSbId, request.ToolName);
                channelUrl = channel.ChannelUrl;
            }
            catch (SendbirdException ex)
            {
                _logger.LogError(ex, "Sendbird channel creation failed in SubmitInterest (renter={RenterId}, tool={ToolId})", renter.Id, tool.Id);
                _dbgLog("sendbird-fail", new { stage = "new-interest", status = (int)ex.Status, body = ex.ResponseBody });
                return StatusCode(502, new { error = "Chat provisioning failed" });
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
                _logger.LogError(ex, "Interest insert failed after successful Sendbird provisioning (channelUrl={ChannelUrl})", channelUrl);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<string> EnsureSendbirdUserAsync(Models.AppUser user)
        {
            var desired = string.IsNullOrEmpty(user.SendbirdUserId) ? user.Id.ToString() : user.SendbirdUserId;
            var provisioned = await _sendbird.CreateOrGetUserAsync(desired, user.Username ?? desired);
            if (user.SendbirdUserId != provisioned)
            {
                await _supabase.UpdateUserSendbirdIdAsync(user.Id, provisioned);
                user.SendbirdUserId = provisioned;
            }
            return provisioned;
        }

        [HttpGet("my-interests")]
        public async Task<ActionResult<List<MyInterestItem>>> GetMyInterests([FromQuery] Guid userId)
        {
            // Validate input
            if (userId == Guid.Empty)
                return BadRequest(new { error = "Missing userId" });

            // Fetch interests
            var asRenter = await _supabase.GetInterestsByRenterAsync(userId.ToString());
            var asOwner = await _supabase.GetInterestsByOwnerAsync(userId);

            var allItems = new List<(InterestSubmission Sub, string Role)>();

            foreach (var i in asRenter)
                allItems.Add((i, "renter"));

            foreach (var i in asOwner)
            {
                // Guard against duplicates
                if (allItems.Any(x => x.Sub.Id == i.Id)) continue;
                allItems.Add((i, "owner"));
            }

            // Deduplicate by channel_url (or fallback to Id)
            var grouped = allItems
                .GroupBy(x => string.IsNullOrEmpty(x.Sub.ChannelUrl)
                    ? x.Sub.Id.ToString()
                    : x.Sub.ChannelUrl)
                .Select(g => g.OrderByDescending(x => x.Sub.CreatedAt).First())
                .ToList();

            // Cache users to avoid repeated DB calls
            var userCache = new Dictionary<Guid, Models.AppUser?>();

            var results = new List<MyInterestItem>();

            foreach (var (sub, role) in grouped)
            {
                string counterpartName;

                if (role == "renter" && sub.OwnerId.HasValue)
                {
                    if (!userCache.TryGetValue(sub.OwnerId.Value, out var owner))
                    {
                        owner = await _supabase.GetUserByIdAsync(sub.OwnerId.Value);
                        userCache[sub.OwnerId.Value] = owner;
                    }

                    counterpartName = owner?.Username ?? owner?.Email ?? "Owner";
                }
                else if (role == "owner" && Guid.TryParse(sub.RenterId, out var renterGuid))
                {
                    if (!userCache.TryGetValue(renterGuid, out var renter))
                    {
                        renter = await _supabase.GetUserByIdAsync(renterGuid);
                        userCache[renterGuid] = renter;
                    }

                    counterpartName = renter?.Username ?? renter?.Email ?? "Renter";
                }
                else
                {
                    counterpartName = role == "renter" ? "Owner" : "Renter";
                }

                results.Add(new MyInterestItem
                {
                    Id = sub.Id,
                    ToolName = sub.ToolName,
                    ChannelUrl = sub.ChannelUrl,
                    Status = sub.Status,
                    CounterpartName = counterpartName,
                    Role = role,
                    CreatedAt = sub.CreatedAt
                });
            }

            return Ok(results.OrderByDescending(r => r.CreatedAt).ToList());
        }

        [HttpGet("interests/{interestId:guid}/pickup-address")]
        public async Task<ActionResult<PickupAddressResponse>> GetPickupAddress(Guid interestId)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { error = "Not authenticated" });

            var interest = await _supabase.GetInterestByIdAsync(interestId);
            if (interest is null)
                return NotFound(new { error = "Interest not found" });

            var isOwner = interest.OwnerId == userId.Value;
            var isRenter = interest.RenterId == userId.Value.ToString();
            if (!isOwner && !isRenter)
                return Forbid();

            try
            {
                var response = await BuildPickupAddressResponseAsync(interest, userId.Value);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("interests/{interestId:guid}/reveal-address")]
        public async Task<ActionResult<PickupAddressResponse>> RevealPickupAddress(Guid interestId)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { error = "Not authenticated" });

            var interest = await _supabase.GetInterestByIdAsync(interestId);
            if (interest is null)
                return NotFound(new { error = "Interest not found" });

            if (interest.OwnerId != userId.Value)
                return Forbid();

            var normalizedStatus = NormalizeInterestStatus(interest.Status);
            if (!CanRevealAddress(normalizedStatus) && !IsAddressRevealedStatus(normalizedStatus))
            {
                return BadRequest(new { error = "This booking cannot reveal an address right now." });
            }

            if (!IsAddressRevealedStatus(normalizedStatus))
            {
                await _supabase.UpdateInterestStatusAsync(interestId, "revealed");
                interest.Status = "revealed";
            }

            try
            {
                var response = await BuildPickupAddressResponseAsync(interest, userId.Value);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("interests/{interestId:guid}/start-handoff")]
        public async Task<ActionResult<PickupAddressResponse>> StartHandoff(Guid interestId)
        {
            return await TransitionInterestAsync(
                interestId,
                allowedActor: "owner",
                requiredStatus: "revealed",
                nextStatus: "handoff_requested",
                invalidStatusMessage: "You can only start handoff after the address is revealed.");
        }

        [HttpPost("interests/{interestId:guid}/confirm-pickup")]
        public async Task<ActionResult<PickupAddressResponse>> ConfirmPickup(Guid interestId)
        {
            return await TransitionInterestAsync(
                interestId,
                allowedActor: "renter",
                requiredStatus: "handoff_requested",
                nextStatus: "handed_off",
                invalidStatusMessage: "Pickup can only be confirmed after the owner starts handoff.");
        }

        [HttpPost("interests/{interestId:guid}/request-return")]
        public async Task<ActionResult<PickupAddressResponse>> RequestReturn(Guid interestId)
        {
            return await TransitionInterestAsync(
                interestId,
                allowedActor: "renter",
                requiredStatus: "handed_off",
                nextStatus: "return_requested",
                invalidStatusMessage: "You can request return only after pickup is confirmed.");
        }

        [HttpPost("interests/{interestId:guid}/confirm-return")]
        public async Task<ActionResult<PickupAddressResponse>> ConfirmReturn(Guid interestId)
        {
            return await TransitionInterestAsync(
                interestId,
                allowedActor: "owner",
                requiredStatus: "return_requested",
                nextStatus: "completed",
                invalidStatusMessage: "Return can only be confirmed after the renter requests return.");
        }

        public class OwnerRatingRequest
        {
            [JsonPropertyName("score")]
            public int Score { get; set; }
        }

        [HttpPost("interests/{interestId:guid}/rating")]
        public async Task<ActionResult<PickupAddressResponse>> SubmitOwnerRating(Guid interestId, [FromBody] OwnerRatingRequest body)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { error = "Not authenticated" });

            if (body is null || body.Score < 1 || body.Score > 5)
                return BadRequest(new { error = "Score must be between 1 and 5." });

            var interest = await _supabase.GetInterestByIdAsync(interestId);
            if (interest is null)
                return NotFound(new { error = "Interest not found" });

            if (interest.RenterId != userId.Value.ToString())
                return Forbid();

            if (NormalizeInterestStatus(interest.Status) != "completed")
                return BadRequest(new { error = "You can only rate after the rental is completed." });

            if (interest.OwnerId is not Guid ownerId)
                return BadRequest(new { error = "This rental has no owner to rate." });

            await _supabase.UpsertRatingAsync(interestId, userId.Value, ownerId, body.Score);
            await _supabase.RecomputeUserAggregateAsync(ownerId);

            try
            {
                var response = await BuildPickupAddressResponseAsync(interest, userId.Value);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private Guid? GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }

        private static string NormalizeInterestStatus(string? status) =>
            string.IsNullOrWhiteSpace(status) ? "pending" : status.Trim().ToLowerInvariant();

        private static bool IsAddressRevealedStatus(string? status) =>
            NormalizeInterestStatus(status) is "revealed" or "handoff_requested" or "handed_off" or "return_requested" or "completed";

        private static bool CanRevealAddress(string? status) =>
            NormalizeInterestStatus(status) is "pending" or "confirmed";

        private async Task<ActionResult<PickupAddressResponse>> TransitionInterestAsync(
            Guid interestId,
            string allowedActor,
            string requiredStatus,
            string nextStatus,
            string invalidStatusMessage)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { error = "Not authenticated" });

            var interest = await _supabase.GetInterestByIdAsync(interestId);
            if (interest is null)
                return NotFound(new { error = "Interest not found" });

            var isOwner = interest.OwnerId == userId.Value;
            var isRenter = interest.RenterId == userId.Value.ToString();
            if ((allowedActor == "owner" && !isOwner) || (allowedActor == "renter" && !isRenter))
                return Forbid();

            if (NormalizeInterestStatus(interest.Status) != requiredStatus)
                return BadRequest(new { error = invalidStatusMessage });

            await _supabase.UpdateInterestStatusAsync(interestId, nextStatus);
            interest.Status = nextStatus;

            try
            {
                var response = await BuildPickupAddressResponseAsync(interest, userId.Value);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<PickupAddressResponse> BuildPickupAddressResponseAsync(InterestSubmission interest, Guid viewerId)
        {
            var toolAddress = await _supabase.GetToolAddressByIdAsync(interest.ToolId);
            if (toolAddress is null)
                throw new InvalidOperationException("Tool address details were not found.");

            var normalizedStatus = NormalizeInterestStatus(interest.Status);
            var isOwner = interest.OwnerId == viewerId;
            var isRenter = interest.RenterId == viewerId.ToString();
            var isRevealed = IsAddressRevealedStatus(normalizedStatus);
            var canReveal = isOwner && CanRevealAddress(normalizedStatus);
            var canStartHandoff = isOwner && normalizedStatus == "revealed";
            var canConfirmPickup = isRenter && normalizedStatus == "handoff_requested";
            var canRequestReturn = isRenter && normalizedStatus == "handed_off";
            var canConfirmReturn = isOwner && normalizedStatus == "return_requested";
            var canView = isOwner || isRevealed;

            string? address = null;
            if (canView)
            {
                address = await ReverseGeocodeExactAddressAsync(
                    toolAddress.AddressLat.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    toolAddress.AddressLng.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            var canRateOwner = false;
            int? currentOwnerRating = null;
            if (isRenter && normalizedStatus == "completed" && interest.OwnerId is Guid ownerIdForRating)
            {
                canRateOwner = true;
                var existing = await _supabase.GetRatingAsync(interest.Id, viewerId, ownerIdForRating);
                if (existing is not null)
                    currentOwnerRating = existing.Score;
            }

            return new PickupAddressResponse
            {
                InterestId = interest.Id,
                ToolName = string.IsNullOrWhiteSpace(interest.ToolName) ? toolAddress.Name : interest.ToolName,
                Status = normalizedStatus,
                CanView = canView,
                CanReveal = canReveal,
                IsRevealed = isRevealed,
                Address = address,
                CanStartHandoff = canStartHandoff,
                CanConfirmPickup = canConfirmPickup,
                CanRequestReturn = canRequestReturn,
                CanConfirmReturn = canConfirmReturn,
                CanRateOwner = canRateOwner,
                CurrentOwnerRating = currentOwnerRating
            };
        }

        private async Task<string> ReverseGeocodeExactAddressAsync(string lat, string lng)
        {
            if (string.IsNullOrWhiteSpace(_config["Google:Maps"]))
                throw new InvalidOperationException("Google Maps API key is not configured.");

            var geocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={_config["Google:Maps"]}";
            using var geocodeReq = new HttpRequestMessage(HttpMethod.Get, geocodeUrl);
            using var geocodeResp = await client.SendAsync(geocodeReq);

            if (!geocodeResp.IsSuccessStatusCode)
                throw new InvalidOperationException("Reverse geocoding failed.");

            var geocodeContent = await geocodeResp.Content.ReadAsStringAsync();
            using var geocodeJson = JsonDocument.Parse(geocodeContent);

            if (!geocodeJson.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                throw new InvalidOperationException("No exact address is available for this tool.");

            var address = results[0].GetProperty("formatted_address").GetString();
            if (string.IsNullOrWhiteSpace(address))
                throw new InvalidOperationException("No exact address is available for this tool.");

            return address;
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

        [HttpPost("users/register")]
        public async Task<IActionResult> Register([FromBody] Models.RegisterRequest request)
        {
            var result = await _userService.RegisterUserAsync(request);

            if (!result.IsValid)
            {
                return BadRequest(new
                {
                    error = result.ErrorMessage
                });
            }

            Guid userId = Guid.Empty;
            if (result.Session?.User?.Id is string sessionUserId)
            {
                Guid.TryParse(sessionUserId, out userId);
            }

            // issue auth cookie on success
            var claims = new List<Claim>();
            if (userId != Guid.Empty)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            }
            claims.Add(new Claim(ClaimTypes.Email, request.Email));
            var identity = new ClaimsIdentity(claims, "cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(result);
        }

        [HttpPost("check-availability")]
        public async Task<IActionResult> CheckAvailability(CheckAvailabilityRequest req)
        {
            var conflict = await _supabase.HasBookingConflictAsync(
                req.ToolId,
                DateTime.Parse(req.StartDate),
                DateTime.Parse(req.EndDate)
            );

            return Ok(new
            {
                hasConflict = conflict,
                message = conflict ? "Tool is already booked for these dates" : null
            });
        }
    }
}
