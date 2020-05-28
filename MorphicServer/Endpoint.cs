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
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Prometheus;
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

        #region Creating an Endpoint

        public Endpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger)
        {
            Context = contextAccessor.HttpContext;
            Request = Context.Request;
            Response = Context.Response;
            settings = Context.RequestServices.GetRequiredService<MorphicSettings>();
            this.logger = logger;
            AddAllowedOriginsFromAttributes();
            Response.OnStarting(() =>
            {
                SetCrossOriginHeaders();
                return Task.CompletedTask;
            });
        }
        
        protected readonly ILogger<Endpoint> logger;

        #endregion

        #region Request & Response Information

        /// <summary>The http context for the current request</summary>
        public HttpContext Context { get; private set; }
        /// <summary>The current HTTP request</summary>
        public HttpRequest Request { get; private set; }
        /// <summary>The current HTTP Response</summary>
        public HttpResponse Response { get; private set; }

        #endregion

        #region Site Configuration

        protected MorphicSettings settings;

        #endregion

        #region Metrics

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

        #endregion

        #region Invocation
        
        /// <summary>Used as the <code>RequestDelegate</code> for the route corresponding to each <code>Endpoint</code> subclass</summary>
        /// <remarks>
        /// Creates and populates the endpoint, calls <code>LoadResource()</code>, then invokes the relevant method
        /// </remarks>
        public static async Task Run<T>(HttpContext context) where T: Endpoint
        {
            var endpoint = context.RequestServices.GetRequiredService<T>();
            var logger = endpoint.logger;
            var method = context.Request.Method;
            var statusCode = 500;
            var pathAttr = endpoint.GetType().GetCustomAttribute(typeof(PathAttribute)) as PathAttribute;
            var omitMetrics = endpoint.GetType().GetCustomAttribute(typeof(OmitMetricsAttribute)) as OmitMetricsAttribute;
            var path = pathAttr?.Template;

            if (String.IsNullOrEmpty(path))
            {
                logger.LogError("Unknown path");
                path = "(unknown)";
            }

            var clientIp = context.Request.ClientIp();
            if (clientIp == null)
            {
                logger.LogWarning("No client IP could be found for request");
                clientIp = "";
            }

            using (LogContext.PushProperty("ClientIp", clientIp))
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
                        if (omitMetrics == null)
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
                catch (OperationCanceledException)
                {
                    // happens when the remote closes the connection sometimes
                    logger.LogInformation("caught OperationCanceledException");
                }
                catch (BadHttpRequestException)
                {
                    // happens when the remote closes the connection sometimes
                    logger.LogInformation("caught BadHttpRequestException");
                }
                finally
                {
                    stopWatch.Stop();
                    if (omitMetrics == null)
                    {
                        histogram.Labels(path, method, statusCode.ToString())
                            .Observe(stopWatch.Elapsed.TotalSeconds);
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
            RouteData routeData = null;
            try
            {
                routeData = Context.GetRouteData();
            }catch{

            }
            if (routeData != null)
            {
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
        }

        #endregion

        #region Registration

        /// <summary>Find all <code>Endpoint</code> subclasses and register their routes based on their <code>[Path()]</code> attributes</summary>
        /// <remarks>
        /// Maps the subclass's URL path template to <code>Run</code> with the subclass as the call's generic type
        /// </remarks>
        public static void All(IEndpointRouteBuilder endpoints)
        {
            var endpointType = typeof(Endpoint);
            if (endpointType.GetMethod("Run") is MethodInfo run)
            {
                foreach (var (type, attr) in SubclassesWithPaths)
                {
                    var generic = run.MakeGenericMethod(new Type[] {type});
                    endpoints.Map(attr.Template,
                        generic.CreateDelegate(typeof(RequestDelegate)) as RequestDelegate);
                }
            }
        }

        internal static IEnumerable<(Type, PathAttribute)> SubclassesWithPaths = FindSubclassesWithPaths();

        private static IEnumerable<(Type, PathAttribute)> FindSubclassesWithPaths()
        {
            var subclasses = new List<(Type, PathAttribute)>();
            var endpointType = typeof(Endpoint);
            foreach (var type in endpointType.Assembly.GetTypes())
            {
                if (type.IsSubclassOf(endpointType))
                {
                    if (type.GetCustomAttribute(typeof(PathAttribute)) is PathAttribute attr)
                    {
                        subclasses.Add((type, attr));
                    }
                }
            }
            return subclasses;
        }

        #endregion

        #region Database Operations

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
            return await Load<T>(r => r.Id == id);
        }

        public async Task<T> Load<T>(Expression<Func<T, bool>> filter) where T : Record
        {
            var db = Context.GetDatabase();
            T? record = await db.Get<T>(filter, ActiveSession);
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

        public async Task Delete<T>(Expression<Func<T, bool>> filter) where T : Record
        {
            var db = Context.GetDatabase();
            var success = await db.Delete<T>(filter, ActiveSession);
            if (!success){
                throw new HttpError(HttpStatusCode.InternalServerError);
            }
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

        #endregion

        #region Writing Responses

        /// <summary>Convenience method for serializing an object to JSON as a response</summary>
        public async Task Respond<T>(T obj)
        {
            await Response.WriteJson(obj, Context.RequestAborted);
        }

        #endregion

        #region Authentication

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

        #endregion

        #region Cross Origin

        public class AllowedOrigin
        {

            public AllowedOrigin(string origin, string[] methods, string[] headers)
            {
                Origin = origin;
                Methods = methods;
                Headers = headers;
            }
            
            public AllowedOrigin(string origin): this(origin, AllMethods, DefaultHeaders)
            {
            }

            public AllowedOrigin(string origin, string[] methods): this(origin, methods, DefaultHeaders)
            {
            }

            public AllowedOrigin(string origin, AllowedOrigin other): this(origin, other.Methods, other.Headers)
            {
                Varies = false;
            }

            public static string[] AllMethods = { "*" };
            public static string[] DefaultHeaders = { "Content-Type", "Authorization" };

            public string Origin { get; }
            public string[] Methods { get; }
            public string[] Headers { get; }
            public bool Varies { get; } = true;
        }

        private Dictionary<string, AllowedOrigin> allowedOrigins = new Dictionary<string, AllowedOrigin>();

        public void AddAllowedOriginsFromAttributes()
        {
            var type = this.GetType();
            var allowedOrigins = new List<Endpoint.AllowedOrigin>();
            if (type.GetCustomAttributes(typeof(AllowedOriginAttribute)) is IEnumerable<Attribute> attrs)
            {
                foreach (var attr in attrs)
                {
                    if (attr is AllowedOriginAttribute allowedAttr)
                    {
                        var allowedOrigin = new AllowedOrigin(allowedAttr.Origin, allowedAttr.Methods, allowedAttr.Headers);
                        AddAllowedOrigin(allowedOrigin);
                    }
                }
            }
        }

        public void AddAllowedOrigin(AllowedOrigin allowedOrigin)
        {
            allowedOrigins.Add(allowedOrigin.Origin, allowedOrigin);
        }

        public void AddAllowedOrigin(Uri origin)
        {
            if (origin.IsAbsoluteUri && !String.IsNullOrEmpty(origin.Scheme) && !String.IsNullOrEmpty(origin.Host))
            {
                var builder = new UriBuilder();
                builder.Scheme = origin.Scheme;
                builder.Host = origin.Host;
                builder.Port = origin.Port;
                AddAllowedOrigin(new AllowedOrigin(builder.Uri.ToString().TrimEnd('/')));
            }
        }

        protected void SetCrossOriginHeaders()
        {
            if (EffectiveAllowedOrigin is AllowedOrigin allowed)
            {
                Response.Headers.Add("Access-Control-Allow-Origin", allowed.Origin);
                if (allowed.Varies){
                    Response.Headers.Add("Vary", "Origin");
                }
                if (Request.Headers["Access-Control-Request-Method"].Count > 0)
                {
                    Response.Headers.Add("Access-Control-Allow-Methods", String.Join(", ", allowed.Methods));
                }
                if (Request.Headers["Access-Control-Request-Headers"].Count > 0)
                {
                    Response.Headers.Add("Access-Control-Allow-Headers", String.Join(", ", allowed.Headers));
                }
            }
        }

        private AllowedOrigin? EffectiveAllowedOrigin
        {
            get
            {
                if (Request.Headers["Origin"].FirstOrDefault() is string origin)
                {
                    if (allowedOrigins.TryGetValue(origin, out var allowed))
                    {
                        return allowed;
                    }
                    if (allowedOrigins.TryGetValue("*", out var wildcard))
                    {
                        return new AllowedOrigin(origin, wildcard);
                    }
                }
                return null;
            }
        }

        [Method]
        public Task Options()
        {
            Response.Headers.Add("Access-Control-Max-Age", "360");
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Task.CompletedTask;
        }

        #endregion

        #region Generating URLs to Endpoints

        public Uri ServerUri
        {
            get
            {
                // First, use the value from MorphicSettings if specified
                if (settings.ServerUri is Uri settingsUri)
                {
                    return settingsUri;
                }

                /// Next, use the value from the requset
                if (Request.GetServerUri() is Uri requestUri)
                {
                    return requestUri;
                }

                return new Uri("", UriKind.Relative);
            }
        }

        public Uri GetUri<T>(Dictionary<string, string> pathParameters) where T: Endpoint
        {
            var type = typeof(T);
            var builder = new UriBuilder(ServerUri);
            builder.Path = type.GetRoutePath(pathParameters);
            return builder.Uri;
        }

        #endregion
    }

    #region Error Responses

    /// <summary>
    /// Base class for error Responses for Morphic APIs.
    /// </summary>
    public class BadRequestResponse
    {
        [JsonPropertyName("error")] 
        public string Error { get; set; }

        [JsonPropertyName("details")]
        public Dictionary<string, object>? Details { get; set; }

        public BadRequestResponse(string error)
        {
            Error = error;
        }
        public BadRequestResponse(string error, Dictionary<string, object> details)
        {
            Error = error;
            Details = details;
        }
    }

    #endregion

    #region HttpContext Extensions

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

    #endregion

    #region IServiceCollection Extensions

    public static class IServiceCollectionExtensions
    {
        public static void AddEndpoints(this IServiceCollection services)
        {
            foreach (var (type, attr) in Endpoint.SubclassesWithPaths)
            {
                services.AddTransient(type);
            }
        }
    }

    #endregion
}