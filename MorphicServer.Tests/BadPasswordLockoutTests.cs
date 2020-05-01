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
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MorphicServer.Tests
{
    public class BadPasswordLockoutTests : EndpointTests
    {
        [Fact]
        public async Task TestLockout()
        {
            var userInfo1 = await CreateTestUser();

            Assert.Null(await Database.Get<BadPasswordLockout>(userInfo1.Id));
            Assert.Null(await BadPasswordLockout.UserLockedOut(Database, userInfo1.Id));
            
            var now = DateTime.UtcNow;
            var lockedOut = await BadPasswordLockout.BadAuthAttempt(Database, userInfo1.Id);
            Assert.False(lockedOut);
            
            var bpl = await Database.Get<BadPasswordLockout>(userInfo1.Id);
            Assert.NotNull(bpl);
            Assert.Equal(userInfo1.Id, bpl.Id);
            var expires = bpl.ExpiresAt;
            Assert.True(now < expires);
            Assert.Equal(1, bpl.Count);
            Assert.Null(await BadPasswordLockout.UserLockedOut(Database, userInfo1.Id));

            lockedOut = await BadPasswordLockout.BadAuthAttempt(this.Database, userInfo1.Id);
            Assert.False(lockedOut);
            
            bpl = await Database.Get<BadPasswordLockout>(userInfo1.Id);
            Assert.NotNull(bpl);
            Assert.Equal(userInfo1.Id, bpl.Id);
            Assert.Equal(expires, bpl.ExpiresAt);
            Assert.Equal(2, bpl.Count);
            Assert.Null(await BadPasswordLockout.UserLockedOut(Database, userInfo1.Id));

            lockedOut = await BadPasswordLockout.BadAuthAttempt(this.Database, userInfo1.Id);
            Assert.False(lockedOut);
            
            bpl = await Database.Get<BadPasswordLockout>(userInfo1.Id);
            Assert.NotNull(bpl);
            Assert.Equal(userInfo1.Id, bpl.Id);
            Assert.Equal(expires, bpl.ExpiresAt);
            Assert.Equal(3, bpl.Count);
            Assert.Null(await BadPasswordLockout.UserLockedOut(Database, userInfo1.Id));

            lockedOut = await BadPasswordLockout.BadAuthAttempt(this.Database, userInfo1.Id);
            Assert.False(lockedOut);
            
            bpl = await Database.Get<BadPasswordLockout>(userInfo1.Id);
            Assert.NotNull(bpl);
            Assert.Equal(userInfo1.Id, bpl.Id);
            Assert.Equal(expires, bpl.ExpiresAt);
            Assert.Equal(4, bpl.Count);
            Assert.Null(await BadPasswordLockout.UserLockedOut(Database, userInfo1.Id));

            lockedOut = await BadPasswordLockout.BadAuthAttempt(this.Database, userInfo1.Id);
            Assert.True(lockedOut);
            
            bpl = await Database.Get<BadPasswordLockout>(userInfo1.Id);
            Assert.NotNull(bpl);
            Assert.Equal(userInfo1.Id, bpl.Id);
            Assert.True(expires < bpl.ExpiresAt);
            Assert.Equal(5, bpl.Count);
            var until = await BadPasswordLockout.UserLockedOut(Database, userInfo1.Id);
            Assert.Equal(bpl.ExpiresAt, until);
        }
    }
}