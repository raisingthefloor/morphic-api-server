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
using Morphic.Server.Users;
using Xunit;

namespace Morphic.Server.Tests
{

    using Auth;

    public class PasswordResetTests : EndpointRequestTests
    {
        
        [Fact]
        public async Task ResetPasswordRequestWithUser()
        {
            var userInfo1 = await CreateTestUser();
            MorphicSettings.FrontEndServerUrlPrefix = "http://foo:1234";
            var path = "/v1/auth/username/password_reset/request";
            JobClient.Job = null;

            // Fail: missing email and recaptcha
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            var content = new Dictionary<string, object>();
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email", "g_recaptcha_response"});

            // Fail: missing email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email"});

            // Fail: blank email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", "");
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email"});

            // Fail: bad email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", "something");
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "bad_email_address");

            // bad captcha response
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", userInfo1.Email);
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "bad_recaptcha");
            
            // no emails sent so far!
            Assert.Null(JobClient.Job);

            // Success
            JobClient.Job = null;
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", userInfo1.Email);
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(JobClient.Job);
            // we no longer care about verified. It's a datapoint we save, but people can still request a reset without it.
            Assert.Equal("Morphic.Server.Auth.PasswordResetEmail", JobClient.Job.Type.FullName);

            // success with verified email (shouldn't make a difference)
            var user = await Database.Get<User>(userInfo1.Id);
            Assert.NotNull(user);
            user.EmailVerified = true;
            await Database.Save(user);
            JobClient.Job = null;
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("email", userInfo1.Email);
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(JobClient.Job);
            Assert.Equal("Morphic.Server.Auth.PasswordResetEmail", JobClient.Job.Type.FullName);
        }

        [Fact]
        public async Task ResetPasswordRequestWithoutUser()
        {
            var path = "/v1/auth/username/password_reset/request";
            JobClient.Job = null;

            // Success
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            var content = new Dictionary<string, object>();
            content.Add("email", "Somerandomemail@example.com");
            content.Add("g_recaptcha_response", MockRecaptcha.GoodResponseString);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Morphic.Server.Auth.UnknownEmailPasswordResetEmail", JobClient.Job.Type.FullName);
        }
        
        [Fact]
        public async Task ResetPassword()
        {
            var userInfo1 = await CreateTestUser();
            var token = new OneTimeToken(userInfo1.Id);
            await Database.Save(token);
            var pathPrefix = "/v1/auth/username/password_reset";
            JobClient.Job = null;

            // Fail: missing password
            var request = new HttpRequestMessage(HttpMethod.Post, $"{pathPrefix}/{token.GetUnhashedToken()}");
            var content = new Dictionary<string, object>();
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"new_password"});
            
            // Fail: crappy passwords
            request = new HttpRequestMessage(HttpMethod.Post, $"{pathPrefix}/{token.GetUnhashedToken()}");
            content = new Dictionary<string, object>();
            content.Add("new_password", "password");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "bad_password");

            request = new HttpRequestMessage(HttpMethod.Post, $"{pathPrefix}/{token.GetUnhashedToken()}");
            content = new Dictionary<string, object>();
            content.Add("new_password", "");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"new_password"});
            Assert.Null(JobClient.Job);

            // Success
            request = new HttpRequestMessage(HttpMethod.Post, $"{pathPrefix}/{token.GetUnhashedToken()}");
            content = new Dictionary<string, object>();
            content.Add("new_password", "something new");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(JobClient.Job);
            Assert.Equal("Morphic.Server.Auth.ChangePasswordEmail", JobClient.Job.Type.FullName);
        }
    }
}