using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Linq;
using Xunit;
using System.Text.Json;
using System.Text;

namespace MorphicServer.Tests
{
    public class RegisterEndpointTests : EndpointTests
    {

        [Fact]
        public async Task TestRegisterUsername()
        {
            // GET, not supported
            var path = "/register/username";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
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
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, missing password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""test1""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""test1"", ""password"": ""testing""}", Encoding.UTF8, JsonMediaType);
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

            // POST, success with first/last name
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""test2"", ""password"": ""testing"", ""firstName"": ""Test"", ""lastName"": ""User""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.True(element.TryGetProperty("token", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(element.TryGetProperty("user", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            user = property;
            Assert.True(user.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("PreferencesId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("FirstName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test", property.GetString());
            Assert.True(user.TryGetProperty("LastName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("User", property.GetString());

            // POST, duplicate username
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""username"": ""test2"", ""password"": ""testing"", ""firstName"": ""Test"", ""lastName"": ""User""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.True(element.TryGetProperty("Error", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("ExistingUsername", property.GetString());
        }

        [Fact]
        public async Task TestRegisterKey()
        {
            // GET, not supported
            var path = "/register/key";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
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
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

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

            // POST, success with first/last name
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""key"": ""testkey2"", ""firstName"": ""Test"", ""lastName"": ""User""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.True(element.TryGetProperty("token", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(element.TryGetProperty("user", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            user = property;
            Assert.True(user.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("PreferencesId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("FirstName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test", property.GetString());
            Assert.True(user.TryGetProperty("LastName", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("User", property.GetString());

            // POST, duplicate key
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""key"": ""testkey2"", ""firstName"": ""Test"", ""lastName"": ""User""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.True(element.TryGetProperty("Error", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("ExistingKey", property.GetString());
        }

    }
}