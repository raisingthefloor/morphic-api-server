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

    [Path("/v1/communities/{id}/invitations")]
    public class InvitationsEndpoint: Endpoint
    {

        public InvitationsEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger, IBackgroundJobClient jobClient): base(contextAccessor, logger)
        {
            this.jobClient = jobClient;
        }

        private IBackgroundJobClient jobClient;

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            User = await RequireUser();
            Community = await Load<Community>(Id);
            Member = await Load<Member>(m => m.UserId == User.Id && m.CommunityId == Community.Id && m.State == MemberState.Active);
            if (Member.Role != MemberRole.Manager){
                throw new HttpError(HttpStatusCode.Forbidden);
            }
        }

        private Member Member = null!;
        private Community Community = null!;
        private User User = null!;

        [Method]
        public async Task Post()
        {
            var db = Context.GetDatabase();
            var input = await Request.ReadJson<InvitationPostRequest>();
            if (!User.EmailVerified)
            {
                throw new HttpError(HttpStatusCode.BadRequest, InvitationPostError.EmailVerificationRequired);
            }
            var member = await db.Get<Member>(input.MemeberId);
            if (member == null || member.CommunityId != Community.Id)
            {
                throw new HttpError(HttpStatusCode.BadRequest, InvitationPostError.MemberNotFound);
            }
            if (member.State == MemberState.Active)
            {
                throw new HttpError(HttpStatusCode.BadRequest, InvitationPostError.MemberActive);
            }
            if (!User.IsValidEmail(input.Email))
            {
                throw new HttpError(HttpStatusCode.BadRequest, InvitationPostError.MalformedEmail);
            }
            var invitation = new Invitation()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(14)
            };
            invitation.Email.PlainText = input.Email;
            await db.Save(invitation);

            member.State = MemberState.Invited;
            await db.Save(member);
            jobClient.Enqueue<InvitationEmail>(x => x.SendEmail(
                invitation.Id,
                input.Message,
                Request.ClientIp()
            ));
        }

        class InvitationPostRequest
        {
            [JsonPropertyName("member_id")]
            public string MemeberId { get; set; } = null!;

            [JsonPropertyName("email")]
            public string Email { get; set; } = null!;

            [JsonPropertyName("message")]
            public string? Message { get; set; } = null!;
        }

        class InvitationPostError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;

            public static InvitationPostError MemberNotFound = new InvitationPostError() { Error = "member_not_found" };
            public static InvitationPostError MemberActive = new InvitationPostError() { Error = "member_active" };
            public static InvitationPostError MalformedEmail = new InvitationPostError() { Error = "malformed_email" };
            public static InvitationPostError EmailVerificationRequired = new InvitationPostError() { Error = "email_verification_required" };
        }
    }

}