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

using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Morphic.Server.Users;

namespace Morphic.Server.Payments
{
    using Http;

    [Path("/v1/users/{UserId}/payments")]
    public class PaymentCreateEndpoint : Endpoint
    {
        private readonly IBackgroundJobClient jobClient;
        
        /// <summary>The user id to use, populated from the request URL</summary>
        [Parameter]
        public string UserId = "";

        public PaymentCreateEndpoint(
            IHttpContextAccessor contextAccessor,
            ILogger<Endpoint> logger,
            IBackgroundJobClient jobClient) : base(contextAccessor, logger)
        {
            this.jobClient = jobClient;
        }

        private User authenticatedUser = null!;
        
        /// <summary>Fetch the preferences from a database</summary>
        public override async Task LoadResource()
        {
            authenticatedUser = await RequireUser();
            if (authenticatedUser.Id != UserId)
            {
                throw new HttpError(HttpStatusCode.Forbidden);
            }
        }

        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<PaymentCreateRequest>();
            // TODO presumably some error checking here.
            var transaction = new Transaction(
                authenticatedUser,
                PaymentProcessors.Stripe,
                request.IdempotencyKey,
                request.Amount,
                request.CurrencyCode);
            if (string.IsNullOrEmpty(request.CreditCardId))
            {
                if (request.Cc == null)
                {
                    throw new HttpError(HttpStatusCode.BadRequest, BadPaymentCreateResponse.MissingRequired(new List<string> {"cc", "credit_card_id"}));
                }
                transaction.CreditCardInfo = new CreditCardInfo(request.Cc.Number, request.Cc.Cvv, request.Cc.ExpirationDate, request.Cc.ZipCode);
            }
            else
            {
                transaction.CreditCardId = request.CreditCardId;
            }
            await Context.GetDatabase().Save(transaction);
            jobClient.Enqueue<ProcessStripePayment>(x => x.ProcessPayment(transaction.Id,
                Request.ClientIp()
            ));
            await Respond(new PaymentCreateResponse(transaction.Id));
        }

        public class CreditCard
        {
            /// <summary>
            /// The Credit Card Number
            /// </summary>
            [JsonPropertyName("number")]
            public string Number { get; set;} = null!;
            
            /// <summary>
            /// The Credit Card CVV
            /// </summary>
            [JsonPropertyName("cvv")]
            public string Cvv { get; set;} = null!;
            
            /// <summary>
            /// The expiration date of the card: only mm/yyyy
            /// TODO Perhaps we should split this into two fields for less parsing and potential errors?
            /// </summary>
            [JsonPropertyName("expiration_date")]
            public string ExpirationDate { get; set;} = null!;

            /// <summary>
            /// In the US Zip Code. In the rest of the world? What?
            /// </summary>
            [JsonPropertyName("zip_code")]
            public string ZipCode { get; set; } = null!;
        }

        public class PaymentCreateRequest
        {
            /// <summary>
            /// The amount in pennies or cents or whatever
            /// </summary>
            [JsonPropertyName("amount")]
            public long Amount { get; set; }

            /// <summary>
            /// A currency code: see stripe documentation
            /// </summary>
            [JsonPropertyName("currency_code")]
            public string CurrencyCode { get; set; } = null!;

            /// <summary>
            /// Random Guid created by the client, used in case of retransmissions.
            /// </summary>
            [JsonPropertyName("idempotency_key")]
            public string IdempotencyKey { get; set; } = null!;

            /// <summary>
            /// Credit Card information
            /// </summary>
            [JsonPropertyName("cc")]
            public CreditCard? Cc { get; set; }

            /// <summary>
            /// ID of a previously used credit card
            /// </summary>
            [JsonPropertyName("credit_card_id")]
            public string CreditCardId { get; set; } = null!;

        }
        
        public class PaymentCreateResponse
        {
            public string TransactionId;

            public PaymentCreateResponse(string transactionId)
            {
                TransactionId = transactionId;
            }
        }

        public class BadPaymentCreateResponse : BadRequestResponse
        {
            public BadPaymentCreateResponse(string error, Dictionary<string, object> details) : base(error, details)
            {
            }

            public static BadPaymentCreateResponse MissingRequired(List<string> missing)
            {
                return new BadPaymentCreateResponse(
                    "missing_required",
                    new Dictionary<string, object>
                    {
                        {"required", missing}
                    });
            }
        }
    }
}