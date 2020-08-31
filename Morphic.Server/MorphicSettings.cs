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

namespace Morphic.Server
{

    public class MorphicSettings
    {

        /// <summary>The Server URL prefix. Used to generate URLs for various purposes.</summary>
        public string ServerUrlPrefix { get; set; } = "";

        public Uri? ServerUri {
            get{
                if (ServerUrlPrefix != "")
                {
                    return new Uri(ServerUrlPrefix);
                }
                return null;
            }
        }

        /// <summary>The Server URL prefix for the front-end web server used for things like password reset</summary>
        public string FrontEndServerUrlPrefix { get; set; } = "";

        /// <summary>
        /// Declare users stale after this many days. Default: 3 years.
        /// </summary>
        public int StaleUserAfterDays { get; set; } = 3*365;
        
        public Uri FrontEndServerUri {
            get{
                if (FrontEndServerUrlPrefix != "")
                {
                    return new Uri(FrontEndServerUrlPrefix);
                }
                return new Uri("", UriKind.Relative);
            }
        }

        /// <summary>The Server URL prefix for the front-end web server used for things like password reset</summary>
        public string CommunityServerUrlPrefix { get; set; } = "";

        public Uri CommunityServerUri {
            get{
                if (CommunityServerUrlPrefix != "")
                {
                    return new Uri(CommunityServerUrlPrefix);
                }
                return new Uri("", UriKind.Relative);
            }
        }

        /// <summary>The Server URL prefix for the front-end web server used for things like password reset</summary>
        public string AltCommunityServerUrlPrefix { get; set; } = "";

        public Uri? AltCommunityServerUri {
            get{
                if (AltCommunityServerUrlPrefix != "")
                {
                    return new Uri(AltCommunityServerUrlPrefix);
                }
                return null;
            }
        }

        public Recaptcha3Settings Recaptcha3Settings { get; set; } = new Recaptcha3Settings();
    }

    public class Recaptcha3Settings
    {
        public string Key { get; set; } = "";

        public string Secret { get; set; } = "";

        public double MinimumScore { get; set; } = 0.7;

    }

}