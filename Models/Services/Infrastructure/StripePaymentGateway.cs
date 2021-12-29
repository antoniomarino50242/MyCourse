using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.Options;
using Stripe;
using Stripe.Checkout;

namespace MyCourse.Models.Services.Infrastructure
{
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly IOptionsMonitor<StripeOptions> options;

        public StripePaymentGateway(IOptionsMonitor<StripeOptions> options)
        {
            this.options = options;
        }
        
        public async Task<string> GetPaymentUrlAsync(CoursePayInputModel inputModel)
        {
            SessionCreateOptions sessionCreateOptions = new()
            {
                ClientReferenceId = $"{inputModel.CourseId}/{inputModel.UserId}",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions()
                    {
                        Name = inputModel.Description,
                        Amount = Convert.ToInt64(inputModel.Price.Amount * 100),
                        Currency = inputModel.Price.Currency.ToString(),
                        Quantity = 1
                    }
                },
                Mode = "payment",
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    CaptureMethod = "manual"
                },
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                SuccessUrl = inputModel.ReturnUrl + "?token={CHECKOUT_SESSION_ID}",
                CancelUrl = inputModel.CancelUrl
            };

            RequestOptions requestOptions = new()
            {
                ApiKey = options.CurrentValue.PrivateKey
            };

            SessionService sessionService = new();
            Session session = await sessionService.CreateAsync(sessionCreateOptions, requestOptions);
            return session.Url;
        }

        public Task<CourseSubscribeInputModel> CapturePaymentAsync(string token)
        {
            throw new NotImplementedException();
        }
    }
}