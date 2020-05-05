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

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System;
using System.Net;
using System.Text.Encodings.Web;
using Serilog;
using Serilog.Context;

namespace MorphicServer
{
    public static class HttpResponseExtensions
    {
        
        /// <summary>Serialize the given object to JSON and write it to this response</summary>
        public static async Task WriteJson<T>(this HttpResponse response, T obj, CancellationToken cancellationToken = default(CancellationToken))
        {
            response.ContentType = "application/json; charset=utf-8";
            await response.StartAsync(cancellationToken);
            if (obj != null)
            {
                var options = new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                await JsonSerializer.SerializeAsync(response.Body, obj, obj.GetType(), options, cancellationToken);
            }
            await response.CompleteAsync();
        }

        /// <summary>Send a response for the given error</summary>
        public static async Task WriteError(this HttpResponse response, HttpError error, CancellationToken cancellationToken = default(CancellationToken))
        {
            response.StatusCode = (int)error.Status;
            if (error.Content is object content){
                await response.WriteJson(content, cancellationToken);
            }
        }
    }

    public static class HttpRequestExtensions
    {

        /// <summary>Deserialize the request JSON body into an object</summary>
        public static async Task<T> ReadJson<T>(this HttpRequest request, CancellationToken cancellationToken = default(CancellationToken)) where T: class
        {
            if (request.ContentType == "application/json; charset=utf-8")
            {
                try
                {
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonElementInferredTypeConverter());
                    options.Converters.Add(new NonNullableExceptionJsonConverter());
                    var obj = await JsonSerializer.DeserializeAsync(request.Body, typeof(T), options, cancellationToken);
                    if (obj is T o)
                    {
                        return o;
                    }
                }
                catch (NonNullableExceptionJsonConverter.NullOrMissingProperties e)
                {
                    Log.Logger.Information("Could not deserialize payload, missing required field");
                    var content = new Dictionary<string, object>(){
                        { "error", "missing_required" },
                        { "details", new Dictionary<string, object>() {
                            {"required",  e.PropertyNames }
                        }}
                    };
                    throw new HttpError(HttpStatusCode.BadRequest, content);
                }
                catch (JsonException e)
                {
                    Log.Logger.Information("Could not deserialize payload: {JsonError}", e.Message);
                }
                catch (Exception e)
                {
                    using (LogContext.PushProperty("exception", e))
                    {
                        Log.Logger.Information("Unknown error during deserialize payload");
                    }
                }
                throw new HttpError(HttpStatusCode.BadRequest);
            }
            throw new HttpError(HttpStatusCode.UnsupportedMediaType);
        }
    }
}