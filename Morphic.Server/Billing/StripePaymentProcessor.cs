// Copyright 2020 Raising the Floor - International
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/GPII/universal/blob/master/LICENSE.txt
//
// The R&D leading to these results received funding from the:
// * Rehabilitation Services Administration, US Dept. of Education under 
//   grant H421A150006 (APCP)
// * National Institute on Disability, Independent Living, and 
//   Rehabilitation Research (NIDILRR)
// * Administration for Independent Living & Dept. of Education under grants 
//   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
// * European Union's Seventh Framework Programme (FP7/2007-2013) grant 
//   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
// * William and Flora Hewlett Foundation
// * Ontario Ministry of Research and Innovation
// * Canadian Foundation for Innovation
// * Adobe Foundation
// * Consumer Electronics Association Foundation

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stripe;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Billing
{

    using Community;
    using Users;

    public class StripeSettings{
        public string SecretKey { get; set; } = "";
        public string Plans { get; set; } = "Plans.json";
        public string WebhookSecret { get; set; } = "";
    }

    public class StripePaymentProcessor : IPaymentProcessor
    {

        public StripePaymentProcessor(StripeSettings settings, Plans plans, ILogger<StripePaymentProcessor> logger)
        {
            RequestOptions = new RequestOptions()
            {
                ApiKey = settings.SecretKey
            };
            Customers = new CustomerService();
            Subscriptions = new SubscriptionService();
            SubscriptionItems = new SubscriptionItemService();
            Sources = new SourceService();
            this.plans = plans;
            this.logger = logger;
        }

        public RequestOptions RequestOptions;
        public CustomerService Customers;
        public SubscriptionService Subscriptions;
        public SubscriptionItemService SubscriptionItems;
        public SourceService Sources;
        private ILogger logger;
        private Plans plans;

        private static readonly List<PromotionCode> PromotionCodes = new List<PromotionCode>();
        private static DateTime promotionCodesUpdated = DateTime.MinValue;

        public async Task StartCommunitySubscription(Community community, BillingRecord billing, User contact)
        {
            var plan = plans.GetPlan(billing.PlanId)!;
            if (plan.Stripe is StripePlan stripePlan)
            {
                try
                {
                    var customer = new CustomerCreateOptions()
                    {
                        Name = community.Name,
                        Email = contact.Email.PlainText
                    };
                    var stripeCustomer = await Customers.CreateAsync(customer, RequestOptions);
                    var subscription = new SubscriptionCreateOptions()
                    {
                        Customer = stripeCustomer.Id,
                        Items = new List<SubscriptionItemOptions>()
                        {
                            new SubscriptionItemOptions()
                            {
                                Price = stripePlan.PriceId
                            }
                        }
                    };

                    if (billing.TrialEnd > DateTime.Now){
                        subscription.TrialEnd = billing.TrialEnd;
                    }
                    var stripeSubscription = await Subscriptions.CreateAsync(subscription, RequestOptions);
                    if (billing.Stripe == null)
                    {
                        billing.Stripe = new StripeBillingRecord();
                    }
                    billing.Stripe.CustomerId = stripeCustomer.Id;
                    billing.Stripe.SubscriptionId = stripeSubscription.Id;
                    billing.Stripe.SubscriptionItemId = stripeSubscription.Items.Data[0].Id;
                }
                catch (StripeException e)
                {
                    logger.LogError(e, "Failed to create stripe subscription");
                    throw;
                }
            }else{
                throw new ArgumentException("StripePaymentProcessor requires a StripePlan");
            }
        }

        public async Task ChangeCommunitySubscription(Community community, BillingRecord billing)
        {
            var plan = plans.GetPlan(billing.PlanId)!;
            if (plan.Stripe is StripePlan stripePlan)
            {
                try
                {
                    var update = new SubscriptionItemUpdateOptions()
                    {
                        Price = stripePlan.PriceId
                    };
                    await SubscriptionItems.UpdateAsync(billing.Stripe!.SubscriptionItemId, update, RequestOptions);
                }
                catch (StripeException e)
                {
                    logger.LogError(e, "Failed to update stripe subscription");
                    throw;
                }
            }
            else
            {
                throw new ArgumentException("StripePaymentProcessor requires a StripePlan");
            }
        }

        public async Task CancelCommunitySubscription(Community community, BillingRecord billing)
        {
            var plan = plans.GetPlan(billing.PlanId)!;
            if (plan.Stripe is StripePlan stripePlan)
            {
                try
                {
                    var update = new SubscriptionUpdateOptions()
                    {
                        CancelAtPeriodEnd = true
                    };
                    await Subscriptions.UpdateAsync(billing.Stripe!.SubscriptionId, update, RequestOptions);
                }
                catch (StripeException e)
                {
                    logger.LogError(e, "Failed to update stripe subscription");
                    throw;
                }
            }
            else
            {
                throw new ArgumentException("StripePaymentProcessor requires a StripePlan");
            }
        }

        public async Task ChangeCommunityContact(Community community, BillingRecord billing, User contact)
        {
            try
            {
                var update = new CustomerUpdateOptions()
                {
                    Email = contact.Email.PlainText
                };
                await Customers.UpdateAsync(billing.Stripe!.CustomerId, update, RequestOptions);
            }
            catch (StripeException e)
            {
                logger.LogError(e, "Failed to update stripe customer");
                throw;
            }
        }

        public async Task ChangeCommunityCard(Community community, BillingRecord billing, object card)
        {
            if (card is string stripeToken)
            {
                try
                {
                    var update = new CustomerUpdateOptions()
                    {
                        Source = stripeToken
                    };
                    update.AddExpand("default_source");
                    var stripeCustomer = await Customers.UpdateAsync(billing.Stripe!.CustomerId, update, RequestOptions);
                    if (stripeCustomer.DefaultSource is Stripe.Card stripeCard)
                    {
                        billing.Card = new Card()
                        {
                            Last4 = stripeCard.Last4,
                            Brand = stripeCard.Brand
                        };
                    }
                }
                catch (StripeException e)
                {
                    logger.LogError(e, "Failed to update stripe customer");
                    throw new PaymentProcessorCardException();
                }
            }
            else
            {
                throw new ArgumentException("StripePaymentProcessor requires a string card token");
            }
        }

        /// <summary>
        /// Updates the coupon used by the community billing.
        /// </summary>
        /// <param name="community"></param>
        /// <param name="billing"></param>
        /// <param name="couponCode">The coupon code. null to remove it.</param>
        /// <returns>The coupon, or null if it's unknown.</returns>
        /// <exception cref="PaymentProcessorCardException"></exception>
        public async Task<Coupon?> ChangeCommunityCoupon(Community community, BillingRecord billing, string? couponCode)
        {
            PromotionCode? promo = string.IsNullOrEmpty(couponCode) ? null : await this.FindCode(couponCode);

            try
            {
                CustomerUpdateOptions options = new CustomerUpdateOptions()
                {
                    PromotionCode = promo?.Id,
                };

                options.AddExpand("default_source");
                await this.Customers.UpdateAsync(billing.Stripe!.CustomerId, options, this.RequestOptions);
            }
            catch (StripeException e)
            {
                this.logger.LogError(e, "Failed to update stripe customer");
                throw new PaymentProcessorCardException();
            }

            return promo == null ? null : await this.GetCoupon(promo);
        }

        /// <summary>
        /// Gets information about a coupon code.
        /// </summary>
        public async Task<Coupon?> GetCoupon(string couponCode)
        {
            PromotionCode? promo = await this.FindCode(couponCode);

            return promo == null
                ? null
                : await this.GetCoupon(promo);
        }

        /// <summary>
        /// Gets information about a coupon.
        /// </summary>
        private async Task<Coupon?> GetCoupon(PromotionCode promo)
        {
            promo.Coupon.Metadata.TryGetValue("plan", out string? plan);
            promo.Coupon.Metadata.TryGetValue("email", out string? email);

            Coupon coupon = new Coupon()
            {
                Code = promo.Code,
                Name = promo.Coupon.Name,
                Id = promo.Coupon.Id,
                ApiId = promo.Id,
                AmountOff = promo.Coupon.AmountOff,
                PercentOff = promo.Coupon.PercentOff,
                Expired = promo.ExpiresAt < DateTime.UtcNow,
                Active = promo.Active && promo.Coupon.Valid,
                ValidForPlan = plan,
                ValidForEmail = email
            };

            return coupon;
        }

        /// <summary>
        /// Gets the coupon information from stripe.
        /// </summary>
        private async Task<PromotionCode?> FindCode(string couponCode)
        {
            PromotionCode? Find()
            {
                return StripePaymentProcessor.PromotionCodes.Find(promo =>
                    promo.Code.Equals(couponCode, StringComparison.OrdinalIgnoreCase));
            }

            PromotionCode? promotionCode = Find();

            if (promotionCode == null)
            {
                // The coupon was not found - update the list from stripe.
                bool updated = await this.UpdateCouponCodes();
                if (updated)
                {
                    promotionCode = Find();
                }
            }

            return promotionCode;
        }

        /// <summary>
        /// Updates a list of the coupon codes from stripe.
        /// </summary>
        /// <returns>true if the list was updated.</returns>
        private async Task<bool> UpdateCouponCodes()
        {
            StripeConfiguration.ApiKey = this.RequestOptions.ApiKey;
            bool updated;

            if (DateTime.Now - StripePaymentProcessor.promotionCodesUpdated > TimeSpan.FromMinutes(1))
            {
                PromotionCodeService service = new PromotionCodeService();

                StripeList<PromotionCode> codes = await service.ListAsync(new PromotionCodeListOptions()
                {
                    Limit = 100
                });;


                lock (StripePaymentProcessor.PromotionCodes)
                {
                    StripePaymentProcessor.PromotionCodes.Clear();
                    StripePaymentProcessor.PromotionCodes.AddRange(codes.ToList());
                }

                StripePaymentProcessor.promotionCodesUpdated = DateTime.Now;
                updated = true;
            }
            else
            {
                updated = false;
            }

            return updated;
        }
    }

}