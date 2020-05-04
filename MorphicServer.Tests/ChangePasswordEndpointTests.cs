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
            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}12334/changePassword");
            var content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, Wrong user
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo2.Id}/changePassword");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, right user user, missing fields
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/changePassword");
            content = new Dictionary<string, object>();
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            JsonElement property;
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            Assert.True(property.TryGetProperty("required", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(1, property.GetArrayLength());
            Assert.Equal("existing_password", property[0].GetString());
            
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/changePassword");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            Assert.True(property.TryGetProperty("required", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(1, property.GetArrayLength());
            Assert.Equal("new_password", property[0].GetString());

            // GET, Success
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/changePassword");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}