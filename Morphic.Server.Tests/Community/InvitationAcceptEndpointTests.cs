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

    public class InvitationAcceptEndpointTests : EndpointRequestTests
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
        public async Task TestPost()
        {

            await CreateCommunity();

            // POST, unauth
            var path = $"/v1/communities/{Community.Id}/invitations/{Invitation.Id}/accept";
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", InvitedUserInfo.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}