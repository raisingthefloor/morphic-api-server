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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Linq;
using Xunit;
using System.Text.Json;
using System.Text;

namespace MorphicServer.Tests
{
    public class PreferencesEndpointTests : EndpointTests
    {

        [Fact]
        public async Task TestGet()
        {
            var userInfo1 = await CreateTestUser();
            var userInfo2 = await CreateTestUser();

            // GET, unknown, unauth
            var uuid = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, $"/preferences/{uuid}");
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // GET, unknown
            request = new HttpRequestMessage(HttpMethod.Get, $"/preferences/{uuid}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, known, unauth
            uuid = Guid.NewGuid().ToString();
            request = new HttpRequestMessage(HttpMethod.Get, $"/preferences/{userInfo1.PreferencesId}");
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // GET, known, forbidden
            uuid = Guid.NewGuid().ToString();
            request = new HttpRequestMessage(HttpMethod.Get, $"/preferences/{userInfo2.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, success
            request = new HttpRequestMessage(HttpMethod.Get, $"/preferences/{userInfo1.PreferencesId}");
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
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("Default", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
        }

        [Fact]
        public async Task TestPut()
        {
            var userInfo1 = await CreateTestUser();
            var userInfo2 = await CreateTestUser();

            // PUT, unknown, unauth
            var uuid = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{uuid}");
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": {""first"": 1, ""second"": ""two"", ""third"": 3.1, ""fourth"": true, ""fifth"": false, ""sixth"": null, ""seventh"": [1,true,null], ""eighth"": {""a"": 1, ""b"": false}}}}", Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // PUT, unknown
            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{uuid}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": {""first"": 1, ""second"": ""two"", ""third"": 3.1, ""fourth"": true, ""fifth"": false, ""sixth"": null, ""seventh"": [1,true,null], ""eighth"": {""a"": 1, ""b"": false}}}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // PUT, known, unauth
            uuid = Guid.NewGuid().ToString();
            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo1.PreferencesId}");
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": {""first"": 1, ""second"": ""two"", ""third"": 3.1, ""fourth"": true, ""fifth"": false, ""sixth"": null, ""seventh"": [1,true,null], ""eighth"": {""a"": 1, ""b"": false}}}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // PUT, known, forbidden
            uuid = Guid.NewGuid().ToString();
            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo2.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": {""first"": 1, ""second"": ""two"", ""third"": 3.1, ""fourth"": true, ""fifth"": false, ""sixth"": null, ""seventh"": [1,true,null], ""eighth"": {""a"": 1, ""b"": false}}}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // POST, not allowed
            request = new HttpRequestMessage(HttpMethod.Post, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": {""first"": 1, ""second"": ""two"", ""third"": 3.1, ""fourth"": true, ""fifth"": false, ""sixth"": null, ""seventh"": [1,true,null], ""eighth"": {""a"": 1, ""b"": false}}}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // PUT, success
            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": {""first"": 1, ""second"": ""two"", ""third"": 3.1, ""fourth"": true, ""fifth"": false, ""sixth"": null, ""seventh"": [1,true,null], ""eighth"": {""a"": 1, ""b"": false}}}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            request = new HttpRequestMessage(HttpMethod.Get, $"/preferences/{userInfo1.PreferencesId}");
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
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("UserId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(element.TryGetProperty("Default", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var defaults = property;
            Assert.True(defaults.TryGetProperty("org.raisingthefloor.solution", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var solution = property;
            Assert.True(solution.TryGetProperty("first", out property));
            Assert.Equal(JsonValueKind.Number, property.ValueKind);
            Assert.Equal(1, property.GetInt64());
            Assert.True(solution.TryGetProperty("second", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("two", property.GetString());
            Assert.True(solution.TryGetProperty("third", out property));
            Assert.Equal(JsonValueKind.Number, property.ValueKind);
            Assert.True(Math.Abs(3.1 - property.GetDouble()) < 0.001);
            Assert.True(solution.TryGetProperty("fourth", out property));
            Assert.Equal(JsonValueKind.True, property.ValueKind);
            Assert.True(solution.TryGetProperty("fifth", out property));
            Assert.Equal(JsonValueKind.False, property.ValueKind);
            Assert.True(solution.TryGetProperty("sixth", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
            Assert.True(solution.TryGetProperty("seventh", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(3, property.GetArrayLength());
            var list = property.EnumerateArray().ToList();
            Assert.Equal(JsonValueKind.Number, list[0].ValueKind);
            Assert.Equal(1, list[0].GetInt64());
            Assert.Equal(JsonValueKind.True, list[1].ValueKind);
            Assert.Equal(JsonValueKind.Null, list[2].ValueKind);
            Assert.True(solution.TryGetProperty("eighth", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var obj = property;
            Assert.True(obj.TryGetProperty("a", out property));
            Assert.Equal(JsonValueKind.Number, property.ValueKind);
            Assert.Equal(1, property.GetInt64());
            Assert.True(obj.TryGetProperty("b", out property));
            Assert.Equal(JsonValueKind.False, property.ValueKind);

            // PUT, bad solution (not an object)
            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": 1}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": ""test""}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": true}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": false}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Default"": {""org.raisingthefloor.solution"": []}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // PUT, ignored fields
            request = new HttpRequestMessage(HttpMethod.Put, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            request.Content = new StringContent(@"{""Id"": ""newid"", ""UserId"": ""newuserid"", ""Default"": {""org.raisingthefloor.solution"": {""first"": 12, ""second"": ""changed"", ""third"": 3.1, ""fourth"": true, ""fifth"": false, ""sixth"": null, ""seventh"": [1,true,null], ""eighth"": {""a"": 1, ""b"": false}}}}", Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            request = new HttpRequestMessage(HttpMethod.Get, $"/preferences/{userInfo1.PreferencesId}");
            request.Headers.Add(AuthTokenHeaderName, userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.True(element.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.PreferencesId, property.GetString());
            Assert.True(element.TryGetProperty("UserId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(userInfo1.Id, property.GetString());
            Assert.True(element.TryGetProperty("Default", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            defaults = property;
            Assert.True(defaults.TryGetProperty("org.raisingthefloor.solution", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            solution = property;
            Assert.True(solution.TryGetProperty("first", out property));
            Assert.Equal(JsonValueKind.Number, property.ValueKind);
            Assert.Equal(12, property.GetInt64());
            Assert.True(solution.TryGetProperty("second", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("changed", property.GetString());
            Assert.True(solution.TryGetProperty("third", out property));
            Assert.Equal(JsonValueKind.Number, property.ValueKind);
            Assert.True(Math.Abs(3.1 - property.GetDouble()) < 0.001);
            Assert.True(solution.TryGetProperty("fourth", out property));
            Assert.Equal(JsonValueKind.True, property.ValueKind);
            Assert.True(solution.TryGetProperty("fifth", out property));
            Assert.Equal(JsonValueKind.False, property.ValueKind);
            Assert.True(solution.TryGetProperty("sixth", out property));
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
            Assert.True(solution.TryGetProperty("seventh", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(3, property.GetArrayLength());
            list = property.EnumerateArray().ToList();
            Assert.Equal(JsonValueKind.Number, list[0].ValueKind);
            Assert.Equal(1, list[0].GetInt64());
            Assert.Equal(JsonValueKind.True, list[1].ValueKind);
            Assert.Equal(JsonValueKind.Null, list[2].ValueKind);
            Assert.True(solution.TryGetProperty("eighth", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            obj = property;
            Assert.True(obj.TryGetProperty("a", out property));
            Assert.Equal(JsonValueKind.Number, property.ValueKind);
            Assert.Equal(1, property.GetInt64());
            Assert.True(obj.TryGetProperty("b", out property));
            Assert.Equal(JsonValueKind.False, property.ValueKind);
        }

    }
}