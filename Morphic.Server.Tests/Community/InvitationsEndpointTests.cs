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

    public class InvitationsEndpointTests : EndpointRequestTests
    {

        private Community Community;
        private UserInfo ManagerUserInfo;
        private UserInfo ActiveUserInfo;
        private UserInfo InvitedUserInfo;
        private Member Manager;
        private Member ActiveMember;
        private Member InvitedMember;
        private Member UninvitedMember;

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

            UninvitedMember = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                State = MemberState.Uninvited,
                Role = MemberRole.Member
            };
            InvitedMember.FirstName.PlainText = "Uninvited";
            InvitedMember.LastName.PlainText = "Person";
            await Database.Save(UninvitedMember);
        }

        [Fact]
        public async Task TestPost()
        {

            await CreateCommunity();

            JobClient.Job = null;

            // POST, unauth
            var path = $"/v1/communities/{Community.Id}/invitations";
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            var content = new Dictionary<string, object>();
            content.Add("member_id", UninvitedMember.Id);
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, incorrect content type
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("member_id", UninvitedMember.Id);
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // POST, not a manager
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("member_id", UninvitedMember.Id);
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // POST, not active
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", InvitedUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("member_id", UninvitedMember.Id);
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // POST, missing member_id
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, missing email
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, bad active member id
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("member_id", ActiveMember.Id);
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // POST, bad unknown member id
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("member_id", "abcdef");
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Null(JobClient.Job);

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("member_id", UninvitedMember.Id);
            content.Add("email", "test@morphic.org");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(JobClient.Job);
            Assert.Equal("Morphic.Server.Community.InvitationEmail", JobClient.Job.Type.FullName);

            // POST, success with message
            JobClient.Job = null;
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("member_id", UninvitedMember.Id);
            content.Add("email", "test@morphic.org");
            content.Add("message", "This is a custom message");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(JobClient.Job);
            Assert.Equal("Morphic.Server.Community.InvitationEmail", JobClient.Job.Type.FullName);
        }
    }
}