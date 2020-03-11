using System;
using System.Reflection;

namespace MorphicServer.Attributes
{
    /// <summary>Registers the URL path template that is handled by an <code>Endpoint</code> subclass</summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class Path : System.Attribute
    {
        readonly string template;
        

        /// <summary>Registers the URL path template that is handled by an <code>Endpoint</code> subclass</summary>
        /// <param name="template">The URL path template, like <code>"/some/path/{param}"</code></param>
        /// <remarks>
        /// The template will be used in a call to <code>IEndpointRouteBuilder.Map()</code>
        /// </remarks>
        public Path(string template)
        {
            this.template = template;
        }

        /// <summary name="template">The URL path template, like <code>"/some/path/{param}"</code></summary>
        public string? Template
        {
            get { return template; }
        }
    }

    public static class PathExtensions
    {

        /// <summary>Gets the registered URL path template for this class, or <code>null</code> if no <code>[Path()]</code> attribute was specified</summary>
        public static string? GetRoutePath(this Type type)
        {
            if (type.GetCustomAttribute(typeof(Path)) is Path attr)
            {
                return attr.Template;
            }
            return null;
        }
    }
}