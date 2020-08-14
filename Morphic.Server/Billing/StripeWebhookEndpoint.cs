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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Net;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Stripe;
using System.Linq;

namespace Morphic.Server.Billing
{

    using Http;
    using Community;

    [Path("/v1/stripe/webhook")]
    public class StripeWebhookEndpoint: Endpoint
    {
        public StripeWebhookEndpoint(StripeSettings stripeSettings, Plans plans, IHttpContextAccessor contextAccessor, ILogger<StripeWebhookEndpoint> logger): base(contextAccessor, logger)
        {
            endpointSecret = stripeSettings.WebhookSecret;
            this.plans = plans;
        }

        private string endpointSecret;
        private Plans plans;

        [Method]
        public async Task Post()
        {
            var stream = new StreamReader(Request.Body);
            var json = await stream.ReadToEndAsync();
            if (json == ""){
                throw new HttpError(HttpStatusCode.BadRequest);
            }
            try
            {
                Event stripeEvent;
                try
                {
                    stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], endpointSecret);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Stripe webhook exception thrown parsing event");
                    throw new HttpError(HttpStatusCode.BadRequest);
                }
                switch (stripeEvent.Type)
                {
                    case Events.CustomerSubscriptionUpdated:
                        {
                            if (stripeEvent.Data.Object is Subscription subscription)
                            {
                                await HandleSubscriptionUpdated(subscription);
                            }
                            else
                            {
                                throw new HttpError(HttpStatusCode.BadRequest);
                            }
                            break;
                        }
                    case Events.CustomerSourceUpdated:
                        {
                            if (stripeEvent.Data.Object is Stripe.Card card)
                            {
                                await HandleCardUpdated(card);
                            }
                            break;
                        }
                    default:
                        throw new HttpError(HttpStatusCode.BadRequest);
                }
            }
            catch (StripeException e)
            {
                logger.LogError(e, "Stripe webhook exception thrown");
                throw new HttpError(HttpStatusCode.BadRequest);
            }
        }

        private async Task HandleSubscriptionUpdated(Subscription subscription)
        {
            var db = Context.GetDatabase();
            var billing = await db.Get<BillingRecord>(b => b.Stripe!.SubscriptionId == subscription.Id);
            if (billing == null)
            {
                return;
            }
            var status = billing.Status;
            switch (subscription.Status)
            {
                case "past_due":
                    status = BillingStatus.PastDue;
                    break;
                case "canceled":
                    status = BillingStatus.Canceled;
                    break;
                default:
                    status = BillingStatus.Paid;
                    break;
            }
            if (status != billing.Status){
                await db.SetField(billing, b => b.Status, status);
            }

            var trialEnd = subscription.TrialEnd ?? default(DateTime);
            if (trialEnd != billing.TrialEnd)
            {
                await db.SetField(billing, b => b.TrialEnd, trialEnd);
            }

            var items = subscription.Items.Data.ToArray();
            if (items.Length > 0)
            {
                Plan? plan = null;
                SubscriptionItem? ourItem = null;
                try
                {
                    // Find the item that matches what we have in our db
                    ourItem = items.First(i => i.Id == billing.Stripe!.SubscriptionItemId);
                    plan = plans.GetPlanForStripePrice(items[0].Price.Id);
                }
                catch
                {
                    // If our item isn't found, look for the first item using a price that we know about
                    foreach (var item in items)
                    {
                        plan = plans.GetPlanForStripePrice(item.Price.Id);
                        if (plan != null)
                        {
                            ourItem = item;
                            break;
                        }
                    }
                }
                if (plan != null)
                {
                    if (plan.Id != billing.PlanId)
                    {
                        await db.SetField(billing, b => b.PlanId, plan.Id);
                    }
                }
                if (ourItem != null)
                {
                    if (ourItem.Id != billing.Stripe!.SubscriptionItemId)
                    {
                        await db.SetField(billing, b => b.Stripe!.SubscriptionItemId, ourItem.Id);
                    }
                }
            }
        }

        private async Task HandleCardUpdated(Stripe.Card card)
        {
            var db = Context.GetDatabase();
            var billing = await db.Get<BillingRecord>(b => b.Stripe!.CustomerId == card.CustomerId);
            if (billing == null)
            {
                return;
            }
            billing.Card = new Card()
            {
                Last4 = card.Last4,
                Brand = card.Brand
            };
            await db.SetField(billing, b => b.Card, billing.Card);
        }
    }

}