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

using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;
using Serilog;
using Serilog.Context;

namespace MorphicServer
{
    /// <summary>And endpoint representing user preferences</summary>
    [Path("/v1/users/{id}")]
    public class UserEndpoint: Endpoint
    {

        /// <summary>The lookup id to use, populated from the request URL</summary>
        [Parameter]
        public string Id = "";

        /// <summary>Fetch the user from a database</summary>
        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            if (authenticatedUser.Id != Id)
            {
                using (LogContext.PushProperty("AuthenticatedUserUid", authenticatedUser.Id))
                using (LogContext.PushProperty("UserUid", Id))
                {
                    Log.Logger.Information("{AuthenticatedUserUid} may not request user {UserUid}");
                }

                throw new HttpError(HttpStatusCode.Forbidden);
            }
            User = await Load<User>(Id);
        }

        /// <summary>The user data populated by <code>LoadResource()</code></summary>
        public User User = new User();

        /// <summary>Fetch the user</summary>
        [Method]
        public async Task Get()
        {
            await Respond(User);
        }

        /// <summary>Update the user</summary>
        [Method]
        public async Task Put()
        {
            var updated = await Request.ReadJson<User>();
            User.FirstName = updated.FirstName;
            User.LastName = updated.LastName;
            await Save(User);
        }

        /// <summary>Update the user</summary>
        [Method]
        public async Task Delete()
        {
            await Delete(User);
        }
    }
}