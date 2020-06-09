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
using Microsoft.AspNetCore.Http;

namespace Morphic.Server.Background
{

    using Http;

    /// <summary>
    /// Base class for all background jobs
    /// </summary>
    public class BackgroundJob
    {

        public BackgroundJob(MorphicSettings settings)
        {
            this.settings = settings;
        }

        protected readonly MorphicSettings settings;

        public Uri ServerUri
        {
            get
            {
                return settings.ServerUri!;
            }
        }

        public Uri GetUri<T>(Dictionary<string, string> pathParameters) where T: Endpoint
        {
            return GetUri<T>(ServerUri, pathParameters);
        }

        public Uri GetUri<T>(Uri serverUri, Dictionary<string, string> pathParameters) where T: Endpoint
        {
            var type = typeof(T);
            var builder = new UriBuilder(serverUri);
            builder.Path = type.GetRoutePath(pathParameters);
            return builder.Uri;
        }

        public Uri GetFrontEndUri(string path, Dictionary<string, string>? fragmentParameters = null)
        {
            if (!settings.FrontEndServerUri.IsAbsoluteUri)
            {
                throw new Exception("Can not use non-absolute URI. Probably forgot to set FrontEndServerUrlPrefix");
            }
            var builder = new UriBuilder(settings.FrontEndServerUri);
            builder.Path = path;
            if (fragmentParameters != null)
            {
                var query = new QueryString();
                foreach (var pair in fragmentParameters)
                {
                    query = query.Add(pair.Key, pair.Value);
                }
                builder.Fragment = query.ToUriComponent().Substring(1);
            }
            return builder.Uri;
        }
    }

}