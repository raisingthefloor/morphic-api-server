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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Tests.Billing
{

    using Server.Community;
    using Server.Users;
    using Server.Billing;

    public class MockPaymentProcessor : IPaymentProcessor
    {

        public MockPaymentProcessor()
        {
        }

        public int StartCommunitySubscriptionCalls = 0;
        public int ChangeCommunitySubscriptionCalls = 0;
        public int CancelCommunitySubscriptionCalls = 0;
        public int ChangeCommunityContactCalls = 0;
        public int ChangeCommunityCardCalls = 0;

        public Task StartCommunitySubscription(Community community, BillingRecord billing, User contact)
        {
            ++StartCommunitySubscriptionCalls;
            return Task.CompletedTask;
        }

        public Task ChangeCommunitySubscription(Community community, BillingRecord billing)
        {
            ++ChangeCommunitySubscriptionCalls;
            return Task.CompletedTask;
        }

        public Task CancelCommunitySubscription(Community community, BillingRecord billing)
        {
            ++CancelCommunitySubscriptionCalls;
            return Task.CompletedTask;
        }

        public Task ChangeCommunityContact(Community community, BillingRecord billing, User contact)
        {
            ++ChangeCommunityContactCalls;
            return Task.CompletedTask;
        }

        public Task ChangeCommunityCard(Community community, BillingRecord billing, object card)
        {
            ++ChangeCommunityCardCalls;
            return Task.CompletedTask;
        }

    }

}