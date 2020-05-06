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
using MorphicServer.Attributes;

namespace MorphicServer
{
    [Path("/v1/verifyEmail/{oneTimeToken}")]
    public class ValidateEmailEndpoint : Endpoint
    {
        /// <summary>The lookup id to use, populated from the request URL</summary>
        [Parameter]
        public string oneTimeToken = "";
        
        /// <summary>The user data populated by <code>LoadResource()</code></summary>
        public User User = null!;
        /// <summary>The limited-use token data populated by <code>LoadResource()</code></summary>
        public OneTimeToken OneTimeToken = null!;

        public override async Task LoadResource()
        {
            OneTimeToken = await Load<OneTimeToken>(t => t.Token == oneTimeToken) ?? throw new HttpError(HttpStatusCode.NotFound);
            User = await Load<User>(OneTimeToken.UserId) ?? throw new HttpError(HttpStatusCode.BadRequest);
        }
        
        /// <summary>Fetch the user</summary>
        [Method]
        public async Task Get()
        {
            User.EmailVerified = true;
            await Save(User);
            await OneTimeToken.Invalidate(Context.GetDatabase());
            // TODO Need to respond with a nicer webpage than ""
        }
    }
}