using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MorphicServer.Tests
{
    public class PasswordResetTests : EndpointTests
    {
        [Fact]
        public async Task ResetPasswordRequestHeaderTests()
        {
            var userInfo1 = await CreateTestUser(verifiedEmail:true);

            // Success using x-forwarded headers
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/request");
            var content = new Dictionary<string, object>();
            content.Add("email", userInfo1.Email);
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            MorphicSettings.ServerUrlPrefix = "";
            request.Headers.Add("X-Forwarded-Host", new List<string> {"foo"});
            request.Headers.Add("X-Forwarded-Port", new List<string> {"443"});
            request.Headers.Add("X-Forwarded-Proto", new List<string> {"https"});
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Success using MorphicSettings.ServerUrlPrefix
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/request");
            content = new Dictionary<string, object>();
            content.Add("email", userInfo1.Email);
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            MorphicSettings.ServerUrlPrefix = "http://foo:1234";
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        
        [Fact]
        public async Task ResetPasswordRequestWithUser()
        {
            var userInfo1 = await CreateTestUser();
            MorphicSettings.ServerUrlPrefix = "http://foo:1234";

            // Fail: missing email and recaptcha
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/request");
            var content = new Dictionary<string, object>();
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email", "g_recaptcha_response"});

            // Fail: missing email
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/request");
            content = new Dictionary<string, object>();
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email"});

            // Fail: blank email
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/request");
            content = new Dictionary<string, object>();
            content.Add("email", "");
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"email"});

            // Fail: bad email
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/request");
            content = new Dictionary<string, object>();
            content.Add("email", "something");
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "bad_email_address");

            // Success
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/request");
            content = new Dictionary<string, object>();
            content.Add("email", userInfo1.Email);
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ResetPasswordRequestWithoutUser()
        {
            // Success
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/request");
            var content = new Dictionary<string, object>();
            content.Add("email", "Somerandomemail@example.com");
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        
        [Fact]
        public async Task ResetPassword()
        {
            var userInfo1 = await CreateTestUser();
            var token = new OneTimeToken(userInfo1.Id);
            await Database.Save(token);
            
            // Fail: missing password and recaptcha
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/{token.GetUnhashedToken()}");
            var content = new Dictionary<string, object>();
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"new_password", "g_recaptcha_response"});

            // Fail: missing password
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/{token.GetUnhashedToken()}");
            content = new Dictionary<string, object>();
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"new_password"});

            // Fail: crappy passwords
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/{token.GetUnhashedToken()}");
            content = new Dictionary<string, object>();
            content.Add("new_password", "password");
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "bad_password");

            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/{token.GetUnhashedToken()}");
            content = new Dictionary<string, object>();
            content.Add("new_password", "");
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"new_password"});

            // Success
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/username/password_reset/{token.GetUnhashedToken()}");
            content = new Dictionary<string, object>();
            content.Add("new_password", "something new");
            content.Add("g_recaptcha_response", "12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}