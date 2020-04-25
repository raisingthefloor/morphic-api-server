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
using System.Diagnostics;
using System.Reflection;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MorphicServer.Attributes;
using System.Linq;
using Prometheus;
using Serilog;
using Serilog.Context;

namespace MorphicServer
{
    /// <summary>The abstract base class for all MorphicServer endpoints, where an endpoint is a class that responds to requests at a given URL.</summary>
    /// <remarks>
    /// Subclasses should register themselves with a <code>[Path("/path/to/endpoint")]</code> attribute, and <code>[Method]</code> attributes on
    /// any instance methods that map to HTTP request methods.
    /// </remarks>
    /// <example>
    /// Subclass Example:
    /// <code>
    /// using System.Threading.Tasks;
    /// using MorphicServer.Attributes 
    ///
    /// namespace MorphicServer
    /// {
    ///     [Path("/preferences/{id}")]  // registers the URL template as a route handled by this class
    ///     class PreferencesEndpoint: Endpoint
    ///     {
    ///
    ///         [Parameter]  // Causes the Id field to be populated with the {id} string from the request's URL
    ///         public string Id = "";
    ///      
    ///         [Method]  // Called for GET requests
    ///         public Task Get()
    ///         {
    ///             Respond(Preferences)
    ///         }
    ///
    ///         private Preferences Preferences; // data model
    ///
    ///         public override async Task LoadResource() // called before Get()
    ///         {
    ///             // query db and populate the Preferences field
    ///         }   
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Endpoint
    {

        #pragma warning disable CS8618
        /// <summary>The http context for the current request</summary>
        public HttpContext Context { get; private set; }
        /// <summary>The current HTTP request</summary>
        public HttpRequest Request { get; private set; }
        /// <summary>The current HTTP Response</summary>
        public HttpResponse Response { get; private set; }
        #pragma warning restore CS8618

        private static readonly string counter_metric_name = "http_server_requests";
        private static readonly string histo_metric_name = "http_server_requests_duration";
        private static readonly string[] labelNames = new[] {"path", "method", "status"};

        private static readonly Counter counter = Metrics.CreateCounter(counter_metric_name, "HTTP Requests Total",
            new CounterConfiguration
            {
                LabelNames = labelNames
            });
        private static readonly Histogram histogram = Metrics.CreateHistogram(histo_metric_name,
            "HTTP Request Duration",
            labelNames);
        
            /// <summary>Used as the <code>RequestDelegate</code> for the route corresponding to each <code>Endpoint</code> subclass</summary>
        /// <remarks>
        /// Creates and populates the endpoint, calls <code>LoadResource()</code>, then invokes the relevant method
        /// </remarks>
        public static async Task Run<T>(HttpContext context) where T: Endpoint, new()
        {
            // Having the Endpoint subclasses and empty-constructable makes their code simpler and allows
            // us to construct with a generic.  However, it means we need to populate some fields here instead
            // of in a constructor.
            var endpoint = new T();
            endpoint.Context = context;
            endpoint.Request = context.Request;
            endpoint.Response = context.Response;
            var method = context.Request.Method;
            var statusCode = 500;
            var pathAttr = endpoint.GetType().GetCustomAttribute(typeof(Path)) as Path;
            var path = context.Request.Path.ToString();
            if (pathAttr?.Template == null)
            {
                Log.Logger.Error("No Path on endpoint");
            }
            else
            {
                path = pathAttr.Template;
            }

            if (String.IsNullOrEmpty(path))
            {
                Log.Logger.Error("Unknown path");
                path = "(unknown)";
            }

            using (LogContext.PushProperty("MorphicEndpoint", endpoint.ToString()))
            using (LogContext.PushProperty("SourceContext", typeof(Endpoint).ToString()))
            {
                var stopWatch = Stopwatch.StartNew();
                try
                {
                    if (endpoint.MethodInfoForRequestMethod(context.Request.Method) is MethodInfo methodInfo)
                    {
                        Func<Task> call = async () =>
                        {
                            endpoint.PopulateParameterFields();
                            await endpoint.LoadResource();
                            if (methodInfo.Invoke(endpoint, new object[] { }) is Task task)
                            {
                                await task;
                            }
                        };
                        if (methodInfo.GetRunInTransaction())
                        {
                            await endpoint.WithTransaction(async (session) => { await call(); });
                        }
                        else
                        {
                            await call();
                        }
                        if (path != "/metrics" && path != "/alive" && path != "/ready")
                        {
                            statusCode = context.Response.StatusCode;
                            counter.Labels(path, method, statusCode.ToString()).Inc();
                        }
                    }
                    else
                    {
                        // If the class doesn't have a matching method, respond with MethodNotAllowed
                        context.Response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                        statusCode = context.Response.StatusCode;
                        counter.Labels(path, method, statusCode.ToString()).Inc();
                    }
                }
                catch (HttpError error)
                {
                    statusCode = (int) error.Status;
                    counter.Labels(path, method, statusCode.ToString()).Inc();
                    await context.Response.WriteError(error, context.RequestAborted);
                }
                finally
                {
                    stopWatch.Stop();
                    histogram.Labels(path, method, statusCode.ToString())
                        .Observe(stopWatch.Elapsed.TotalSeconds);

                }
            }
        }

        /// <summary>Find all <code>Endpoint</code> subclasses and register their routes based on their <code>[Path()]</code> attributes</summary>
        /// <remarks>
        /// Maps the subclass's URL path template to <code>Run</code> with the subclass as the call's generic type
        /// </remarks>
        public static void All(IEndpointRouteBuilder endpoints)
        {
            var endpointType = typeof(Endpoint);
            if (endpointType.GetMethod("Run") is MethodInfo run)
            {
                foreach (var type in endpointType.Assembly.GetTypes())
                {
                    if (type.IsSubclassOf(endpointType))
                    {
                        if (type.GetCustomAttribute(typeof(Path)) is Path attr)
                        {
                            using (LogContext.PushProperty("MorphicEndpoint", type.ToString()))
                            using (LogContext.PushProperty("MorphicEndpointPath", attr.Template))
                            {
                                Log.Logger.Debug("Mapping MorphicEndpoint");
                                var generic = run.MakeGenericMethod(new Type[] {type});
                                endpoints.Map(attr.Template,
                                    generic.CreateDelegate(typeof(RequestDelegate)) as RequestDelegate);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Get the method reflection info that matches the given request method name</summary>
        /// <remarks>
        /// Will search this instance for a method that has a <code>[Method]</code> attribute matching the request method name
        /// </remarks>
        private MethodInfo? MethodInfoForRequestMethod(string requestMethod)
        {
            if (GetType().GetMethodForRequestMethod(requestMethod) is MethodInfo methodInfo)
            {
                return methodInfo;
            }
            return null;
        }

        /// <summary>Populate fields registered with <code>[Parameter]</code> attributes with values from the request URL</summary>
        private void PopulateParameterFields()
        {
            var routeData = Context.GetRouteData();
            foreach (var fieldInfo in GetType().GetFields())
            {
                if (fieldInfo.GetParameterName() is string name)
                {
                    object value;
                    if (routeData.Values.TryGetValue(name, out value))
                    {
                        fieldInfo.SetValue(this, value);
                    }
                }
            }
        }

        /// <summary>
        /// Called after <code>PopulateParameterFields()</code> but before the method handler to give the endpoint a chance
        /// to load the data of the resource it represents
        /// </summary>
        public virtual Task LoadResource()
        {
            return Task.CompletedTask;
        }

        public async Task<T> Load<T>(string id) where T: Record
        {
            var db = Context.GetDatabase();
            T? record = await db.Get<T>(id, ActiveSession);
            if (record == null){
                throw new HttpError(HttpStatusCode.NotFound);
            }
            return record;
        }

        public async Task Save<T>(T obj) where T: Record
        {
            var db = Context.GetDatabase();
            var success = await db.Save<T>(obj, ActiveSession);
            if (!success){
                throw new HttpError(HttpStatusCode.InternalServerError);
            }
        }

        public async Task Delete<T>(T obj) where T: Record
        {
            var db = Context.GetDatabase();
            var success = await db.Delete<T>(obj, ActiveSession);
            if (!success){
                throw new HttpError(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Convenience method for serializing an object to JSON as a response</summary>
        public async Task Respond<T>(T obj)
        {
            await Response.WriteJson(obj, Context.RequestAborted);
        }

        /// <summary>Return the logged in user or throw an exception</summary>
        public async Task<User> RequireUser()
        {
            var user = await Context.GetUser();
            if (user == null){
                Context.Response.Headers.Add("WWW-Authenticate", "Bearer");
                throw new HttpError(HttpStatusCode.Unauthorized);
            }
            return user;
        }

        public Database.Session? ActiveSession;

        public async Task WithTransaction(Func<Database.Session, Task> operations)
        {
            var db = Context.GetDatabase();
            var success = await db.WithTransaction(
                async (session) => 
                {
                    ActiveSession = session;
                    await operations(session);
                    ActiveSession = null;
                },
                Context.RequestAborted
            );
            if (!success){
                throw new HttpError(HttpStatusCode.InternalServerError);
            }
        }
    }

    public static class HttpContextExtensions
    {
        public static Database GetDatabase(this HttpContext context)
        {
            return context.RequestServices.GetRequiredService<Database>();
        }

        public static async Task<User?> GetUser(this HttpContext context)
        {
            var db = context.GetDatabase();
            if (context.Request.Headers["Authorization"].FirstOrDefault() is string authorization)
            {
                if (authorization.StartsWith("Bearer "))
                {
                    var providedToken = authorization.Substring(7);
                    var token = await db.Get<AuthToken>(providedToken);
                    if (token != null && token.UserId != null)
                    {
                        return await db.Get<User>(token.UserId);
                    }
                }
            }
            return null;
        }
    }
}