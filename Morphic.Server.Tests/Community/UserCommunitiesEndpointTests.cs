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

    public class UserCommunitiesEndpointTests : EndpointRequestTests
    {

        private Community Community1;
        private Community Community2;
        private Community Community3;
        private UserInfo UserInfo1;
        private UserInfo UserInfo2;
        private UserInfo UserInfo3;

        private async Task CreateCommunity(){
            UserInfo1 = await CreateTestUser();
            UserInfo2 = await CreateTestUser();
            UserInfo3 = await CreateTestUser();

            var community1Id = Guid.NewGuid().ToString();
            var community2Id = Guid.NewGuid().ToString();
            var community3Id = Guid.NewGuid().ToString();

            var bar1 = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = community1Id
            };
            await Database.Save(bar1);
            Community1 = new Community()
            {
                Id = community1Id,
                Name = "Test Community",
                DefaultBarId = bar1.Id
            };
            await Database.Save(Community1);
            
            var bar2 = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = community2Id
            };
            await Database.Save(bar2);
            Community2 = new Community()
            {
                Id = community2Id,
                Name = "Test Community 2",
                DefaultBarId = bar2.Id
            };
            await Database.Save(Community2);
            
            var bar3 = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = community3Id
            };
            await Database.Save(bar3);
            Community3 = new Community()
            {
                Id = community3Id,
                Name = "Test Community 3",
                DefaultBarId = bar3.Id
            };
            await Database.Save(Community3);

            var member = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community1.Id,
                UserId = UserInfo1.Id,
                State = MemberState.Active,
                Role = MemberRole.Manager
            };
            await Database.Save(member);

            member = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community2.Id,
                UserId = UserInfo1.Id,
                State = MemberState.Invited,
                Role = MemberRole.Member
            };
            await Database.Save(member);

            member = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community3.Id,
                UserId = UserInfo1.Id,
                State = MemberState.Active,
                Role = MemberRole.Member
            };
            await Database.Save(member);

            member = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community1.Id,
                UserId = UserInfo2.Id,
                State = MemberState.Active,
                Role = MemberRole.Member
            };
            await Database.Save(member);

            member = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community2.Id,
                UserId = UserInfo2.Id,
                State = MemberState.Uninvited,
                Role = MemberRole.Member
            };
            await Database.Save(member);
        }

        [Fact]
        public async Task TestGet()
        {
            await CreateCommunity();

            // GET, no auth
            var path = $"/v1/users/{UserInfo1.Id}/communities";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // GET, other user
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserInfo2.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // GET, success
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserInfo1.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.True(element.TryGetProperty("communities", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(2, property.GetArrayLength());
            var elements = property.EnumerateArray().ToArray();
            element = elements[0];
            Assert.True(element.TryGetProperty("id", out property));
            Assert.True(element.TryGetProperty("name", out property));
            Assert.True(element.TryGetProperty("role", out property));

            // GET, success user 2
            path = $"/v1/users/{UserInfo2.Id}/communities";
            request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserInfo2.AuthToken);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            json = await response.Content.ReadAsStringAsync();
            document = JsonDocument.Parse(json);
            element = document.RootElement;
            Assert.True(element.TryGetProperty("communities", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            Assert.Equal(1, property.GetArrayLength());
            elements = property.EnumerateArray().ToArray();
            element = elements[0];
            Assert.True(element.TryGetProperty("id", out property));
            Assert.True(element.TryGetProperty("name", out property));
            Assert.True(element.TryGetProperty("role", out property));
        }
    }
}