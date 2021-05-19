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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Morphic.Server.Community
{
    using Http;
    using Users;

    [Path("/v1/users/{userId}/communities/{id}")]
    public class UserCommunityEndpoint: Endpoint
    {

        public UserCommunityEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        [Parameter]
        public string UserId = "";

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            User = await Load<User>(UserId);
            if (authenticatedUser.Id != User.Id){
                throw new HttpError(HttpStatusCode.NotFound);
            }
            Member = await Load<Member>(m => m.CommunityId == Id && m.UserId == authenticatedUser.Id && m.State == MemberState.Active);
            Community = await Load<Community>(Id);
            //
            string? barId;
            if (Member.BarIds.Count > 0) 
            {
                barId = Member.BarIds[0];
            }
            else if (Member.BarId != null) 
            {
                // for backwards-compatibility
                barId = Member.BarId;
            }
            else 
            {
                barId = Community.DefaultBarId;
            }
            Bar = await Load<Bar>(barId);
        }

        public User User = null!;
        public Member Member = null!;
        public Community Community = null!;
        public Bar Bar = null!;

        [Method]
        public async Task Get(){
            if (Community.IsMemberLocked)
            {
                throw new HttpError(HttpStatusCode.BadRequest, new Dictionary<string, object>()
                {
                    {"error", "community_locked"}
                });
            }
            await Respond(new UserCommunity()
            {
                Id = Community.Id,
                Name = Community.Name,
                Bar = Bar
            });
        }

        private class UserCommunity
        {

            [JsonPropertyName("id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;

            [JsonPropertyName("bar")]
            public Bar Bar { get; set; } = null!;

        }
    }

}