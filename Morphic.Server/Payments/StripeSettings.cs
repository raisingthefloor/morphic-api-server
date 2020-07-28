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

using Stripe;

namespace Morphic.Server.Payments
{
    public class StripeSettings
    {
        //public string ApiKeyPublic { get; set; } = "pk_test_51H6X3kF2lpQHHE2Y53uIFt6vYSS81j9KscRV76QqUF8Y4YJJ2bJOQUgIgdtEWeGbRk53N9SKU2dhSttpXkIXzNSV00ZzmKJWY2";

        public string ApiKeySecret { get; set; } = "sk_test_51H6X3kF2lpQHHE2YF6koDSp0CxvFzYMLY4tQhSS6H5t6qQj3GcDGNhHQ4kKEJmRXRCG4COjAzoT4ZJNj4qrERD7000lhXBnGJx";

        public string? AccountId { get; set; } = null;
        
        public RequestOptions StripeRequestOptions(string idempotencyKey)
        {
            var requestOptions = new RequestOptions();
            requestOptions.ApiKey = ApiKeySecret;
            requestOptions.StripeAccount = AccountId;
            requestOptions.IdempotencyKey = idempotencyKey;
            return requestOptions;
        }
    }
}