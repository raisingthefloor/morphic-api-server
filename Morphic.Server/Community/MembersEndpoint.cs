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
using System.Linq;

namespace Morphic.Server.Community{

    using Http;

    [Path("/v1/communities/{id}/members")]
    public class MembersEndpoint: Endpoint
    {

        public MembersEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            Community = await Load<Community>(Id);
            var member = await Load<Member>(m => m.CommunityId == Community.Id && m.UserId == authenticatedUser.Id && m.State == MemberState.Active);
            if (member.Role != MemberRole.Manager){
                throw new HttpError(HttpStatusCode.Forbidden);
            }
        }

        public Community Community = null!;

        [Method]
        public async Task Get()
        {
            var db = Context.GetDatabase();
            var page = new CommunityMembersPage();
            var members = await db.GetEnumerable<Member>(m => m.CommunityId == Community.Id);
            foreach (var member in members)
            {
                /* for backwards compatibility:
                 * - if the database record just contained "bar_id", make sure that data is
                 *   presented as part of the "bar_ids" array in our response instead
                 */
                List<string> barIds;
                if (member.BarId != null)
                {
                    // NOTE: the database is invalid if it contains both bar_id and bar_ids entries
                    //       for this record; therefore it is proper to only return bar_id (if populated)
                    barIds = new List<string>() { member.BarId };
                }
                else 
                {
                    barIds = member.BarIds;
                }

                page.Members.Add(new CommunityMembersItem()
                {
                    Id = member.Id,
                    FirstName = member.FirstName.PlainText,
                    LastName = member.LastName.PlainText,
                    Role = member.Role,
                    BarIds = barIds,
                    State = member.State,
                    UserId = member.UserId
                });
            }
            await Respond(page);
        }

        class CommunityMembersPage
        {
            [JsonPropertyName("members")]
            public List<CommunityMembersItem> Members { get; set; } = new List<CommunityMembersItem>();
        }

        class CommunityMembersItem
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("first_name")]
            public string? FirstName { get; set; }

            [JsonPropertyName("last_name")]
            public string? LastName { get; set; }

            [JsonPropertyName("role")]
            public MemberRole Role { get; set; }

			// ** REMOVED **
        //    [JsonPropertyName("bar_id")]
        //    public string? BarId { get; set; }

            [JsonPropertyName("bar_ids")]
            public List<string> BarIds { get; set; } = new List<string>();

            [JsonPropertyName("state")]
            public MemberState State { get; set; }

            [JsonPropertyName("userId")]
            public string? UserId { get; set; }
        }

        [Method]
        public async Task Post()
        {
            if (Community.MemberLimit > 0 && Community.MemberCount >= Community.MemberLimit){
                throw new HttpError(HttpStatusCode.BadRequest, new Dictionary<string, object>()
                {
                    {"error", "limit_reached"}
                });
            }
            var input = await Request.ReadJson<MemberPostRequest>();
            var member = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                State = MemberState.Uninvited,
                Role = MemberRole.Member,
                CreatedAt = DateTime.Now
            };
            member.FirstName.PlainText = input.FirstName;
            member.LastName.PlainText = input.LastName;
            await Save(member);
            await Context.GetDatabase().Increment(Community, c => c.MemberCount, 1);
            await Respond(new MemberPostResponse()
            {
                Member = member
            });
        }

        class MemberPostRequest
        {
            [JsonPropertyName("first_name")]
            public string? FirstName { get; set; } = null!;

            [JsonPropertyName("last_name")]
            public string? LastName { get; set; } = null!;
        }

        class MemberPostResponse
        {
            [JsonPropertyName("member")]
            public Member Member { get; set; } = null!;
        }
    }

}