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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Morphic.Server.Community{

    using Http;

    [Path("/v1/communities/{id}/members")]
    public class CommunityMembersEndpoint: Endpoint
    {

        public CommunityMembersEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            Community = await Load<Community>(Id);
            var member = await Load<Member>(m => m.CommunityId == Community.Id && m.UserId == authenticatedUser.Id);
            if (member.Role != Role.Manager){
                throw new HttpError(HttpStatusCode.Unauthorized);
            }
        }

        public Community Community = null!;

        [Method]
        public async Task Get()
        {

        }

        [Method]
        public async Task Post()
        {
            var db = Context.GetDatabase();
            var authenticatedUser = await RequireUser();
            var input = await Request.ReadJson<CommunityPutRequest>();
            var community = new Community();
            community.Id = Guid.NewGuid().ToString();
            community.Name = input.Name;
            await db.Save(community);
            var member = new Member();
            member.Id = Guid.NewGuid().ToString();
            member.CommunityId = community.Id;
            member.UserId = authenticatedUser.Id;
            await db.Save(member);
            // TODO: default bar
        }

        class CommunityPutRequest
        {
            public string Name { get; set; } = null!;
        }

        class CommunityMembersPage
        {
            [JsonPropertyName("members")]
            public List<CommunityMembersItem> Members { get; set; } = new List<CommunityMembersItem>();

            [JsonPropertyName("total")]
            public int Total { get; set; }
        }

        class CommunityMembersItem
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("first_name")]
            public string? FirstName { get; set; }

            [JsonPropertyName("last_name")]
            public string? LastName { get; set; }
        }
    }

}