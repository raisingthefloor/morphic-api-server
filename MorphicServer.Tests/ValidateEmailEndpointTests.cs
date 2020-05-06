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

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace MorphicServer.Tests
{
    public class ValidateEmailEndpointTests : EndpointTests
    {
        [Fact]
        public async Task TestEmailValidation()
        {
            var userInfo1 = await CreateTestUser(null, null);

            var user = await Database.Get<User>(userInfo1.Id);
            Assert.NotNull(user);
            Assert.False(user.EmailVerified);
            var pendingEmail = await Database.Get<PendingEmail>(p => p.UserId == userInfo1.Id);
            Assert.NotNull(pendingEmail);
            var oneTimeToken = await Database.Get<OneTimeToken>(t => t.UserId == userInfo1.Id);
            Assert.NotNull(oneTimeToken);

            // GET, bad token (we're sending the ID of the token not the token just for fun)
            var path = $"/v1/verifyEmail/{oneTimeToken.Id}";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            bool isPrimary;
            var emailText = EncryptedField.FromCombinedString(pendingEmail.EmailText).Decrypt(out isPrimary);
            Assert.NotNull(emailText);
            Assert.Contains(oneTimeToken.Token, emailText);

            // GET, bad token (we're sending the encrypted blob)
            path = $"/v1/verifyEmail/{oneTimeToken.Token}";
            request = new HttpRequestMessage(HttpMethod.Get, path);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            user = await Database.Get<User>(userInfo1.Id);
            Assert.NotNull(user);
            Assert.True(user.EmailVerified);
            Assert.Null(await Database.Get<OneTimeToken>(t => t.UserId == userInfo1.Id));
        }
    }
}