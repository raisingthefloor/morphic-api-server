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
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Linq;
using Xunit;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Morphic.Server.Tests.Billing
{

    using Server.Billing;

    public class StripeWebhookEndpointTests : EndpointRequestTests
    {

        [Fact]
        public async Task TestPost()
        {
            // POST, no content
            var path = $"/v1/stripe/webhook";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Method = HttpMethod.Post;
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var json = "{\n  \"id\": \"evt_1HElhEFNS9zyw2T7xOadM2fB\",\n  \"object\": \"event\",\n  \"api_version\": \"2020-03-02\",\n  \"created\": 1597107032,\n  \"data\": {\n    \"object\": {\n      \"id\": \"sub_HoOURrrvyzUi6K\",\n      \"object\": \"subscription\",\n      \"application_fee_percent\": null,\n      \"billing_cycle_anchor\": 1599699031,\n      \"billing_thresholds\": null,\n      \"cancel_at\": null,\n      \"cancel_at_period_end\": false,\n      \"canceled_at\": null,\n      \"collection_method\": \"charge_automatically\",\n      \"created\": 1597107032,\n      \"current_period_end\": 1599699031,\n      \"current_period_start\": 1597107032,\n      \"customer\": \"cus_HoOUJwzTrNFYUV\",\n      \"days_until_due\": null,\n      \"default_payment_method\": null,\n      \"default_source\": null,\n      \"default_tax_rates\": [],\n      \"discount\": null,\n      \"ended_at\": null,\n      \"items\": {\n        \"object\": \"list\",\n        \"data\": [\n          {\n            \"id\": \"si_HoOUIxe2lMSA4Z\",\n            \"object\": \"subscription_item\",\n            \"billing_thresholds\": null,\n            \"created\": 1597107032,\n            \"metadata\": {},\n            \"plan\": {\n              \"id\": \"price_1HEhXfFNS9zyw2T7YKMUaXSj\",\n              \"object\": \"plan\",\n              \"active\": true,\n              \"aggregate_usage\": null,\n              \"amount\": 600,\n              \"amount_decimal\": \"600\",\n              \"billing_scheme\": \"per_unit\",\n              \"created\": 1597091063,\n              \"currency\": \"usd\",\n              \"interval\": \"month\",\n              \"interval_count\": 1,\n              \"livemode\": false,\n              \"metadata\": {},\n              \"nickname\": null,\n              \"product\": \"prod_HoKCngRhIjfgLt\",\n              \"tiers\": null,\n              \"tiers_mode\": null,\n              \"transform_usage\": null,\n              \"trial_period_days\": 30,\n              \"usage_type\": \"licensed\"\n            },\n            \"price\": {\n              \"id\": \"price_1HEhXfFNS9zyw2T7YKMUaXSj\",\n              \"object\": \"price\",\n              \"active\": true,\n              \"billing_scheme\": \"per_unit\",\n              \"created\": 1597091063,\n              \"currency\": \"usd\",\n              \"livemode\": false,\n              \"lookup_key\": null,\n              \"metadata\": {},\n              \"nickname\": null,\n              \"product\": \"prod_HoKCngRhIjfgLt\",\n              \"recurring\": {\n                \"aggregate_usage\": null,\n                \"interval\": \"month\",\n                \"interval_count\": 1,\n                \"trial_period_days\": 30,\n                \"usage_type\": \"licensed\"\n              },\n              \"tiers_mode\": null,\n              \"transform_quantity\": null,\n              \"type\": \"recurring\",\n              \"unit_amount\": 600,\n              \"unit_amount_decimal\": \"600\"\n            },\n            \"quantity\": 1,\n            \"subscription\": \"sub_HoOURrrvyzUi6K\",\n            \"tax_rates\": []\n          }\n        ],\n        \"has_more\": false,\n        \"total_count\": 1,\n        \"url\": \"/v1/subscription_items?subscription=sub_HoOURrrvyzUi6K\"\n      },\n      \"latest_invoice\": \"in_1HElhEFNS9zyw2T7p4ozSWvE\",\n      \"livemode\": false,\n      \"metadata\": {},\n      \"next_pending_invoice_item_invoice\": null,\n      \"pause_collection\": null,\n      \"pending_invoice_item_interval\": null,\n      \"pending_setup_intent\": null,\n      \"pending_update\": null,\n      \"plan\": {\n        \"id\": \"price_1HEhXfFNS9zyw2T7YKMUaXSj\",\n        \"object\": \"plan\",\n        \"active\": true,\n        \"aggregate_usage\": null,\n        \"amount\": 600,\n        \"amount_decimal\": \"600\",\n        \"billing_scheme\": \"per_unit\",\n        \"created\": 1597091063,\n        \"currency\": \"usd\",\n        \"interval\": \"month\",\n        \"interval_count\": 1,\n        \"livemode\": false,\n        \"metadata\": {},\n        \"nickname\": null,\n        \"product\": \"prod_HoKCngRhIjfgLt\",\n        \"tiers\": null,\n        \"tiers_mode\": null,\n        \"transform_usage\": null,\n        \"trial_period_days\": 30,\n        \"usage_type\": \"licensed\"\n      },\n      \"quantity\": 1,\n      \"schedule\": null,\n      \"start_date\": 1597107032,\n      \"status\": \"trialing\",\n      \"tax_percent\": null,\n      \"transfer_data\": null,\n      \"trial_end\": 1599699031,\n      \"trial_start\": 1597107032\n    }\n  },\n  \"livemode\": false,\n  \"pending_webhooks\": 0,\n  \"request\": {\n    \"id\": \"req_LAb964lMnL9WfN\",\n    \"idempotency_key\": \"5883484b-1cbb-4b7d-9d9c-d1650895fcf2\"\n  },\n  \"type\": \"customer.subscription.updated\"\n}";

            // POST, missing signature
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Method = HttpMethod.Post;
            request.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, bad signature
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Method = HttpMethod.Post;
            request.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            request.Headers.Add("Stripe-Signature", "t=1492774577,v1=5257a869e7ecebeda32affa62cdca3fa51cad7e77a0e56ff536d0ce8e108d8bd,v0=6ffbb59b2300aae63f272406069a9788598b792a944a07aba816edb039989a39");
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }
    }
}