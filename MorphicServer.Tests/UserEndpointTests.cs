using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Linq;
using Xunit;
using System.Text.Json;
using System.Text;

namespace MorphicServer.Tests
{
    public class UserEndpointTests : EndpointTests
    {

        [Fact]
        public async Task TestGet()
        {
            var userInfo1 = await CreateTestUser();
            var userInfo2 = await CreateTestUser();

            // GET, unknown, unauth
            var uuid = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, $"/users/{uuid}");
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // GET, unknown
            request = new HttpRequestMessage(HttpMethod.Get, $"/users/{uuid}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, known, unauth
            uuid = Guid.NewGuid().ToString();
            request = new HttpRequestMessage(HttpMethod.Get, $"/users/{userInfo1.Id}");
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // GET, known, forbidden
            uuid = Guid.NewGuid().ToString();
            request = new HttpRequestMessage(HttpMethod.Get, $"/users/{userInfo2.Id}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, success
            request = new HttpRequestMessage(HttpMethod.Get, $"/users/{userInfo1.Id}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.True(element.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(element.TryGetProperty("PreferencesId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("FirstName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test", property.GetString());
            Assert.True(element.TryGetProperty("LastName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("User", property.GetString());
        }

        [Fact]
        public async Task TestPut()
        {
            var userInfo1 = await CreateTestUser();
            var userInfo2 = await CreateTestUser();

            // PUT, unknown, unauth
            var uuid = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Put, $"/users/{uuid}");
            request.Content = new StringContent(@"{""FirstName"": ""Changed"", ""LastName"": ""Value""}", Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // PUT, unknown
            request = new HttpRequestMessage(HttpMethod.Put, $"/users/{uuid}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""FirstName"": ""Changed"", ""LastName"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // PUT, known, unauth
            uuid = Guid.NewGuid().ToString();
            request = new HttpRequestMessage(HttpMethod.Put, $"/users/{userInfo1.Id}");
            request.Content = new StringContent(@"{""FirstName"": ""Changed"", ""LastName"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // PUT, known, forbidden
            uuid = Guid.NewGuid().ToString();
            request = new HttpRequestMessage(HttpMethod.Put, $"/users/{userInfo2.Id}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""FirstName"": ""Changed"", ""LastName"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // POST, not allowed
            request = new HttpRequestMessage(HttpMethod.Post, $"/users/{userInfo1.Id}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""FirstName"": ""Changed"", ""LastName"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // PUT, success
            request = new HttpRequestMessage(HttpMethod.Put, $"/users/{userInfo1.Id}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""FirstName"": ""Changed"", ""LastName"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            request = new HttpRequestMessage(HttpMethod.Get, $"/users/{userInfo1.Id}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.True(element.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(element.TryGetProperty("PreferencesId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("FirstName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Changed", property.GetString());
            Assert.True(element.TryGetProperty("LastName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Value", property.GetString());

            // PUT, ingored fields
            request = new HttpRequestMessage(HttpMethod.Put, $"/users/{userInfo1.Id}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""FirstName"": ""Changed"", ""LastName"": ""Again"", ""Id"": ""newid"", ""PreferencesId"": ""newprefsid""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            request = new HttpRequestMessage(HttpMethod.Get, $"/users/{userInfo1.Id}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.True(element.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(element.TryGetProperty("PreferencesId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("FirstName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Changed", property.GetString());
            Assert.True(element.TryGetProperty("LastName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Again", property.GetString());
        }

    }
}