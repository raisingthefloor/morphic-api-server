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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Morphic.Server.Tests.Payment
{

    using Db;
    using Server.Auth;
    public class PaymentCreateEndpointTests : EndpointRequestTests
    {
        [Fact]
        public async Task TestPost()
        {
            var userInfo1 = await CreateTestUser();
            var userInfo2 = await CreateTestUser();
            JobClient.Job = null;

            // some valid content to be used throughout
            var cc = new Dictionary<string, object>();
            cc.Add("number", "1234-1234-1234-1234");
            cc.Add("cvv", "1234");
            cc.Add("expiration_date", "12/2020");
            cc.Add("zip_code", "90210");
            
            var requestContent = new Dictionary<string, object>();
            requestContent.Add("amount", 12000);
            requestContent.Add("cc", cc);

            // Post, Unknown user
            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}1234/payments");
            request.Content = new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // Post, Wrong user
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo2.Id}/payments");
            request.Content = new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // Post, Missing fields
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/payments");
            request.Content = new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"currency_code"});
            Assert.Null(JobClient.Job);

            // success
            requestContent.Add("currency_code", "usd");

            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/payments");
            request.Content = new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.True(element.TryGetProperty("transaction_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.NotNull(JobClient.Job);
            Assert.Equal("Morphic.Server.Payments.ProcessStripePayment", JobClient.Job.Type.FullName);
        }

    }
}
