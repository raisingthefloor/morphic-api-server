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
    [Path("/users/{userid}/preferences/{id}")]
    public class PreferencesEndpoint: Endpoint
    {

        /// <summary>The user id to use, populated from the request URL</summary>
        [Parameter]
        public string UserId = "";

        /// <summary>The lookup id to use, populated from the request URL</summary>
        [Parameter]
        public string Id = "";

        /// <summary>Fetch the preferences from a database</summary>
        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            using (LogContext.PushProperty("AuthenticatedUserUid", authenticatedUser.Id))
            using (LogContext.PushProperty("AuthenticatedUserPreferenceId", authenticatedUser.PreferencesId))
            using (LogContext.PushProperty("RequestedPreferencesUid", Id))
            {
                if (authenticatedUser.Id != UserId || authenticatedUser.PreferencesId != Id)
                {
                    Log.Logger.Information("PREFERENCE_ACCESS_DENIED: {AuthenticatedUserUid} may not request preferences {Id}");
                    throw new HttpError(HttpStatusCode.Forbidden);
                }
                Log.Logger.Debug("PREFERENCE_LOADED");
                Preferences = await Load<Preferences>(Id);
            }
        }

        /// <summary>The preferences data populated by <code>LoadResource()</code></summary>
        public Preferences Preferences = new Preferences();

        /// <summary>Fetch the user's preferences</summary>
        [Method]
        public async Task Get()
        {
            await Respond(Preferences);
        }

        /// <summary>Update the user's preferences</summary>
        [Method]
        public async Task Put()
        {
            var updated = await Request.ReadJson<Preferences>();
            Preferences.Default = updated.Default;
            await Save(Preferences);
        }

        /// <summary>Update the user's preferences</summary>
        [Method]
        public async Task Delete()
        {
            await Delete(Preferences);
        }
    }
}