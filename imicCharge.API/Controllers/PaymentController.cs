using imicCharge.API.Data;
using imicCharge.API.Extensions;
using imicCharge.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace imicCharge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(
        PaymentIntentService paymentIntentService,
        UserManager<ApplicationUser> userManager,
        ILogger<PaymentController> logger) : ControllerBase
    {
        /// <summary>
        /// Creates a new Stripe Payment Intent for the authenticated user.
        /// </summary>
        /// <param name="request">A request object containing the amount to be paid.</param>
        /// <returns>A client secret that the frontend can use to confirm the payment.</returns>
        [Authorize]
        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Kunne ikkje identifisere brukar.");
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized("Brukar ikkje funnen.");
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100),
                    Currency = "nok",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", user.Id }
                    }
                };

                var paymentIntent = await paymentIntentService.CreateAsync(options);
                return Ok(new { clientSecret = paymentIntent.ClientSecret });
            }
            catch (StripeException e)
            {
                logger.LogError(e, "A Stripe error occurred: {StripeError}", e.StripeError.Message);
                return BadRequest(new { error = e.StripeError.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred in CreatePaymentIntent.");
                return StatusCode(500, new { error = "Ein intern feil oppstod under førebuing av betalinga." });
            }
        }

        /// <summary>
        /// Retrieves the current account balance for the authenticated user.
        /// </summary>
        /// <returns>An object containing the user's account balance.</returns>
        [Authorize]
        [HttpGet("get-account-balance")]
        public async Task<IActionResult> GetAccountBalance()
        {
            try
            {
                var userId = User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Kunne ikkje identifisere brukar.");
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized("Brukar ikkje funnen.");
                }

                return Ok(new { accountBalance = user.AccountBalance });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred in GetAccountBalance.");
                return StatusCode(500, new { error = "Ein intern feil oppstod ved henting av saldo." });
            }
        }
    }
}