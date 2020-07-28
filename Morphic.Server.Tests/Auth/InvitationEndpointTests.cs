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

namespace Morphic.Server.Tests.Auth
{

    using Server.Community;

    public class InvitationEndpointTests : EndpointRequestTests
    {

        private Community Community;
        private UserInfo ManagerUserInfo;
        private UserInfo ActiveUserInfo;
        private UserInfo InvitedUserInfo;
        private Member Manager;
        private Member ActiveMember;
        private Member InvitedMember;
        private Member UninvitedMember;
        private Invitation Invitation;

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
                        IsPrimary = true,
                        Configuration = new Dictionary<string, object>(){
                            { "url", "https://google.com"},
                            {"label", "Google" }
                        }
                    },
                    new BarItem(){
                        Kind = BarItemKind.Link,
                        IsPrimary = true,
                        Configuration = new Dictionary<string, object>(){
                            { "url", "https://facebook.com"},
                            { "label", "Facebook" }
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

            Invitation = new Invitation()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                MemberId = InvitedMember.Id,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(14),
                SentAt = DateTime.Now
            };
            Invitation.Email.PlainText = "test@morphic.org";
            await Database.Save(Invitation);
        }

        [Fact]
        public async Task TestGet()
        {

            await CreateCommunity();

            // GET, unauth
            var path = $"/v1/invitations/{Invitation.Id}";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.True(element.TryGetProperty("email", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("test@morphic.org", property.GetString());
            Assert.True(element.TryGetProperty("first_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Invited", property.GetString());
            Assert.True(element.TryGetProperty("last_name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Person", property.GetString());
            Assert.True(element.TryGetProperty("community", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            element = property;
            Assert.True(element.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(Community.Id, property.GetString());
            Assert.True(element.TryGetProperty("name", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("Test Community", property.GetString());
        }
    }
}