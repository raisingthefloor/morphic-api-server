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
using System.Linq;
using Xunit;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Morphic.Server.Tests.Community
{

    using Server.Community;

    public class BarsEndpointTests : EndpointRequestTests
    {

        private Community Community;
        private UserInfo ManagerUserInfo;
        private UserInfo ActiveUserInfo;
        private UserInfo InvitedUserInfo;
        private Member Manager;
        private Member ActiveMember;
        private Member InvitedMember;

        private async Task CreateCommunity(){
            ManagerUserInfo = await CreateTestUser();
            ActiveUserInfo = await CreateTestUser();
            InvitedUserInfo = await CreateTestUser();

            var communityId = Guid.NewGuid().ToString();
            var bar = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default",
                CommunityId = communityId,
                IsShared = true,
                CreatedAt = DateTime.Now
            };
            await Database.Save(bar);

            bar = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Bar 2",
                CommunityId = communityId,
                IsShared = true,
                CreatedAt = DateTime.Now,
                Items = new BarItem[]{
                    new BarItem(){
                        Kind = BarItemKind.Link,
                        Label = "Google",
                        IsPrimary = true,
                        Configuration = new Dictionary<string, object>(){
                            { "url", "https://google.com" }
                        }
                    },
                    new BarItem(){
                        Kind = BarItemKind.Link,
                        Label = "Facebook",
                        IsPrimary = true,
                        Configuration = new Dictionary<string, object>(){
                            { "url", "https://facebook.com" }
                        }
                    }
                }
            };
            await Database.Save(bar);

            Community = new Community()
            {
                Id = communityId,
                Name = "Test Community",
                DefaultBarId = bar.Id
        };
            await Database.Save(Community);

            Manager = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                UserId = ManagerUserInfo.Id,
                State = MemberState.Active,
                Role = MemberRole.Manager
            };
            Manager.FirstName.PlainText = "Manager";
            Manager.LastName.PlainText = "Tester";
            await Database.Save(Manager);

            ActiveMember = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                UserId = ActiveUserInfo.Id,
                State = MemberState.Active,
                Role = MemberRole.Member
            };
            ActiveMember.FirstName.PlainText = "Active";
            ActiveMember.LastName.PlainText = "Member";
            await Database.Save(ActiveMember);

            InvitedMember = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                UserId = InvitedUserInfo.Id,
                State = MemberState.Invited,
                Role = MemberRole.Member
            };
            InvitedMember.FirstName.PlainText = "Invited";
            InvitedMember.LastName.PlainText = "Person";
            await Database.Save(InvitedMember);
        }

        [Fact]
        public async Task TestGet()
        {
            await CreateCommunity();

            // GET, no auth
            var path = $"/v1/communities/{Community.Id}/bars";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // GET, non manager
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, non active
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", InvitedUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // GET, success
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.True(element.TryGetProperty("bars", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(2, property.GetArrayLength());
            var elements = property.EnumerateArray().ToArray();
            element = elements[0];
            Assert.True(element.TryGetProperty("id", out property));
            Assert.True(element.TryGetProperty("name", out property));
            Assert.True(element.TryGetProperty("is_shared", out property));
        }

        [Fact]
        public async Task TestPost()
        {

            await CreateCommunity();

            // POST, unauth
            var path = $"/v1/communities/{Community.Id}/bars";
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            var content = new Dictionary<string, object>();
            content.Add("name", "Test Bar");
            content.Add("is_shared", true);
            content.Add("items", new object[] { });
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, incorrect content type
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Test Bar");
            content.Add("is_shared", true);
            content.Add("items", new object[] { });
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // POST, not a manager
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Test Bar");
            content.Add("is_shared", true);
            content.Add("items", new object[] { });
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // POST, not active
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", InvitedUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Test Bar");
            content.Add("is_shared", true);
            content.Add("items", new object[] { });
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // POST, missing name
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("is_shared", true);
            content.Add("items", new object[] { });
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, missing shared
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Test Bar");
            content.Add("items", new object[] { });
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, missing items
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Test Bar");
            content.Add("is_shared", true);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Test Bar");
            content.Add("is_shared", true);
            content.Add("items", new object[] { });
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.True(element.TryGetProperty("bar", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            element = property;
            Assert.True(element.TryGetProperty("id", out property));
            Assert.True(element.TryGetProperty("name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test Bar", property.GetString());
            Assert.True(element.TryGetProperty("is_shared", out property));
            Assert.Equal(JsonValueKind.True, property.ValueKind);
            Assert.True(element.TryGetProperty("items", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(0, property.GetArrayLength());

            // POST, success with items
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Test Bar");
            content.Add("is_shared", true);
            content.Add("items", new object[]{
                new Dictionary<string, object>(){
                    {"kind", "link"},
                    {"label", "Google"},
                    {"is_primary", true},
                    {"configuration", new Dictionary<string, object>(){
                        {"url", "https://google.com"}
                    }}
                },
                new Dictionary<string, object>(){
                    {"kind", "link"},
                    {"label", "Facebook"},
                    {"is_primary", true},
                    {"configuration", new Dictionary<string, object>(){
                        {"url", "https://facebook.com"}
                    }}
                }
            });
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.True(element.TryGetProperty("bar", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            element = property;
            Assert.True(element.TryGetProperty("id", out property));
            Assert.True(element.TryGetProperty("name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test Bar", property.GetString());
            Assert.True(element.TryGetProperty("is_shared", out property));
            Assert.Equal(JsonValueKind.True, property.ValueKind);
            Assert.True(element.TryGetProperty("items", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(2, property.GetArrayLength());
            var items = property.EnumerateArray().ToList();
            Assert.True(items[0].TryGetProperty("label", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Google", property.GetString());
            Assert.True(items[0].TryGetProperty("configuration", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            Assert.True(items[1].TryGetProperty("label", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Facebook", property.GetString());
            Assert.True(items[1].TryGetProperty("configuration", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
        }
    }
}