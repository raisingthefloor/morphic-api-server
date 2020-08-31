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

namespace Morphic.Server.Community{

    using Http;
    using Users;

    [Path("/v1/users/{id}/communities")]
    public class UserCommunitiesEndpoint: Endpoint
    {

        public UserCommunitiesEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            User = await Load<User>(Id);
            if (authenticatedUser.Id != User.Id){
                throw new HttpError(HttpStatusCode.NotFound);
            }
            var db = Context.GetDatabase();
            var members = await db.GetEnumerable<Member>(m => m.UserId == authenticatedUser.Id && m.State == MemberState.Active, ActiveSession);
            var communities = new List<(Community, Member)>();
            foreach (var member in members)
            {
                var community = await db.Get<Community>(member.CommunityId, ActiveSession);
                if (community is Community community_)
                {
                    communities.Add((community_, member));
                }
            }
            Communities = communities.ToArray();
        }

        public User User = null!;
        public (Community, Member)[] Communities = null!;

        [Method]
        public async Task Get(){
            var collection = new UserCommunityCollection();
            foreach (var pair in Communities){
                if (pair.Item1.IsMemberLocked)
                {
                    continue;
                }
                collection.Communities.Add(new UserCommunityCollectionItem()
                {
                    Id = pair.Item1.Id,
                    Name = pair.Item1.Name,
                    Role = pair.Item2.Role
                });
            }
            await Respond(collection);
        }

        private class UserCommunityCollectionItem{
            [JsonPropertyName("id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;

            [JsonPropertyName("role")]
            public MemberRole Role { get; set; }
        }

        private class UserCommunityCollection{
            [JsonPropertyName("communities")]
            public List<UserCommunityCollectionItem> Communities { get; set; } = new List<UserCommunityCollectionItem>();
        }
    }

}