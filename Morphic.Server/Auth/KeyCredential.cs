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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Auth
{

    using Db;
    using Http;
    using Users;

    public class KeyCredential: Credential
    {
    }

    public static class KeyCredentialDatabase
    {
        public static async Task<User> UserForKey(this Database db, string key)
        {
            var credential = await db.Get<KeyCredential>(key);
            if (credential == null || credential.UserId == null){
                throw new HttpError(HttpStatusCode.BadRequest, BadKeyAuthResponse.InvalidCredentials);
            }
            var user = await db.Get<User>(credential.UserId);
            if (user == null)
            {
                // Not sure how this could happen: It means we have a credential for the user, but no user!
                // How did the credential get there if there's no user?
                db.logger.LogError("{UserId} UserNotFound from credential", credential.UserId);
                throw new HttpError(HttpStatusCode.InternalServerError);
            }

            return user;
        }
        
        class BadKeyAuthResponse : BadRequestResponse
        {
            public static readonly BadRequestResponse InvalidCredentials = new BadKeyAuthResponse("invalid_credentials");
            // Future use: public static readonly BadRequestResponse RateLimited = new BadKeyAuthResponse("rate_limited");

            public BadKeyAuthResponse(string error) : base(error)
            {
            }
        }


    }
}