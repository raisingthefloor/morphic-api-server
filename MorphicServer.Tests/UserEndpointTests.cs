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

using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{uuid}");
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Bearer", response.Headers.WwwAuthenticate.First().Scheme);

            // GET, unknown
            request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{uuid}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, known, unauth
            request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{userInfo1.Id}");
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Bearer", response.Headers.WwwAuthenticate.First().Scheme);

            // GET, known, forbidden
            request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{userInfo2.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, success
            request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{userInfo1.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.True(element.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(element.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("first_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test", property.GetString());
            Assert.True(element.TryGetProperty("last_name", out property));
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
            var request = new HttpRequestMessage(HttpMethod.Put, $"/v1/users/{uuid}");
            request.Content = new StringContent(@"{""first_name"": ""Changed"", ""last_name"": ""Value""}", Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Bearer", response.Headers.WwwAuthenticate.First().Scheme);

            // PUT, unknown
            request = new HttpRequestMessage(HttpMethod.Put, $"/v1/users/{uuid}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            request.Content = new StringContent(@"{""first_name"": ""Changed"", ""last_name"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // PUT, known, unauth
            request = new HttpRequestMessage(HttpMethod.Put, $"/v1/users/{userInfo1.Id}");
            request.Content = new StringContent(@"{""first_name"": ""Changed"", ""last_name"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Bearer", response.Headers.WwwAuthenticate.First().Scheme);

            // PUT, known, forbidden
            request = new HttpRequestMessage(HttpMethod.Put, $"/v1/users/{userInfo2.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            request.Content = new StringContent(@"{""first_name"": ""Changed"", ""last_name"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // POST, not allowed
            request = new HttpRequestMessage(HttpMethod.Post, $"/v1/users/{userInfo1.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            request.Content = new StringContent(@"{""first_name"": ""Changed"", ""last_name"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // PUT, success
            request = new HttpRequestMessage(HttpMethod.Put, $"/v1/users/{userInfo1.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            request.Content = new StringContent(@"{""first_name"": ""Changed"", ""last_name"": ""Value""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{userInfo1.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.True(element.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(element.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("first_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Changed", property.GetString());
            Assert.True(element.TryGetProperty("last_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Value", property.GetString());

            // PUT, ignored fields
            request = new HttpRequestMessage(HttpMethod.Put, $"/v1/users/{userInfo1.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            request.Content = new StringContent(@"{""first_name"": ""Changed"", ""last_name"": ""Again"", ""id"": ""newid"", ""preferences_id"": ""newprefsid""}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{userInfo1.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.True(element.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(element.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("first_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Changed", property.GetString());
            Assert.True(element.TryGetProperty("last_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Again", property.GetString());
        }
        
        [Fact]
        public void TestFullname()
        {
            var user = new User();
            user.FirstName = null;
            user.LastName = null;
            Assert.Equal("", user.FullName);

            user.FirstName = "foo";
            Assert.Equal("foo", user.FullName);

            user.FirstName = null;
            user.LastName = "bar";
            Assert.Equal("bar", user.FullName);

            user.FirstName = "foo";
            user.LastName = "bar";
            Assert.Equal("foo bar", user.FullName);
        }

    }
}