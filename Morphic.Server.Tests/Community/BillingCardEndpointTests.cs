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
using Microsoft.Extensions.DependencyInjection;

namespace Morphic.Server.Tests.Community
{

    using Server.Community;
    using Server.Billing;
    using Billing;

    public class BillingCardEndpointTests : EndpointRequestTests
    {

        private Community Community;
        private BillingRecord Billing;
        private UserInfo ManagerUserInfo;
        private UserInfo OtherManagerUserInfo;
        private UserInfo ActiveUserInfo;
        private UserInfo InvitedUserInfo;
        private Member Manager;
        private Member OtherManager;
        private Member ActiveMember;
        private Member InvitedMember;
        private Bar DefaultBar;

        private async Task CreateCommunity(){
            ManagerUserInfo = await CreateTestUser();
            OtherManagerUserInfo = await CreateTestUser();
            ActiveUserInfo = await CreateTestUser();
            InvitedUserInfo = await CreateTestUser();

            var communityId = Guid.NewGuid().ToString();
            var managerId = Guid.NewGuid().ToString();
            DefaultBar = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default",
                IsShared = true,
                CommunityId = communityId,
                CreatedAt = DateTime.Now
            };
            await Database.Save(DefaultBar);

            Billing = new BillingRecord()
            {
                PlanId = Server.Services.GetRequiredService<Plans>().Default.Id,
                TrialEnd = DateTime.UnixEpoch.AddSeconds(1234567890.123),
                Status = BillingStatus.Paid,
                CommunityId = communityId,
                ContactMemeberId = managerId,
            };
            await Database.Save(Billing);

            Community = new Community()
            {
                Id = communityId,
                Name = "Test Community",
                DefaultBarId = DefaultBar.Id,
                BillingId = Billing.Id
            };
            await Database.Save(Community);

            Manager = new Member()
            {
                Id = managerId,
                CommunityId = Community.Id,
                UserId = ManagerUserInfo.Id,
                State = MemberState.Active,
                Role = MemberRole.Manager
            };
            Manager.FirstName.PlainText = "Manager";
            Manager.LastName.PlainText = "Tester";
            await Database.Save(Manager);

            OtherManager = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                UserId = OtherManagerUserInfo.Id,
                State = MemberState.Active,
                Role = MemberRole.Manager
            };
            OtherManager.FirstName.PlainText = "Other";
            OtherManager.LastName.PlainText = "Manager";
            await Database.Save(OtherManager);

            ActiveMember = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                UserId = ActiveUserInfo.Id,
                State = MemberState.Active,
                Role = MemberRole.Member,
                BarId = DefaultBar.Id
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

            // GET
            var path = $"/v1/communities/{Community.Id}/billing/card";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [Fact]
        public async Task TestPost()
        {
            await CreateCommunity();

            // POST, unauth
            var path = $"/v1/communities/{Community.Id}/billing/card";
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            var content = new Dictionary<string, object>();
            content.Add("token", "abc123");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // POST, incorrect content type
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("token", "abc123");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // POST, not a manager
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("token", "abc123");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // POST, not active
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", InvitedUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("token", "abc123");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // POST, missing token
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var paymentProcessor = (Server.Services.GetRequiredService<IPaymentProcessor>() as MockPaymentProcessor)!;

            Assert.Equal(0, paymentProcessor.ChangeCommunityCardCalls);

            // POST, success
            request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("token", "abc123");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, paymentProcessor.ChangeCommunityCardCalls);
        }

    }
}