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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Morphic.Server.Tests
{

    using Db;
    using Server.Auth;

    public class ChangePasswordEndpointTests : EndpointRequestTests
    {
        [Fact]
        public async Task TestPost()
        {
            var userInfo1 = await CreateTestUser();
            var userInfo2 = await CreateTestUser();
            JobClient.Job = null;

            // Post, Unknown user
            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}12334/password");
            var content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // Post, Wrong user
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo2.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // Post, right user user, missing fields
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
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

            // POST, Success
            Assert.Null(JobClient.Job);
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(JobClient.Job);
            Assert.Equal("Morphic.Server.Auth.ChangePasswordEmail", JobClient.Job.Type.FullName);

            // Post, Change again with delete-tokens
            JobClient.Job = null;
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password+"12345");
            content.Add("new_password", userInfo1.Password);
            content.Add("delete_existing_tokens", true);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(JobClient.Job);
            Assert.Equal("Morphic.Server.Auth.ChangePasswordEmail", JobClient.Job.Type.FullName);

            // Post, token was deleted. Get 401
            JobClient.Job = null;
            request = new HttpRequestMessage(HttpMethod.Post, $"v1/users/{userInfo1.Id}/password");
            content = new Dictionary<string, object>();
            content.Add("existing_password", userInfo1.Password);
            content.Add("new_password", userInfo1.Password+"12345");
            content.Add("delete_existing_tokens", true);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(JobClient.Job);
        }

        [Fact]
        public async Task TestLockout()
        {
            var userInfo1 = await CreateTestUser();

            Assert.Null(await Database.UserLockedOut(userInfo1.Id));
            
            var lockedOut = await Database.BadPasswordAuthAttempt(userInfo1.Id);
            Assert.False(lockedOut);
            
            lockedOut = await Database.BadPasswordAuthAttempt(userInfo1.Id);
            Assert.False(lockedOut);
            
            lockedOut = await Database.BadPasswordAuthAttempt(userInfo1.Id);
            Assert.False(lockedOut);
            
            lockedOut = await Database.BadPasswordAuthAttempt(userInfo1.Id);
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

            lockedOut = await Database.BadPasswordAuthAttempt(userInfo1.Id);
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