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
    using Billing;

    [Path("/v1/communities/{id}/billing")]
    public class BillingEndpoint: Endpoint
    {

        public BillingEndpoint(IPaymentProcessor paymentProcessor, Plans plans, IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
            this.paymentProcessor = paymentProcessor;
            this.plans = plans;
        }

        private IPaymentProcessor paymentProcessor;
        private Plans plans;

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
            if (Community.BillingId == null)
            {
                throw new HttpError(HttpStatusCode.NotFound);
            }
            Billing = await Load<BillingRecord>(Community.BillingId);
        }

        private Member Member = null!;
        private Community Community = null!;
        private User User = null!;
        private BillingRecord Billing = null!;

        [Method]
        public async Task Get()
        {
            await Respond(Billing);
        }

        [Method]
        public async Task Put()
        {
            var db = Context.GetDatabase();
            var input = await Request.ReadJson<BillingPutRequest>();
            var plan = plans.GetPlan(input.PlanId);
            if (plan == null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BillingPutError.BadPlanId);
            }
            if (plan.MemberLimit < Community.MemberCount)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BillingPutError.PlanLimitExceeded);
            }
            var member = await db.Get<Member>(m => m.Id == input.ContactMemberId && m.CommunityId == Community.Id && m.Role == MemberRole.Manager);
            if (member == null || member.UserId == null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BillingPutError.BadMemberId);
            }
            var user = await db.Get<User>(member.UserId);
            if (user == null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BillingPutError.BadMemberId);
            }
            if (plan.Id != Billing.PlanId)
            {
                Billing.PlanId = plan.Id;
                await paymentProcessor.ChangeCommunitySubscription(Community, Billing);
                await db.SetField(Billing, b => b.PlanId, Billing.PlanId);
                await db.SetField(Community, c => c.MemberLimit, plan.MemberLimit);
            }
            if (member.Id != Billing.ContactMemeberId)
            {
                Billing.ContactMemeberId = member.Id;
                await paymentProcessor.ChangeCommunityContact(Community, Billing, user);
                await db.SetField(Billing, b => b.ContactMemeberId, Billing.ContactMemeberId);
            }
        }

        class BillingPutRequest
        {
            [JsonPropertyName("plan_id")]
            public string PlanId { get; set; } = null!;

            [JsonPropertyName("contact_member_id")]
            public string ContactMemberId { get; set; } = null!;
        }

        class BillingPutError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;

            public static BillingPutError BadPlanId = new BillingPutError() { Error = "bad_plan_id" };
            public static BillingPutError BadMemberId = new BillingPutError() { Error = "bad_member_id" };
            public static BillingPutError PlanLimitExceeded = new BillingPutError() { Error = "plan_limit_exceeded" };
        }

    }

}