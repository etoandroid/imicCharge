using imicCharge.API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace imicCharge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController(
        ILogger<StripeWebhookController> logger,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration) : ControllerBase
    {
        /// <summary>
        /// Handles incoming webhook events from Stripe.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var webhookSecret = configuration["StripeSettings:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                logger.LogError("Stripe Webhook Secret is not configured.");
                return StatusCode(500, "Internal server configuration error.");
            }

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], webhookSecret);

                logger.LogInformation("Stripe webhook event received: {EventType}", stripeEvent.Type);

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
                    {
                        logger.LogError("Could not deserialize PaymentIntent object from Stripe event.");
                        return BadRequest();
                    }

                    if (paymentIntent.Metadata.TryGetValue("user_id", out var userId))
                    {
                        var user = await userManager.FindByIdAsync(userId);
                        if (user != null)
                        {
                            var amountToAdd = (decimal)paymentIntent.Amount / 100;
                            user.AccountBalance += amountToAdd;
                            var result = await userManager.UpdateAsync(user);

                            if (result.Succeeded)
                            {
                                logger.LogInformation("Successfully updated balance for user {UserId} with {Amount}", userId, amountToAdd);
                            }
                            else
                            {
                                logger.LogError("Failed to update balance for user {UserId}. Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                            }
                        }
                        else
                        {
                            logger.LogWarning("Webhook received for non-existent user ID: {UserId}", userId);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Webhook received for PaymentIntent {Id} without a user_id in metadata.", paymentIntent.Id);
                    }
                }
                else
                {
                    logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                }

                return Ok();
            }
            catch (StripeException e)
            {
                logger.LogError(e, "Error processing Stripe webhook: Invalid signature or payload.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred in the Stripe webhook handler.");
                return StatusCode(500);
            }
        }
    }
}