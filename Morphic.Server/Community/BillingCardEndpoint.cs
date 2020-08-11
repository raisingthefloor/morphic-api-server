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

    [Path("/v1/communities/{id}/billing/card")]
    public class BillingCardEndpoint: Endpoint
    {

        public BillingCardEndpoint(IPaymentProcessor paymentProcessor, IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
            this.paymentProcessor = paymentProcessor;
        }

        private IPaymentProcessor paymentProcessor;

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
        public async Task Post()
        {
            var db = Context.GetDatabase();
            var input = await Request.ReadJson<CardPostRequest>();
            await paymentProcessor.ChangeCommunityCard(Community, Billing, input.Token);
            await db.SetField(Billing, b => b.Card, Billing.Card);
            await Respond(new CardPostResponse()
            {
                Card = Billing.Card!
            });
        }

        class CardPostRequest
        {
            [JsonPropertyName("token")]
            public string Token { get; set; } = null!;
        }

        class CardPostResponse
        {
            [JsonPropertyName("card")]
            public Card Card { get; set; } = null!;
        }

        class CardPostError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; } = null!;

            public static CardPostError Invalid = new CardPostError() { Error = "invalid" };
        }

    }

}