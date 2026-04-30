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
    /// <summary>
    /// Provides API endpoints for managing tools, interests, user registration, location services, and related
    /// operations for the Supabase database.
    /// </summary>
    /// <remarks>This controller exposes endpoints for tool CRUD operations, interest submission and workflow,
    /// user registration, location lookups (including reverse geocoding and autocomplete), and category retrieval. It
    /// integrates with Supabase, Google Maps APIs for geocoding and place suggestions, and
    /// Sendbird for chat provisioning between users. Most endpoints require authentication for actions related to
    /// interests and user data.</remarks>
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

        /// <summary>
        /// Initializes a new instance of the SupabaseController class with the specified configuration, services, HTTP
        /// client factory, and logger.
        /// </summary>
        /// <param name="config">The application configuration settings used to initialize the controller.</param>
        /// <param name="supabase">The service used to interact with Supabase-related functionality.</param>
        /// <param name="sendbird">The service used to interact with Sendbird-related functionality.</param>
        /// <param name="httpClientFactory">The factory used to create HTTP client instances for making external requests.</param>
        /// <param name="userService">The service used to manage user-related operations.</param>
        /// <param name="logger">The logger used to record diagnostic and operational information for the controller.</param>
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

        //simple test to make sure api is running
        [HttpGet]
        public string Test()
        {
            Console.WriteLine("success: \"test\"");
            return "good";
        }

        //gets neighborhoods and their coordinates
        [HttpGet("neighborhoodTriples")]
        public async Task<List<NeighborhoodTriple>> neighborhoodTriples()
        {
            var res = await _supabase.GetNeighborhoodTriples();
            Console.WriteLine($"\"neighborhoodCoords\": {res.Count}");
            return res;
        }
        
        //convert lat, lng to address
        [HttpGet("reverseGeocode/{lat}/{lng}")]
        public async Task<String> ReverseGeocode(string lat, string lng)
        {
            //call helper method
            var address = await ReverseGeocodeExactAddressAsync(lat, lng);

            Console.WriteLine($"reverseGeocode/{lat}/{lng}: {address}");
            return address;
        }

        //get suggestions for autocomplete
        [HttpGet("suggestLocations/{location}")]
        public async Task<List<Models.AddressPair>> GetLocation(string location)
        {
            List<Models.AddressPair> tuples = new List<Models.AddressPair>();

            //wrapper for google maps places:autocomplete api: input -> (suggested address, placeId) list)
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

        //get coordinates from google placeId
        [HttpGet("getCoordinates/{placeId}")]
        public async Task<Models.Location> GetCoordinates(string placeId)
        {
            //wrapper google maps places api: placeId -> latitude, longitude
            var placesUrl = $"https://places.googleapis.com/v1/places/{placeId}";

            var placesReq = new HttpRequestMessage(HttpMethod.Get, placesUrl);
            placesReq.Headers.Add("X-Goog-Api-Key", _config["Google:Maps"]);
            placesReq.Headers.Add("X-Goog-FieldMask", "location");

            //navigate tree
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

        //access user secret by key
        [HttpGet("getSecret/{key}")]
        public string GetSecret(string key)
        {
            //get associated value
            string? val = _config[key];
            Console.WriteLine(val != null ? $"success: \"getSecret/{key}\"" : $"failure: \"getSecret/{key}\"");
            return val ?? string.Empty;
        }

        //get all categories
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

        //get all tools
        [HttpGet("Tools")]
        public async Task<ActionResult<List<Models.Tool>>> GetTools()
        {
            var items = await _supabase.GetToolsAsync();
            return Ok(items);
        }

        //get tool by id
        [HttpGet("Tools/{id}")]
        public async Task<ActionResult<Models.Tool>> GetToolById(Guid id)
        {
            var tool = await _supabase.GetToolByIdAsync(id);
            if (tool is null) return NotFound();

            //get ratings
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

        //post tool to db
        [HttpPost("Tools")]
        public async Task<ActionResult<Models.Tool>> InsertTool([FromBody] Models.Tool t)
        {
            var item = await _supabase.InsertToolAsync(t);
            return Ok(item);
        }

        //delete tool from db
        [HttpDelete("Tools/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _supabase.DeleteToolAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Submits a new interest request for a tool rental, creating or reusing a chat channel between the renter and
        /// the tool owner.
        /// </summary>
        /// <remarks>The caller must be authenticated. The method ensures that both the renter and the
        /// tool owner have valid chat accounts before proceeding. If an interest already exists for the same renter and
        /// tool, the existing chat channel is reused. Returns appropriate error responses for authentication failures,
        /// missing users or tools, invalid ownership, or chat provisioning errors.</remarks>
        /// <param name="request">The interest request details, including the tool to rent, rental period, and an optional message. Must not
        /// be null.</param>
        /// <returns>An ActionResult containing the interest response, including the chat channel URL and interest ID if
        /// successful. Returns an error response if the request is invalid or cannot be processed.</returns>
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

        /// <summary>
        /// Retrieves a list of interest submissions associated with the specified user, including both interests as a
        /// renter and as an owner.
        /// </summary>
        /// <remarks>The returned list includes deduplicated interest submissions where the user is either
        /// a renter or an owner. The most recent submission per channel or interest is returned. Counterpart user
        /// information is included for each interest item.</remarks>
        /// <param name="userId">The unique identifier of the user whose interests are to be retrieved. Must not be <see cref="Guid.Empty"/>.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="MyInterestItem"/> objects representing the
        /// user's interests. Returns a bad request response if <paramref name="userId"/> is not provided.</returns>
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

        /// <summary>
        /// Retrieves the pickup address information for the specified interest if the authenticated user is the owner
        /// or renter.
        /// </summary>
        /// <remarks>The caller must be authenticated and must be either the owner or renter associated
        /// with the specified interest to access the pickup address information.</remarks>
        /// <param name="interestId">The unique identifier of the interest for which to retrieve the pickup address.</param>
        /// <returns>An <see cref="ActionResult{PickupAddressResponse}"/> containing the pickup address details if found and
        /// authorized; otherwise, an appropriate error response such as Unauthorized, NotFound, Forbid, or BadRequest.</returns>
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

        /// <summary>
        /// Reveals the pickup address for the specified interest if the authenticated user is the owner and the
        /// interest is in a state that allows address revelation.
        /// </summary>
        /// <remarks>Returns <see cref="UnauthorizedResult"/> if the user is not authenticated, <see
        /// cref="NotFoundResult"/> if the interest does not exist, <see cref="ForbidResult"/> if the user is not the
        /// owner, and <see cref="BadRequestResult"/> if the interest is not in a valid state to reveal the address or
        /// if an error occurs while building the response.</remarks>
        /// <param name="interestId">The unique identifier of the interest for which to reveal the pickup address.</param>
        /// <returns>An <see cref="ActionResult{PickupAddressResponse}"/> containing the pickup address information if
        /// successful; otherwise, an error response indicating the reason for failure.</returns>
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

        /// <summary>
        /// Initiates the handoff process for the specified interest by transitioning its status to 'handoff requested'.
        /// </summary>
        /// <remarks>This action is only allowed for the owner of the interest and when the interest
        /// status is 'revealed'.</remarks>
        /// <param name="interestId">The unique identifier of the interest for which to start the handoff process.</param>
        /// <returns>A response containing the pickup address details if the handoff is successfully started; otherwise, an error
        /// result.</returns>
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


        /// <summary>
        /// Confirms the pickup of an item for the specified interest, transitioning its status from 'handoff requested'
        /// to 'handed off'.
        /// </summary>
        /// <remarks>This action can only be performed by the renter when the interest is in the 'handoff
        /// requested' status. If the interest is not in the correct state, the operation will not succeed.</remarks>
        /// <param name="interestId">The unique identifier of the interest for which the pickup is being confirmed.</param>
        /// <returns>An ActionResult containing the updated pickup address information if the pickup is successfully confirmed.</returns>
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

        /// <summary>
        /// Initiates a return request for the specified interest after the item has been picked up.
        /// </summary>
        /// <remarks>This action can only be performed by the renter and only after the pickup has been
        /// confirmed. If the interest is not in the correct status, the request will not be processed.</remarks>
        /// <param name="interestId">The unique identifier of the interest for which the return is being requested.</param>
        /// <returns>A response containing the pickup address details for the return request.</returns>
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

        /// <summary>
        /// Confirms the return of an item for the specified interest after a return has been requested.
        /// </summary>
        /// <remarks>This action can only be performed by the owner and only when the interest is in the
        /// 'return_requested' status. If the interest is not in the correct status, the operation will not
        /// proceed.</remarks>
        /// <param name="interestId">The unique identifier of the interest for which the return is being confirmed.</param>
        /// <returns>An ActionResult containing the updated pickup address information if the return confirmation is successful.</returns>
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

        /// <summary>
        /// Represents a request to submit an owner rating, including the score value.
        /// </summary>
        public class OwnerRatingRequest
        {
            [JsonPropertyName("score")]
            public int Score { get; set; }
        }

        /// <summary>
        /// Submits a rating for the owner of a completed rental interest.
        /// </summary>
        /// <remarks>The authenticated user must be the renter associated with the specified interest, and
        /// the rental must be completed before submitting a rating. Returns appropriate error responses for invalid
        /// input, unauthorized access, or if the interest cannot be rated.</remarks>
        /// <param name="interestId">The unique identifier of the rental interest to rate.</param>
        /// <param name="body">The rating details to submit, including the score. The score must be between 1 and 5.</param>
        /// <returns>An ActionResult containing the updated pickup address response if the rating is successfully submitted;
        /// otherwise, an error response indicating the reason for failure.</returns>
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

        /// <summary>
        /// Retrieves the unique identifier of the currently authenticated user, if available.
        /// </summary>
        /// <returns>A <see cref="Guid"/> representing the authenticated user's unique identifier, or <see langword="null"/> if
        /// the user is not authenticated or the identifier is not present.</returns>
        private Guid? GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }

        /// <summary>
        /// Normalizes the specified interest status string to a standard format.
        /// </summary>
        /// <param name="status">The interest status to normalize. May be null, empty, or contain leading/trailing whitespace.</param>
        /// <returns>A normalized interest status string. Returns "pending" if <paramref name="status"/> is null, empty, or
        /// consists only of whitespace; otherwise, returns the trimmed, lowercase version of <paramref name="status"/>.</returns>
        private static string NormalizeInterestStatus(string? status) =>
            string.IsNullOrWhiteSpace(status) ? "pending" : status.Trim().ToLowerInvariant();

        /// <summary>
        /// Determines whether the specified status string represents an address that has been revealed or is in a
        /// related post reveal state.
        /// </summary>
        /// <remarks>The method returns true for statuses normalized to "revealed", "handoff_requested",
        /// "handed_off", "return_requested", or "completed". Status normalization is applied before
        /// evaluation.</remarks>
        /// <param name="status">The status string to evaluate. May be null.</param>
        /// <returns>true if the status indicates the address is revealed or in a related state; otherwise, false.</returns>
        private static bool IsAddressRevealedStatus(string? status) =>
            NormalizeInterestStatus(status) is "revealed" or "handoff_requested" or "handed_off" or "return_requested" or "completed";

        /// <summary>
        /// Determines whether the address can be revealed based on the specified interest status.
        /// </summary>
        /// <param name="status">The interest status to evaluate. May be null.</param>
        /// <returns>true if the address can be revealed for the given status; otherwise, false.</returns>
        private static bool CanRevealAddress(string? status) =>
            NormalizeInterestStatus(status) is "pending" or "confirmed";

        /// <summary>
        /// Transitions the status of an interest to a new state if the current user is authorized and the interest is
        /// in the required status.
        /// </summary>
        /// <remarks>The method checks user authentication, authorization, and the current status of the
        /// interest before performing the transition. Only the specified actor can perform the transition, and the
        /// interest must be in the required status.</remarks>
        /// <param name="interestId">The unique identifier of the interest to transition.</param>
        /// <param name="allowedActor">Specifies which actor ('owner' or 'renter') is permitted to perform the transition.</param>
        /// <param name="requiredStatus">The status that the interest must currently have for the transition to proceed.</param>
        /// <param name="nextStatus">The status to set for the interest if the transition is successful.</param>
        /// <param name="invalidStatusMessage">The error message to return if the interest is not in the required status.</param>
        /// <returns>An ActionResult containing a PickupAddressResponse if the transition is successful; otherwise, an
        /// appropriate error result such as Unauthorized, NotFound, Forbid, or BadRequest.</returns>
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

        /// <summary>
        /// Builds a response containing pickup address details and related permissions for a given interest and viewer.
        /// </summary>
        /// <remarks>The returned response includes permission flags indicating what actions the viewer
        /// can perform based on their role and the current status of the interest. Address information is only included
        /// if the viewer is authorized to view it.</remarks>
        /// <param name="interest">The interest submission for which to build the pickup address response. Must contain valid tool and
        /// participant information.</param>
        /// <param name="viewerId">The unique identifier of the user requesting the pickup address information. Determines permissions and
        /// visibility in the response.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a PickupAddressResponse with
        /// address details, permissions, and status information relevant to the viewer.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the tool address details associated with the interest could not be found.</exception>
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

        //convert latitude, longitude to address
        private async Task<string> ReverseGeocodeExactAddressAsync(string lat, string lng)
        {
            if (string.IsNullOrWhiteSpace(_config["Google:Maps"]))
                throw new InvalidOperationException("Google Maps API key is not configured.");

            //wrapper for google maps reverse geocoding api: latitude, longitude -> address
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

        /// <summary>
        /// Represents a request to create a new tool with the specified name, description, and price.
        /// </summary>
        /// <param name="Name">The name of the tool to create. Cannot be null or empty.</param>
        /// <param name="Description">A brief description of the tool. Cannot be null.</param>
        /// <param name="Price">The price of the tool. Must be a non-negative value.</param>
        public record CreateToolRequest(string Name, string Description, decimal Price);

        /// <summary>
        /// Represents a request to express interest in a tool, including tool identification, optional message, and an
        /// optional date range.
        /// </summary>
        /// <remarks>Use this class to submit information about a user's interest in a specific tool,
        /// optionally specifying a message and a date range for the interest period. All properties must be set
        /// appropriately before sending the request.</remarks>
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

        /// <summary>
        /// Represents the response returned after processing an interest related operation.
        /// </summary>
        /// <remarks>This class is used to represent the outcome of an API call related to
        /// interests, including operation success, the associated channel URL, and the unique identifier of the
        /// interest. All properties are optional except for Success, which indicates whether the operation completed
        /// successfully.</remarks>
        public class InterestResponse
        {
            public bool Success { get; set; }

            [JsonPropertyName("channel_url")]
            public string? ChannelUrl { get; set; }

            [JsonPropertyName("interest_id")]
            public Guid? InterestId { get; set; }
        }

        /// <summary>
        /// Represents an item describing a user's interest in a tool, including details such as the tool name, status,
        /// and associated metadata.
        /// </summary>
        /// <remarks>This class is used to track and manage user interests in various tools or
        /// services, including information about the tool, the user's role wrt the tool, and relevant timestamps. All properties are
        /// gettable and settable.</remarks>
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


        /// <summary>
        /// Handles user registration by creating a new user account with the provided registration details.
        /// </summary>
        /// <remarks>On successful registration, an authentication cookie is issued for the new user,
        /// enabling immediate authentication. The response includes any validation errors if registration is
        /// unsuccessful.</remarks>
        /// <param name="request">The registration information for the new user. Must include all required fields such as email and password.
        /// Cannot be null.</param>
        /// <returns>An IActionResult indicating the result of the registration operation. Returns 200 OK with registration
        /// details on success, or 400 Bad Request with error information if registration fails.</returns>
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

        /// <summary>
        /// Checks whether the specified tool is available for booking within the requested date range.
        /// </summary>
        /// <remarks>Returns a conflict message if the tool is already booked for the specified dates. The
        /// response does not include details about existing bookings.</remarks>
        /// <param name="req">The booking request containing the tool identifier and the desired start and end dates. Cannot be null.</param>
        /// <returns>An <see cref="IActionResult"/> containing a JSON object with a Boolean value indicating whether a booking
        /// conflict exists and an optional message if the tool is already booked.</returns>
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
