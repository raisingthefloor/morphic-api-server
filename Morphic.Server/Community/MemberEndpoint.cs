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
    using Json;
    using Billing;
    using Users;

    [Path("/v1/communities/{communityId}/members/{id}")]
    public class MemberEndpoint: Endpoint
    {

        public MemberEndpoint(IPaymentProcessor paymentProcessor, IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
            this.paymentProcessor = paymentProcessor;
        }

        private IPaymentProcessor paymentProcessor;

        [Parameter]
        public string CommunityId = "";

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            AuthenticatedUser = await RequireUser();
            Community = await Load<Community>(CommunityId);
            AuthenticatedMember = await Load<Member>(m => m.CommunityId == Community.Id && m.UserId == AuthenticatedUser.Id && m.State == MemberState.Active);
            if (AuthenticatedMember.Role != MemberRole.Manager){
                throw new HttpError(HttpStatusCode.Forbidden);
            }
            Member = await Load<Member>(m => m.Id == Id && m.CommunityId == Community.Id);
        }

        public Community Community = null!;
        public User AuthenticatedUser = null!;
        public Member AuthenticatedMember = null!;
        public Member Member = null!;

        [Method]
        public async Task Get()
        {
            await Respond(Member);
        }

        [Method]
        public async Task Put()
        {
            var db = Context.GetDatabase();
            var input = await Request.ReadJson<MemberPutRequest>();
            Member.FirstName.PlainText = input.FirstName;
            Member.LastName.PlainText = input.LastName;
            if (Member.Id == AuthenticatedMember.Id && input.Role == MemberRole.Member)
            {
                throw new HttpError(HttpStatusCode.BadRequest, MemberPutError.CannotDemoteSelf);
            }
            Member.Role = input.Role;
            if (input.BarId is string barId)
            {
                var bar = await db.Get<Bar>(barId);
                if (bar == null || bar.CommunityId != Community.Id)
                {
                    throw new HttpError(HttpStatusCode.BadRequest, MemberPutError.BadBarId);
                }
            }
            //
            // backwards-compatibility: if the member already had a single BarId, move it to the BarIds array in the response
            if (Member.BarId != null && Member.BarIds.Contains(Member.BarId!) == false) 
            {
                Member.BarIds.Add(Member.BarId);
            }
            Member.BarId = null;
            //
            // breaking change: we can not longer accept null bar IDs
            if (input.BarId == null) 
            {
                throw new HttpError(HttpStatusCode.BadRequest, MemberPutError.BadBarId);
            }
            //
            // legacy: Member.BarId = input.BarId;
            if (Member.BarIds.Contains(input.BarId) == false) 
            {
                Member.BarIds.Add(input.BarId);
            }
            //
            await Save(Member);
            // If we're demoting the member that is the billing contact for the community,
            // then make the logged-in member the new billing contact
            if (Community.BillingId is string billingId)
            {
                var billing = await db.Get<BillingRecord>(billingId);
                if (billing != null)
                {
                    if (billing.ContactMemeberId == Member.Id && Member.Role != MemberRole.Manager)
                    {
                        billing.ContactMemeberId = AuthenticatedMember.Id;
                        await this.paymentProcessor.ChangeCommunityContact(Community, billing, AuthenticatedUser);
                        await db.SetField(billing, b => b.ContactMemeberId, billing.ContactMemeberId);
                    }
                }
            }
        }

        [Method]
        public async Task Delete()
        {
            var db = Context.GetDatabase();
            if (Member.Id == AuthenticatedMember.Id)
            {
                throw new HttpError(HttpStatusCode.BadRequest, MemberDeleteError.CannotDeleteSelf);
            }
            // If we're removing the member that is the billing contact for the community,
            // then make the logged-in member the new billing contact
            if (Community.BillingId is string billingId)
            {
                var billing = await db.Get<BillingRecord>(billingId);
                if (billing != null)
                {
                    if (billing.ContactMemeberId == Member.Id)
                    {
                        billing.ContactMemeberId = AuthenticatedMember.Id;
                        await this.paymentProcessor.ChangeCommunityContact(Community, billing, AuthenticatedUser);
                        await db.SetField(billing, b => b.ContactMemeberId, billing.ContactMemeberId);
                    }
                }
            }
            await Delete(Member);
            await Context.GetDatabase().Increment(Community, c => c.MemberCount, -1);
        }

        class MemberPutRequest
        {
            [JsonPropertyName("first_name")]
            public string? FirstName { get; set; } = null!;

            [JsonPropertyName("last_name")]
            public string? LastName { get; set; } = null!;

            [JsonPropertyName("bar_id")]
            public string? BarId { get; set; }

            [JsonPropertyName("role")]
            [JsonRequired]
            public MemberRole Role { get; set; } = MemberRole.Member;
        }

        class MemberPutError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;

            public static MemberPutError BadBarId = new MemberPutError() { Error = "bad_bar_id" };
            public static MemberPutError CannotDemoteSelf = new MemberPutError() { Error = "cannot_demote_self" };
        }

        class MemberDeleteError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;

            public static MemberPutError CannotDeleteSelf = new MemberPutError() { Error = "cannot_delete_self" };
        }
    }

}