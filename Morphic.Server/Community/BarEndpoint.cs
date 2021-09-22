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
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Morphic.Server.Community
{

    using Http;
    using Users;
    using Json;

    [Path("/v1/communities/{communityId}/bars/{id}")]
    public class BarEndpoint: Endpoint
    {

        public BarEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }


        [Parameter]
        public string CommunityId = "";

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            User = await RequireUser();
            Community = await Load<Community>(CommunityId);
            Member = await Load<Member>(m => m.UserId == User.Id && m.CommunityId == Community.Id && m.State == MemberState.Active);
            Bar = await Load<Bar>(Id);
            if (Member.Role != MemberRole.Manager && this.Member.UserId != this.User.Id)
            {
                throw new HttpError(HttpStatusCode.Forbidden);
            }
            if (Bar.CommunityId != Community.Id)
            {
                throw new HttpError(HttpStatusCode.NotFound);
            }
        }

        private Member Member = null!;
        private Community Community = null!;
        private User User = null!;
        private Bar Bar = null!;

        [Method]
        public async Task Get()
        {
            await Respond(Bar);
        }

        [Method]
        public async Task Put()
        {
            if (Member.Role != MemberRole.Manager)
            {
                throw new HttpError(HttpStatusCode.Forbidden);
            }

            var input = await Request.ReadJson<BarPutRequest>();
            if (Bar.Id == Community.DefaultBarId && !input.IsShared)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BarPutError.DefaultMustBeShared);
            }
            Bar.Name = input.Name;
            Bar.IsShared = input.IsShared;
            Bar.Items = input.Items;
            await Save(Bar);
        }

        class BarPutRequest
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;

            [JsonPropertyName("is_shared")]
            [JsonRequired]
            public bool IsShared { get; set; }

            [JsonPropertyName("items")]
            public BarItem[] Items { get; set; } = null!;
        }

        [Method]
        public async Task Delete()
        {
            if (Member.Role != MemberRole.Manager)
            {
                throw new HttpError(HttpStatusCode.Forbidden);
            }

            if (Bar.Id == Community.DefaultBarId)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BarDeleteError.CannotDeleteDefault);
            }
            var db = Context.GetDatabase();
            var members = await db.GetEnumerable<Member>(m => m.CommunityId == Community.Id && ((m.BarId == Bar.Id) || (m.BarIds.Contains(Bar.Id))));
            foreach (var member in members){
                throw new HttpError(HttpStatusCode.BadRequest, BarDeleteError.CannotDeleteUsed);
            }
            await Delete(Bar);
        }

        class BarPutError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;

            public static BarPutError DefaultMustBeShared = new BarPutError() { Error = "default_must_be_shared" };
        }

        class BarDeleteError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;

            public static BarPutError CannotDeleteUsed = new BarPutError() { Error = "cannot_delete_used" };
            public static BarPutError CannotDeleteDefault = new BarPutError() { Error = "cannot_delete_default" };
        }
    }

}