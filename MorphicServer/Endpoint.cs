using System;
using System.Reflection;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MorphicServer.Attributes;

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
            try
            {
                endpoint.PopulateParameterFields();
                await endpoint.LoadResource();
                if (endpoint.MethodInfoForRequestMethod(context.Request.Method) is MethodInfo methodInfo)
                {
                    if (methodInfo.Invoke(endpoint, new object[] { }) is Task task)
                    {
                        await task;
                    }
                }
                else
                {
                    // If the class doesn't have a matching method, respond with MethodNotAllowed
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                }
            }catch (HttpError error){
                await context.Response.WriteError(error, context.RequestAborted);
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
                            var generic = run.MakeGenericMethod(new Type[] { type });
                            endpoints.Map(attr.Template, generic.CreateDelegate(typeof(RequestDelegate)) as RequestDelegate);
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
            T? record = await db.Get<T>(id);
            if (record == null){
                throw new HttpError(HttpStatusCode.NotFound);
            }
            return record;
        }

        public async Task Save<T>(T obj) where T: Record
        {
            var db = Context.GetDatabase();
            var success = await db.Save<T>(obj);
            if (!success){
                throw new HttpError(HttpStatusCode.InternalServerError);
            }
        }

        public async Task Delete<T>(T obj) where T: Record
        {
            var db = Context.GetDatabase();
            var success = await db.Delete<T>(obj);
            if (!success){
                throw new HttpError(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Convenience method for serializing an object to JSON as a response</summary>
        public async Task Respond<T>(T obj)
        {
            await Response.WriteJson(obj, Context.RequestAborted);
        }
    }

    public static class HttpContextExtensions
    {
        public static Database GetDatabase(this HttpContext context)
        {
            return context.RequestServices.GetRequiredService<Database>();
        }
    }
}