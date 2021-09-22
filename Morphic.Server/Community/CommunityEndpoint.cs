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
using System.Net;
using System.Text.Json.Serialization;

namespace Morphic.Server.Community
{

    using Http;

    [Path("/v1/communities/{id}")]
    public class CommunityEndpoint: Endpoint
    {

        public CommunityEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            Community = await Load<Community>(Id);
            Member = await Load<Member>(m => m.CommunityId == Community.Id && m.UserId == authenticatedUser.Id && m.State == MemberState.Active);
            if (Member.Role != MemberRole.Manager && this.Member.UserId != authenticatedUser.Id){
                throw new HttpError(HttpStatusCode.Forbidden);
            }
        }

        public Community Community = null!;
        public Member Member = null!;

        [Method]
        public async Task Get()
        {
            await Respond(Community);
        }

        [Method]
        public async Task Put()
        {
            if (Member.Role != MemberRole.Manager)
            {
                throw new HttpError(HttpStatusCode.Forbidden);
            }

            var db = Context.GetDatabase();
            var input = await Request.ReadJson<CommunityPutRequest>();
            Community.Name = input.Name;
            var bar = await db.Get<Bar>(input.DefaultBarId);
            if (bar == null || bar.CommunityId != Community.Id)
            {
                throw new HttpError(HttpStatusCode.BadRequest, CommunityPutError.BadBarId);
            }
            Community.DefaultBarId = input.DefaultBarId;
            await Context.GetDatabase().Save(Community);
        }

        [Method]
        public async Task Delete(){
            if (Member.Role != MemberRole.Manager)
            {
                throw new HttpError(HttpStatusCode.Forbidden);
            }

            var db = Context.GetDatabase();
            await db.Delete(Community);
            await db.DeleteAll<Member>(m => m.CommunityId == Community.Id);
            await db.DeleteAll<Bar>(b => b.CommunityId == Community.Id);
        }

        class CommunityPutRequest
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;

            [JsonPropertyName("default_bar_id")]
            public string DefaultBarId { get; set; } = null!;
        }

        class CommunityPutError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;

            public static CommunityPutError BadBarId = new CommunityPutError() { Error = "bad_bar_id" };
        }
    }

}