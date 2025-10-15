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
    EaseeService easeeService,
    IConfiguration configuration) : ControllerBase // Legg til IConfiguration
    {
        /// <summary>
        /// Initiates a charging session for the authenticated user on a specified charger.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartCharging([FromBody] StartChargingRequest request)
        {
            var userId = User.GetUserId();
            var user = await userManager.FindByIdAsync(userId!);
            if (user == null)
            {
                return NotFound("Brukar ikkje funnen.");
            }

            // Business Logic: Check account balance
            if (user.AccountBalance <= 0)
            {
                return BadRequest(new { error = "Du har for lita saldo. Fyll på kontoen din før du startar lading." });
            }

            logger.LogInformation("User {UserId} starting charge on {ChargerId}. Balance: {Balance}", userId, request.ChargerId, user.AccountBalance);

            var success = await easeeService.StartChargingAsync(request.ChargerId);

            if (success)
            {
                return Ok(new { message = $"Ladeførespurnad for ladar {request.ChargerId} er sendt." });
            }

            return StatusCode(500, new { error = $"Klarte ikkje å starte lading for ladar {request.ChargerId}." });
        }

        /// <summary>
        /// Stops the current charging session and processes the payment.
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> StopCharging([FromBody] StopChargingRequest request)
        {
            var userId = User.GetUserId();
            var user = await userManager.FindByIdAsync(userId!);
            if (user == null)
            {
                return NotFound("Brukar ikkje funnen.");
            }

            logger.LogInformation("User {UserId} stopping charge on {ChargerId}", userId, request.ChargerId);

            var success = await easeeService.StopChargingAsync(request.ChargerId);

            if (!success)
            {
                return StatusCode(500, new { error = $"Klarte ikkje å stoppe lading for ladar {request.ChargerId}." });
            }

            // Business Logic: Get session details and deduct cost
            var session = await easeeService.GetLatestChargingSessionAsync(request.ChargerId);
            if (session == null)
            {
                logger.LogWarning("Could not retrieve latest charging session for charger {ChargerId}", request.ChargerId);
                return Ok(new { message = "Lading stoppa, men kunne ikkje hente ladeforbruk. Kontakt kundeservice." });
            }

            var pricePerKwh = configuration.GetValue<decimal>("ChargingSettings:PricePerKwh");
            var cost = (decimal)session.Kwh * pricePerKwh;

            user.AccountBalance -= cost;
            await userManager.UpdateAsync(user);

            logger.LogInformation("Charged user {UserId} for {Kwh} kWh. Cost: {Cost}. New balance: {Balance}", userId, session.Kwh, cost, user.AccountBalance);

            return Ok(new
            {
                message = $"Lading stoppa. Du har blitt belasta {cost:C} for {session.Kwh:F2} kWh.",
                newBalance = user.AccountBalance
            });
        }
    }
}