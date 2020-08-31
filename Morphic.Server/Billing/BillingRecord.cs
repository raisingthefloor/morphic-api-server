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

    using Db;

    public class BillingRecord: Record
    {

        [JsonIgnore]
        public string? CommunityId { get; set; }

        [JsonPropertyName("plan_id")]
        public string PlanId { get; set; } = null!;

        [JsonPropertyName("trial_end")]
        public DateTime TrialEnd { get; set; }

        [JsonPropertyName("status")]
        public BillingStatus Status { get; set; }

        [JsonPropertyName("contact_member_id")]
        public string ContactMemeberId { get; set; } = null!;

        [JsonPropertyName("card")]
        public Card? Card { get; set; }

        [JsonIgnore]
        public StripeBillingRecord? Stripe { get; set; }
    }

    public enum BillingStatus
    {
        Paid,
        PastDue,
        Canceled,
        Closed
    }
};