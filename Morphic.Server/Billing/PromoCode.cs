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

using System;
using System.Net.Mail;
using System.Text.Json.Serialization;

namespace Morphic.Server.Billing
{

    using Db;

    public class Coupon
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("api_id")]
        public string ApiId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("amount_off")]
        public long? AmountOff { get; set; }

        [JsonPropertyName("percent_off")]
        public decimal? PercentOff { get; set; }

        [JsonPropertyName("expired")]
        public bool Expired { get; set; }

        private bool isActive = false;

        [JsonPropertyName("active")]
        public bool Active
        {
            get => this.isActive && !this.Expired;
            set => this.isActive = value;
        }

        public string? ValidForPlan { get; set; }
        public string? ValidForEmail { get; set; }

        /// <summary>Determines if the coupon is valid for an email address</summary>
        public bool IsEmailAllowed(string email)
        {
            bool valid;
            if (string.IsNullOrEmpty(this.ValidForEmail))
            {
                valid = true;
            }
            else
            {
                try
                {
                    MailAddress address = new MailAddress(email);
                    valid = address.Host == this.ValidForEmail ||
                            address.Host.EndsWith("." + this.ValidForEmail);
                }
                catch (FormatException)
                {
                    valid = false;
                }
            }

            return valid;
        }


    }
};