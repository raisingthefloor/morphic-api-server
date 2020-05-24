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
using Newtonsoft.Json.Linq;
using Serilog;

namespace MorphicServer
{
    public interface IRecaptcha
    {
        public string Key { get; }
        public bool ReCaptchaPassed(string gRecaptchaResponse);
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
        
        public Recaptcha(MorphicSettings settings)
        {
            if (settings.Recaptcha3Settings.Key == "" || settings.Recaptcha3Settings.Secret == "")
            {
                throw new RecaptchaException("Missing key or secret");
            }
            this.settings = settings.Recaptcha3Settings;
        }

        public bool ReCaptchaPassed(string gRecaptchaResponse)
        {
            HttpClient httpClient = new HttpClient();
            var res = httpClient.GetAsync($"https://www.google.com/recaptcha/api/siteverify?secret={settings.Secret}&response={gRecaptchaResponse}").Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                Log.Logger.Error("Error while sending request to ReCaptcha");
                return false;
            }
    
            dynamic jsonData = JObject.Parse(res.Content.ReadAsStringAsync().Result);
            return jsonData.success == "true";
        }
    }
}