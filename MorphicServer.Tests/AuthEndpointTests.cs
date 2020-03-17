using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Linq;
using Xunit;
using System.Text.Json;
using System.Text;

namespace MorphicServer.Tests
{
    public class AuthEndpointTests : EndpointTests
    {

        [Fact]
        public async Task TestUsername()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/register/username");
            request.Content = new StringContent(@"{""username"": ""test"", ""password"": ""testing""}", Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // GET, not supported
            var path = "/auth/username";
            request = new HttpRequestMessage(HttpMethod.Get, path);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // POST, missing content type
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""test1"", ""password"": ""testing""}", Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // POST, missing username
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""password"": ""testing""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, missing password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""test""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, unknown username
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""testunknown"", ""password"": ""testwrong""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, wrong password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""test"", ""password"": ""testwrong""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""test"", ""password"": ""testing""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.True(element.TryGetProperty("token", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(element.TryGetProperty("user", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var user = property;
            Assert.True(user.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("PreferencesId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("FirstName", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
            Assert.True(user.TryGetProperty("LastName", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
        }

        [Fact]
        public async Task TestRegisterKey()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/register/key");
            request.Content = new StringContent(@"{""key"": ""testkey""}", Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // GET, not supported
            var path = "/auth/key";
            request = new HttpRequestMessage(HttpMethod.Get, path);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // POST, missing content type
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""key"": ""testkey""}", Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // POST, missing key
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, wrong key
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""key"": ""testwrong""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""key"": ""testkey""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.True(element.TryGetProperty("token", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(element.TryGetProperty("user", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var user = property;
            Assert.True(user.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("PreferencesId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("FirstName", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
            Assert.True(user.TryGetProperty("LastName", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
        }

    }
}