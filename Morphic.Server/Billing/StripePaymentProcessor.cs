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
            Sources = new SourceService();
            this.plans = plans;
            this.logger = logger;
        }

        public RequestOptions RequestOptions;
        public CustomerService Customers;
        public SubscriptionService Subscriptions;
        public SourceService Sources;
        private ILogger logger;
        private Plans plans;

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
                    var update = new SubscriptionUpdateOptions()
                    {
                        Items = new List<SubscriptionItemOptions>()
                        {
                            new SubscriptionItemOptions()
                            {
                                Price = stripePlan.PriceId
                            }
                        }
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
                    if (stripeCustomer.DefaultSource is Source stripeSource)
                    {
                        billing.Card = new Card()
                        {
                            Last4 = stripeSource.Card.Last4,
                            Brand = stripeSource.Card.Brand
                        };
                    }
                }
                catch (StripeException e)
                {
                    logger.LogError(e, "Failed to update stripe customer");
                    throw;
                }
            }
            else
            {
                throw new ArgumentException("StripePaymentProcessor requires a string card token");
            }
        }

    }

}