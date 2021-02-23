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

    public class BillingEndpointTests : EndpointRequestTests
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

            // GET, not authorized
            var path = $"/v1/communities/{Community.Id}/billing";
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
            Assert.Equal(Billing.Id, property.GetString());
            Assert.True(element.TryGetProperty("plan_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("testplan1", property.GetString());

            Assert.True(element.TryGetProperty("trial_end", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            // Check the date looks like a date.
            DateTime.TryParse(property.GetString(), out DateTime date);

            Assert.True(element.TryGetProperty("status", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal("paid", property.GetString());
        }

        [Fact]
        public async Task TestPut()
        {
            await CreateCommunity();

            // PUT, unauth
            var path = $"/v1/communities/{Community.Id}/billing";
            var request = new HttpRequestMessage(HttpMethod.Put, path);
            var content = new Dictionary<string, object>();
            content.Add("plan_id", "testplan1");
            content.Add("contact_member_id", Manager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // PUT, incorrect content type
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("plan_id", "testplan1");
            content.Add("contact_member_id", Manager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

            // PUT, not a manager
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("plan_id", "testplan1");
            content.Add("contact_member_id", Manager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // PUT, not active
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", InvitedUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("plan_id", "testplan1");
            content.Add("contact_member_id", Manager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // PUT, missing plan
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("contact_member_id", Manager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // PUT, missing contact
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("contact_member_id", Manager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // PUT, bad plan
            path = $"/v1/communities/{Community.Id}/billing";
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("plan_id", "testplanbad");
            content.Add("contact_member_id", Manager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // PUT, bad member (notfound)
            path = $"/v1/communities/{Community.Id}/billing";
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("plan_id", "testplan1");
            content.Add("contact_member_id", "badmember");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // PUT, bad member (not a manager)
            path = $"/v1/communities/{Community.Id}/billing";
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("plan_id", "testplan1");
            content.Add("contact_member_id", ActiveMember.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var paymentProcessor = (Server.Services.GetRequiredService<IPaymentProcessor>() as MockPaymentProcessor)!;

            Assert.Equal(0, paymentProcessor.ChangeCommunitySubscriptionCalls);
            Assert.Equal(0, paymentProcessor.ChangeCommunityContactCalls);

            // PUT, success, change plan
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("plan_id", "testplan2");
            content.Add("contact_member_id", Manager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, paymentProcessor.ChangeCommunitySubscriptionCalls);
            Assert.Equal(0, paymentProcessor.ChangeCommunityContactCalls);

            // PUT, success, change contact
            request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagerUserInfo.AuthToken);
            content = new Dictionary<string, object>();
            content.Add("plan_id", "testplan2");
            content.Add("contact_member_id", OtherManager.Id);
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, paymentProcessor.ChangeCommunitySubscriptionCalls);
            Assert.Equal(1, paymentProcessor.ChangeCommunityContactCalls);
        }

    }
}