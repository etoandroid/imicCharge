using imicCharge.API.Data;
using imicCharge.API.Extensions;
using imicCharge.API.Models;
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
        ILogger<ChargeController> logger) : ControllerBase
    {
        /// <summary>
        /// Initiates a charging session for the authenticated user on a specified charger.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartCharging([FromBody] StartChargingRequest request)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Kunne ikkje identifisere brukar.");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Brukar ikkje funnen.");
            }

            // --- TODO: Business Logic ---
            // 1. Check if user.AccountBalance is sufficient.
            // 2. Call EaseeService to start charging on request.ChargerId.
            // 3. Handle success or failure response from EaseeService.

            logger.LogInformation("User {UserId} requested to start charging on charger {ChargerId}", userId, request.ChargerId);

            // Placeholder response
            return Ok(new { message = $"Ladeforespørsel for lader {request.ChargerId} er motteke." });
        }

        /// <summary>
        /// Stops the current charging session for the authenticated user on a specified charger.
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> StopCharging([FromBody] StopChargingRequest request)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Kunne ikkje identifisere brukar.");
            }

            // --- TODO: Business Logic ---
            // 1. Call EaseeService to stop charging on request.ChargerId.
            // 2. On success, get total energy used from EaseeService.
            // 3. Calculate the cost and deduct it from the user's AccountBalance.
            // 4. Update the user in the database.

            logger.LogInformation("User {UserId} requested to stop charging on charger {ChargerId}", userId, request.ChargerId);

            // Placeholder response
            return Ok(new { message = $"Stopp-forespørsel for ladar {request.ChargerId} er motteke." });
        }
    }
}