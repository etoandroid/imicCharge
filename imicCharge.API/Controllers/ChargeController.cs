using imicCharge.API.Data;
using imicCharge.API.Extensions;
using imicCharge.API.Models;
using imicCharge.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace imicCharge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChargeController(
        UserManager<ApplicationUser> userManager,
        ILogger<ChargeController> logger,
        EaseeService easeeService) : ControllerBase
    {
        /// <summary>
        /// Initiates a charging session for the authenticated user on a specified charger.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartCharging([FromBody] StartChargingRequest request)
        {
            var user = await _getCurrentUserAsync();
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Business Logic: Check account balance before starting
            if (user.AccountBalance <= 0)
            {
                return BadRequest(new { error = "Du har for lita saldo. Fyll på kontoen din før du startar lading." });
            }

            logger.LogInformation("User {UserId} starting charge on {ChargerId}. Balance: {Balance}", user.Id, request.ChargerId, user.AccountBalance);

            var success = await easeeService.StartChargingAsync(request.ChargerId);

            if (success)
            {
                return Ok(new { message = $"Ladeførespurnad for ladar {request.ChargerId} er sendt." });
            }

            return StatusCode(500, new { error = $"Klarte ikkje å starte lading for ladar {request.ChargerId}." });
        }

        /// <summary>
        /// Stops the current charging session and processes the final payment.
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> StopCharging([FromBody] StopChargingRequest request)
        {
            var user = await _getCurrentUserAsync();
            if (user == null)
            {
                return NotFound("User not found.");
            }

            logger.LogInformation("User {UserId} stopping charge on {ChargerId}", user.Id, request.ChargerId);

            // Get the final session details BEFORE stopping the charge
            var session = await easeeService.GetOngoingSessionAsync(request.ChargerId);
            if (session == null)
            {
                logger.LogWarning("Could not retrieve ongoing charging session for charger {ChargerId} before stopping.", request.ChargerId);
                // We can still try to stop the charger
            }

            var success = await easeeService.StopChargingAsync(request.ChargerId);

            if (!success)
            {
                return StatusCode(500, new { error = $"Klarte ikkje å stoppe lading for ladar {request.ChargerId}." });
            }

            // If we successfully fetched the session, calculate and deduct the final cost
            if (session?.CostIncludingVat != null)
            {
                var cost = (decimal)session.CostIncludingVat.Value;
                user.AccountBalance -= cost;
                await userManager.UpdateAsync(user);

                logger.LogInformation("Charged user {UserId} for {Kwh} kWh. Cost: {Cost}. New balance: {Balance}", user.Id, session.SessionEnergy, cost, user.AccountBalance);

                return Ok(new
                {
                    message = $"Lading stoppa. Du har blitt belasta {cost:C} for {session.SessionEnergy:F2} kWh.",
                    newBalance = user.AccountBalance
                });
            }

            // Fallback message if session details could not be retrieved
            return Ok(new { message = "Lading stoppa. Saldoen din vil bli oppdatert om kort tid." });
        }

        /// <summary>
        /// Gets a list of all chargers available to the authenticated user.
        /// </summary>
        [HttpGet("chargers")]
        public async Task<IActionResult> GetChargers()
        {
            var chargers = await easeeService.GetChargersAsync();
            if (chargers == null)
            {
                return NotFound("Kunne ikkje hente ladarar.");
            }
            return Ok(chargers);
        }

        /// <summary>
        /// Retrieves ongoing/live charging session details for a specific charger.
        /// </summary>
        [HttpGet("status/{chargerId}")]
        public async Task<IActionResult> GetChargingStatus(string chargerId)
        {
            var user = await _getCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var session = await easeeService.GetOngoingSessionAsync(chargerId);
            if (session == null)
            {
                return NotFound("Could not retrieve charging status.");
            }

            // Use the accurate cost directly from the Easee API
            var cost = (decimal)(session.CostIncludingVat ?? 0);
            var remainingBalance = user.AccountBalance - cost;

            // Get live power usage from the charger state endpoint
            var chargerState = await easeeService.GetChargerStateAsync(chargerId);
            var powerUsage = 0.0;
            if (chargerState.HasValue && chargerState.Value.TryGetProperty("chargerPow", out var powerElement))
            {
                powerUsage = powerElement.GetDouble();
            }

            return Ok(new
            {
                Kwh = session.SessionEnergy,
                RemainingBalance = remainingBalance,
                PowerUsage = powerUsage
            });
        }

        /// <summary>
        /// Helper method to get the current authenticated user.
        /// </summary>
        private async Task<ApplicationUser?> _getCurrentUserAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }
            return await userManager.FindByIdAsync(userId);
        }
    }
}

