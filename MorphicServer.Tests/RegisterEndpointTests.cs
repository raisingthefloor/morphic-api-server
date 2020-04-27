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
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
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
            var path = "/v1/register/username";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // POST, missing content type
            request = new HttpRequestMessage(HttpMethod.Post, path);
            var content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "testing123");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // POST, missing username
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("password", "testing123");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            JsonElement property;
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            // POST, missing password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");

            // POST, missing email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "testing123");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            // POST, blank password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            // POST, whitespace password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "              ");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "\t");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            // POST, short password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "short");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            JsonElement element;
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "short_password");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var details = property;
            JsonElement minimum_length;
            Assert.True(details.TryGetProperty("minimum_length", out minimum_length));
            Assert.Equal(6, minimum_length.GetInt16());
            
            // POST, known bad password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "password");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "bad_password");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            // POST, malformed email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "testing123");
            content.Add("email", "test1example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "malformed_email");

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "testing123");
            content.Add("email", "test1@example.com");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.True(element.TryGetProperty("token", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(element.TryGetProperty("user", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var user = property;
            Assert.True(user.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("first_name", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
            Assert.True(user.TryGetProperty("last_name", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            // POST, success with first/last name
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1fl");
            content.Add("password", "testing123");
            content.Add("email", "test1fl@example.com");
            content.Add("first_name", "Test");
            content.Add("last_name", "User");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
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
            Assert.True(user.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("first_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test", property.GetString());
            Assert.True(user.TryGetProperty("last_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("User", property.GetString());

            // POST, duplicate username
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test1");
            content.Add("password", "testing123");
            content.Add("email", "test123@example.com");
            content.Add("first_name", "Test");
            content.Add("last_name", "User");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "existing_username");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            // POST, duplicate email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "test23");
            content.Add("password", "testing123");
            content.Add("email", "test1@example.com");
            content.Add("first_name", "Test");
            content.Add("last_name", "User");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.BadRequest, "existing_email");
        }

        [Fact]
        public async Task TestRegisterKeyDisabled()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/register/key");
            request.Content = new StringContent(@"{""key"": ""testkey""}", Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Disabled until we re-enable the endpoint
        // [Fact]
        public async Task TestRegisterKey()
        {
            // GET, not supported
            var path = "/v1/register/key";
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
            JsonElement property;
            var error = await assertJsonError(response, HttpStatusCode.BadRequest, "missing_required");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
            
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
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.True(element.TryGetProperty("token", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(element.TryGetProperty("user", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var user = property;
            Assert.True(user.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("first_name", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
            Assert.True(user.TryGetProperty("last_name", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);

            // POST, success with first/last name
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""key"": ""testkey2"", ""first_name"": ""Test"", ""last_name"": ""User""}", Encoding.UTF8, JsonMediaType);
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
            Assert.True(user.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            Assert.True(user.TryGetProperty("first_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test", property.GetString());
            Assert.True(user.TryGetProperty("last_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("User", property.GetString());

            // POST, duplicate key
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""key"": ""testkey2"", ""firstName"": ""Test"", ""lastName"": ""User""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            error = await assertJsonError(response, HttpStatusCode.BadRequest, "existing_key");
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
        }

    }
}