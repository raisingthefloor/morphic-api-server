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
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Web;

namespace Morphic.Server.Community
{

    using Http;
    using Billing;

    [Path("/v1/validate/link/{url}")]
    public class ValidateLinkEndpoint: Endpoint
    {
        [Parameter]
        public string Url = string.Empty;

        public ValidateLinkEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        public override async Task LoadResource()
        {
            await RequireUser();
        }

        [Method]
        public async Task Head()
        {
            await ValidateLinkEndpoint.CheckLink(HttpUtility.UrlDecode(this.Url), this.Request.ClientIp());
        }

        /// <summary>
        /// Check if a http/https url is valid, and the resource it points to is available.
        /// </summary>
        /// <returns>Returns if the link is ok. HttpError exception if not.</returns>
        /// <exception cref="HttpError">Thrown if the link is isn't good.</exception>
        public static async Task CheckLink(string urlString, string? clientIp, Action<Uri>? requestMaker = null)
        {
            try
            {
                if (!Uri.TryCreate(urlString, UriKind.Absolute, out Uri? parsed))
                {
                    throw new HttpError(HttpStatusCode.BadRequest);
                }

                // Only checking http
                if (parsed.IsFile || parsed.IsLoopback || parsed.IsUnc ||
                    (parsed.Scheme != "http" && parsed.Scheme != "https"))
                {
                    throw new HttpError(HttpStatusCode.BadRequest);
                }

                // Limit the scope to normal looking urls
                if (parsed.HostNameType != UriHostNameType.Dns || !parsed.IsDefaultPort ||
                    !string.IsNullOrEmpty(parsed.UserInfo))
                {
                    throw new HttpError(HttpStatusCode.BadRequest);
                }

                if (requestMaker != null)
                {
                    requestMaker(parsed);
                }
                else
                {
                    HttpWebRequest req = (HttpWebRequest) WebRequest.Create(parsed);
                    if (!string.IsNullOrEmpty(clientIp))
                    {
                        req.Headers.Add("X-Forwarded-For", clientIp);
                    }

                    req.Method = "HEAD";
                    req.Timeout = 10000;
                    HttpWebResponse response = (HttpWebResponse) await req.GetResponseAsync();
                    if ((int) response.StatusCode >= 400)
                    {
                        throw new HttpError(HttpStatusCode.Gone);
                    }

                    response.Close();
                }

            }
            catch (HttpError httpError)
            {
                throw;
            }
            catch (Exception)
            {
                throw new HttpError(HttpStatusCode.Gone);
            }
        }



        private class LinkCheck
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        private class PlansPage
        {
            [JsonPropertyName("plans")]
            public Plan[] Plans { get; set; } = null!;
        }
    }

}