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
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Auth
{
    public interface IRecaptcha
    {
        string Key { get; }

        Task<bool> ReCaptchaPassed(string action, string gRecaptchaResponse);
    }
    
    public class Recaptcha : IRecaptcha
    {
        private readonly Recaptcha3Settings settings;

        public string Key
        {
            get
            {
                return settings.Key;
            }
        }

        class RecaptchaException : Exception
        {
            public RecaptchaException(string error) : base(error)
            {
            }
        }
        
        public Recaptcha(MorphicSettings settings, ILogger<Recaptcha> logger)
        {
            if (settings.Recaptcha3Settings.Key == "" || settings.Recaptcha3Settings.Secret == "")
            {
                throw new RecaptchaException("Missing key or secret");
            }
            this.settings = settings.Recaptcha3Settings;
            this.logger = logger;
        }

        private readonly ILogger<Recaptcha> logger;

        public async Task<bool> ReCaptchaPassed(string action, string gRecaptchaResponse)
        {
            HttpClient httpClient = new HttpClient();
            var query = new QueryString();
            query = query.Add("secret", settings.Secret);
            query = query.Add("response", gRecaptchaResponse);
            var content = new StringContent(query.ToUriComponent().Substring(1), Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var response = await httpClient.PostAsync($"https://www.google.com/recaptcha/api/siteverify", content);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Error while sending request to ReCaptcha");
                return false;
            }

            try
            {
                var json = await response.Content.ReadAsStringAsync();
                // var stream = await response.Content.ReadAsStreamAsync();
                var result = JsonSerializer.Deserialize<RecaptchaResult>(json);
                if (!result.Success)
                {
                    logger.LogWarning("ReCaptcha result success = false ({0})", string.Join(',', result.ErrorCodes));
                    return false;
                }
                if (result.Action != action)
                {
                    logger.LogWarning("ReCaptcha result mismatched action");
                    return false;
                }
                if (result.Score < settings.MinimumScore)
                {
                    logger.LogWarning("ReCaptcha result score too low ({0} < {1})", result.Score, settings.MinimumScore);
                    return false;
                }
                return true;
            }
            catch (JsonException e)
            {
                logger.LogError(e, "Failed to parse result from ReCaptcha");
            }

            return false;
        }

        private class RecaptchaResult
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; } = false;

            [JsonPropertyName("action")]
            public string Action { get; set; } = "";

            [JsonPropertyName("score")]
            public double Score { get; set; } = 0;

            [JsonPropertyName("error-codes")]
            public string[] ErrorCodes { get; set; } = new string[] { };
        }
    }
}