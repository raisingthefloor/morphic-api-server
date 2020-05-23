using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MorphicServer.Tests
{
    public class ChangePasswordEndpointTests : EndpointTests
    {
        [Fact]
        public async Task TestGet()
        {
            var userInfo1 = await CreateTestUser();
            var userInfo2 = await CreateTestUser();
            
            // GET, Unknown user
            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}12334/password");
            var content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, Wrong user
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo2.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, right user user, missing fields
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            JsonElement property;
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"existing_password"});
            
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"new_password"});

            // Missing password
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", "");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            assertMissingRequired(error, new List<string> {"new_password"});

            // bad password
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", "password");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "bad_password");

            // GET, Success
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // GET, Change again with delete-tokens
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password+"12345");
            content.Add("new_password", userInfo1.Password);
            content.Add("delete_existing_tokens", true);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // GET, token was deleted. Get 401
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            content.Add("delete_existing_tokens", true);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestLockout()
        {
            var userInfo1 = await CreateTestUser();

            Assert.Null(await BadPasswordLockout.UserLockedOut(Database, userInfo1.Id));
            
            var lockedOut = await BadPasswordLockout.BadAuthAttempt(Database, userInfo1.Id);
            Assert.False(lockedOut);
            
            lockedOut = await BadPasswordLockout.BadAuthAttempt(Database, userInfo1.Id);
            Assert.False(lockedOut);
            
            lockedOut = await BadPasswordLockout.BadAuthAttempt(Database, userInfo1.Id);
            Assert.False(lockedOut);
            
            lockedOut = await BadPasswordLockout.BadAuthAttempt(Database, userInfo1.Id);
            Assert.False(lockedOut);
            
            // GET, Success; not locked out
            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            var content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            lockedOut = await BadPasswordLockout.BadAuthAttempt(Database, userInfo1.Id);
            Assert.True(lockedOut);
            
            // GET, Success; not locked out
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password+"12345");
            content.Add("new_password", userInfo1.Password);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await assertJsonError(response, HttpStatusCode.BadRequest, "locked");
        }
    }
}