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

using System;
using System.Text.Json.Serialization;

namespace Morphic.Server.Billing
{
    public class Plan
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("member_limit")]
        public int MemberLimit { get; set; }

        [JsonPropertyName("months")]
        public int Months { get; set; }

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "USD";

        [JsonPropertyName("stripe")]
        public StripePlan? Stripe { get; set; }

        /// <summary>
        /// The price, for displaying.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [JsonPropertyName("price_text")]
        public String PriceText {
            get
            {
                if (this.Price < 0)
                {
                    return "";
                }

                if (this.Currency != "USD")
                {
                    throw new NotImplementedException("Support for multiple currencies is not implemented.");
                }

                decimal price = (decimal)this.Price / 100;
                return price.ToString("$#.##");
            }
        }


    }

}