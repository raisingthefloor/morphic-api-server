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
using System.Linq;
using System.Net.Http.Headers;
using Xunit;
using System.Text.Json;
using System.Text;

namespace Morphic.Server.Tests
{
    public class AuthEndpointTests : EndpointRequestTests
    {

        [Fact]
        public async Task TestUsername()
        {
            var userInfo1 = await CreateTestUser();
            
            // GET, not supported
            var path = "/v1/auth/username";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request); Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // POST, missing content type
            var content = new Dictionary<string, object>();
            content.Add("username", $"{userInfo1.Username}");
            content.Add("password", $"{userInfo1.Password}");
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // POST, missing username
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("password", $"{userInfo1.Password}");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, missing password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", $"{userInfo1.Username}");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, unknown username
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", "testunknown");
            content.Add("password", "testwrong");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, wrong password
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", $"{userInfo1.Username}");
            content.Add("password", "testwrong");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            content = new Dictionary<string, object>();
            content.Add("username", $"{userInfo1.Username}");
            content.Add("password", $"{userInfo1.Password}");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
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
            Assert.True(user.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(user.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(user.TryGetProperty("first_name", out property));
            Assert.Equal(userInfo1.FirstName, property.GetString());
            Assert.True(user.TryGetProperty("last_name", out property));
            Assert.Equal(userInfo1.LastName, property.GetString());
            Assert.True(user.TryGetProperty("email_verified", out property));
            Assert.Equal(userInfo1.EmailVerified, property.GetBoolean());
        }

        [Fact]
        public async Task TestDeleteToken()
        {
            UserInfo userInfo = await this.CreateTestUser();

            // authenticate
            const string authPath = "/v1/auth/username";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, authPath);
            Dictionary<string,object> content = new Dictionary<string, object>
            {
                { "username", $"{userInfo.Username}" },
                { "password", $"{userInfo.Password}" }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);

            HttpResponseMessage response = await this.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // check if authenticated
            string testPath = $"/v1/users/{userInfo.Id}";
            HttpRequestMessage userRequest1 = new HttpRequestMessage(HttpMethod.Get, testPath);
            userRequest1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo.AuthToken);

            response = await Client.SendAsync(userRequest1);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // un-authenticate
            HttpRequestMessage unauthRequest = new HttpRequestMessage(HttpMethod.Delete, "/v1/auth/token");
            unauthRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo.AuthToken);

            response = await Client.SendAsync(unauthRequest);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // check if not authenticated
            HttpRequestMessage userRequest2 = new HttpRequestMessage(HttpMethod.Get, testPath);
            userRequest2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo.AuthToken);

            response = await Client.SendAsync(userRequest2);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // un-authenticate (again)
            HttpRequestMessage unauthRequestAgain = new HttpRequestMessage(HttpMethod.Delete, "/v1/auth/token");
            unauthRequestAgain.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo.AuthToken);

            response = await Client.SendAsync(unauthRequestAgain);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task TestKeyDisabled()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/register/key");
            request.Content = new StringContent(@"{""key"": ""testkey""}", Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Disabled until we re-enabled the endpoint
        [Fact (Skip = "We're not using the Key endpoints today")]
        public async Task TestKey()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/register/key");
            request.Content = new StringContent(@"{""key"": ""testkey""}", Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // GET, not supported
            var path = "/v1/auth/key";
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
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, wrong key
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(@"{""key"": ""testwrong""}", Encoding.UTF8, JsonMediaType);
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
        }

    }
}