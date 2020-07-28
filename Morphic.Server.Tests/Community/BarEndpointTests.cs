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

    public class BarEndpointTests : EndpointRequestTests
    {

        private Community Community;
        private UserInfo ManagerUserInfo;
        private UserInfo ActiveUserInfo;
        private UserInfo InvitedUserInfo;
        private Member Manager;
        private Member ActiveMember;
        private Member InvitedMember;
        private Bar DefaultBar;
        private Bar UsedBar;
        private Bar UnusedBar;

        private async Task CreateCommunity(){
            ManagerUserInfo = await CreateTestUser();
            ActiveUserInfo = await CreateTestUser();
            InvitedUserInfo = await CreateTestUser();

            var communityId = Guid.NewGuid().ToString();
            DefaultBar = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default",
                IsShared = true,
                CommunityId = communityId,
                CreatedAt = DateTime.Now
            };
            await Database.Save(DefaultBar);

            UsedBar = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Bar 2",
                CommunityId = communityId,
                IsShared = true,
                CreatedAt = DateTime.Now,
                Items = new BarItem[]{
                    new BarItem(){
                        Kind = BarItemKind.Link,
                        IsPrimary = true,
                        Configuration = new Dictionary<string, object>(){
                            { "url", "https://google.com" },
                            { "label", "Google" }
                        }
                    },
                    new BarItem(){
                        Kind = BarItemKind.Link,
                        IsPrimary = true,
                        Configuration = new Dictionary<string, object>(){
                            { "url", "https://facebook.com" },
                            { "label", "Facebook" }
                        }
                    }
                }
            };
            await Database.Save(UsedBar);

            UnusedBar = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Bar 3",
                CommunityId = communityId,
                IsShared = true,
                CreatedAt = DateTime.Now,
                Items = new BarItem[]{
                    new BarItem(){
                        Kind = BarItemKind.Link,
                        IsPrimary = true,
                        Configuration = new Dictionary<string, object>(){
                            { "url", "https://google.com" },
                            { "label", "Google" }
                        }
                    },
                    new BarItem(){
                        Kind = BarItemKind.Link,
                        IsPrimary = true,
                        Configuration = new Dictionary<string, object>(){
                            { "url", "https://facebook.com" },
                            { "label", "Facebook" }
                        }
                    }
                }
            };
            await Database.Save(UnusedBar);

            Community = new Community()
            {
                Id = communityId,
                Name = "Test Community",
                DefaultBarId = DefaultBar.Id
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
                Role = MemberRole.Member,
                BarId = UsedBar.Id
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

            // GET, not authorized
            var path = $"/v1/communities/{Community.Id}/bars/{UsedBar.Id}";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // GET, not manager
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // GET, not active
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
            Assert.True(element.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(UsedBar.Id, property.GetString());
            Assert.True(element.TryGetProperty("name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Bar 2", property.GetString());
            Assert.True(element.TryGetProperty("is_shared", out property));
            Assert.Equal(JsonValueKind.True, property.ValueKind);
            Assert.True(element.TryGetProperty("items", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(2, property.GetArrayLength());
            var items = property.EnumerateArray().ToList();
        }

        [Fact]
        public async Task TestPut()
        {
            await CreateCommunity();
            var items = new object[]{
                new Dictionary<string, object>(){
                    {"kind", "link"},
                    {"is_primary", true},
                    {"configuration", new Dictionary<string, object>(){
                        {"url", "https://google.com"},
                        {"label", "Google"}
                    }}
                },
                new Dictionary<string, object>(){
                    {"kind", "link"},
                    {"is_primary", true},
                    {"configuration", new Dictionary<string, object>(){
                        {"url", "https://facebook.com"},
                        {"label", "Facebook"}
                    }}
                }
            };

            // PUT, unauth
            var path = $"/v1/communities/{Community.Id}/bars/{UsedBar.Id}";
            var request = new HttpRequestMessage(HttpMethod.Put, path);
            var content = new Dictionary<string, object>();
            content.Add("name", "Changed");
            content.Add("is_shared", true);
            content.Add("items", items);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // PUT, incorrect content type
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Changed");
            content.Add("is_shared", true);
            content.Add("items", items);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // PUT, not a manager
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Changed");
            content.Add("is_shared", true);
            content.Add("items", items);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // PUT, not active
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", InvitedUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Changed");
            content.Add("is_shared", true);
            content.Add("items", items);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // PUT, missing name
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("is_shared", true);
            content.Add("items", items);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // PUT, missing shared
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Changed");
            content.Add("items", items);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // PUT, missing items
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Changed");
            content.Add("is_shared", true);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // PUT, success
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Changed");
            content.Add("is_shared", true);
            content.Add("items", items);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // PUT, can't make default un-shared
            path = $"/v1/communities/{Community.Id}/bars/{DefaultBar.Id}";
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("name", "Changed");
            content.Add("is_shared", false);
            content.Add("items", items);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TestDelete()
        {
            await CreateCommunity();

            // DELETE, unauth
            var path = $"/v1/communities/{Community.Id}/bars/{UnusedBar.Id}";
            var request = new HttpRequestMessage(HttpMethod.Delete, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // DELETE, not a manager
            request = new HttpRequestMessage(HttpMethod.Delete, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // DELETE, not active
            request = new HttpRequestMessage(HttpMethod.Delete, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", InvitedUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // DELETE, success
            request = new HttpRequestMessage(HttpMethod.Delete, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // DELETE, can't delete used
            path = $"/v1/communities/{Community.Id}/bars/{UsedBar.Id}";
            request = new HttpRequestMessage(HttpMethod.Delete, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // DELETE, can't delete default
            path = $"/v1/communities/{Community.Id}/bars/{DefaultBar.Id}";
            request = new HttpRequestMessage(HttpMethod.Delete, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}