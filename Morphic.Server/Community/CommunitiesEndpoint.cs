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
using System.Text.Json.Serialization;

namespace Morphic.Server.Community
{

    using Http;
    using Users;

    [Path("/v1/communities")]
    public class CommunitiesEndpoint: Endpoint
    {

        public CommunitiesEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        public override async Task LoadResource()
        {
            User = await RequireUser();
        }

        private User User = null!;

        [Method]
        public async Task Post()
        {
            var db = Context.GetDatabase();
            var input = await Request.ReadJson<CommunityPutRequest>();
            var communityId = Guid.NewGuid().ToString();

            var bar = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default",
                CommunityId = communityId,
                CreatedAt = DateTime.Now,
                IsShared = true
            };
            await db.Save(bar);

            var community = new Community()
            {
                Id = communityId,
                Name = input.Name,
                DefaultBarId = bar.Id,
                CreatedAt = DateTime.Now
            };
            await db.Save(community);

            var member = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = community.Id,
                UserId = User.Id,
                Role = MemberRole.Manager,
                State = MemberState.Active,
                CreatedAt = DateTime.Now
            };
            member.FirstName.PlainText = User.FirstName?.PlainText;
            member.LastName.PlainText = User.LastName?.PlainText;
            await db.Save(member);

            await Respond(new CommunityPutResponse()
            {
                Community = community
            });
        }

        class CommunityPutRequest
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;
        }

        class CommunityPutResponse
        {
            [JsonPropertyName("community")]
            public Community Community { get; set; } = null!;
        }
    }

}