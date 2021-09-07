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
using Hangfire;

namespace Morphic.Server.Community
{

    using Http;
    using Users;
    using Json;

    [Path("/v1/communities/{communityId}/invitations/{id}/accept")]
    public class InvitationAcceptEndpoint: Endpoint
    {

        public InvitationAcceptEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
            AddAllowedOrigin(settings.FrontEndServerUri);
        }

        [Parameter]
        public string CommunityId = "";

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            User = await RequireUser();
            Community = await Load<Community>(CommunityId);
            Invitation = await Load<Invitation>(Id);
            if (Invitation.CommunityId != Community.Id)
            {
                throw new HttpError(HttpStatusCode.NotFound);
            }
            Member = await Load<Member>(Invitation.MemberId);
            if (Member.CommunityId != Community.Id)
            {
                throw new HttpError(HttpStatusCode.NotFound);
            }
        }

        private Member Member = null!;
        private Community Community = null!;
        private User User = null!;
        private Invitation Invitation = null!;

        [Method]
        public async Task Post()
        {
            var db = Context.GetDatabase();
            Member.UserId = User.Id;
            Member.State = MemberState.Active;

            if (this.User.Email.PlainText == this.Invitation.Email.PlainText)
            {
                this.User.EmailVerified = true;
            }

            await Save(Member);
            await db.Delete(Invitation);
        }

    }

}