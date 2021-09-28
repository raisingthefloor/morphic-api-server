// Copyright 2021 Raising the Floor - International
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

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Morphic.Json;

namespace Morphic.Server.Community
{

    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using System.Net;
    using System.Text.Json.Serialization;
    using Db;
    using Http;
    using Users;
    using Billing;

    /// <summary>
    /// Handles coupons.
    /// </summary>
    [Path("/v1/communities/{id}/billing/coupon")]
    public class BillingCouponEndpoint: Endpoint
    {
        public BillingCouponEndpoint(IPaymentProcessor paymentProcessor, IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger, Plans allPlans): base(contextAccessor, logger)
        {
            this.PaymentProcessor = paymentProcessor;
            this.AllPlans = allPlans;
        }

        private readonly IPaymentProcessor PaymentProcessor;
        private readonly Plans AllPlans;
        
        private Member member = null!;
        private Community community = null!;
        private User user = null!;
        private BillingRecord billing = null!;

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            this.user = await this.RequireUser();
            this.community = await this.Load<Community>(this.Id);
            this.member = await this.Load<Member>(m => m.UserId == this.user.Id && m.CommunityId == this.community.Id && m.State == MemberState.Active);

            if (this.member.Role != MemberRole.Manager){
                throw new HttpError(HttpStatusCode.Forbidden);
            }

            if (this.community.BillingId == null)
            {
                throw new HttpError(HttpStatusCode.NotFound);
            }

            this.billing = await this.Load<BillingRecord>(this.community.BillingId);
        }

        /// <summary>
        /// Check a coupon code against some plans.
        /// </summary>
        [Method]
        public async Task Post()
        {
            CouponCheckRequest checkRequest = await this.Request.ReadJson<CouponCheckRequest>();

            Coupon? coupon = await this.PaymentProcessor.GetCoupon(checkRequest.CouponCode);

            if (coupon == null)
            {
                await this.Respond(CouponCheckErrorResponse.Unknown);
            }
            else if (coupon.Expired && checkRequest.IncludeInactive != true)
            {
                await this.Respond(CouponCheckErrorResponse.Expired);
            }
            else if (!coupon.Active && checkRequest.IncludeInactive != true)
            {
                await this.Respond(CouponCheckErrorResponse.Inactive);
            }
            else if (!string.IsNullOrEmpty(this.user.EmailPlaintext) && !coupon.IsEmailAllowed(this.user.EmailPlaintext))
            {
                await this.Respond(CouponCheckErrorResponse.InvalidEmail);
            }
            else
            {
                CouponCheckResponse response = new CouponCheckResponse();
                foreach ((string key, string planId) in checkRequest.Plans)
                {
                    Plan? plan = this.AllPlans.GetPlan(planId);
                    if (plan != null)
                    {
                        response.DiscountedPlans[key] = new DiscountedPlan(plan, coupon, this.user.EmailPlaintext);
                    }
                }

                await this.Respond(response);
            }
        }

        /// <summary>
        /// Applies a coupon code to the community billing.
        /// </summary>
        [Method]
        public async Task Put()
        {
            Database db = this.Context.GetDatabase();
            CouponUpdateRequest input = await this.Request.ReadJson<CouponUpdateRequest>();

            Coupon? coupon = await this.PaymentProcessor.ChangeCommunityCoupon(this.community, this.billing, input.CouponCode);

            await db.SetField(this.billing, b => b.CouponCode, input.CouponCode);
            await db.SetField(this.billing, b => b.Coupon, coupon);
        }

        /// <summary>
        /// Used by the PUT handler, to set the coupon used by the community.
        /// </summary>
        class CouponUpdateRequest
        {
            [JsonPropertyName("coupon_code")]
            public string CouponCode { get; set; } = null!;
        }

        /// <summary>
        /// Used by the POST handler, to check a coupon against some plans.
        /// </summary>
        class CouponCheckRequest
        {
            [JsonPropertyName("coupon_code")]
            public string CouponCode { get; set; } = null!;

            [JsonPropertyName("inactive")]
            public bool? IncludeInactive { get; set; }

            [JsonPropertyName("plans")]
            public Dictionary<string, string> Plans { get; set; }
        }

        /// <summary>
        /// Returned by the POST handler.
        /// </summary>
        class CouponCheckResponse
        {
            [JsonPropertyName("discounted_plans")]
            public Dictionary<string, DiscountedPlan?> DiscountedPlans { get; } =
                new Dictionary<string, DiscountedPlan?>();
        }

        class CouponCheckErrorResponse
        {
            [JsonPropertyName("error")]
            public string Error { get; set; }

            public static CouponCheckErrorResponse Unknown = new CouponCheckErrorResponse() { Error = "unknown"};
            public static CouponCheckErrorResponse Expired = new CouponCheckErrorResponse() { Error = "expired"};
            public static CouponCheckErrorResponse Inactive = new CouponCheckErrorResponse() { Error = "inactive"};
            public static CouponCheckErrorResponse InvalidEmail = new CouponCheckErrorResponse() { Error = "invalid_email"};
        }
    }
}