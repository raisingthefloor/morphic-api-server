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

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Morphic.Server.Tests
{
    public class VerifyEmailRequestUnauthTests : EndpointRequestTests
    {
        [Fact]
        public async Task VerifyEmailRequestUnauth()
        {
            var userInfo1 = await CreateTestUser();
            MorphicSettings.FrontEndServerUrlPrefix = "http://foo:1234";
            var path = "/v1/user/verify_email_request";
            JobClient.Job = null;

            // Fail: missing email and recaptcha
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            var content = new Dictionary<string, object>();
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email", "g_recaptcha_response"});
            Assert.Null(JobClient.Job);
            
            // Fail: missing email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email"});
            Assert.Null(JobClient.Job);

            // Fail: blank email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", "");
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email"});
            Assert.Null(JobClient.Job);

            // Fail: bad email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", "something");
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "bad_email_address");
            Assert.Null(JobClient.Job);

            // bad captcha response
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", userInfo1.Email);
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "bad_recaptcha");
            Assert.Null(JobClient.Job);

            // Success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", userInfo1.Email);
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(JobClient.Job);
            Assert.Equal("Morphic.Server.Auth.EmailVerificationEmail", JobClient.Job.Type.FullName);
        }
    }
}