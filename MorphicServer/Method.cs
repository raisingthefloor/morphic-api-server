using System;
using System.Reflection;

namespace MorphicServer.Attributes
{
    /// <summary>Registers a handler for an HTTP method</summary>
    /// <remarks>
    /// Intended for use on instance methods of <code>Endpoint</code> subclasses, indicating which
    /// code method should be called for which HTTP request method.
    /// </remarks>
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class Method : System.Attribute
    {
        readonly string? name;
        
        /// <summary>Resgisters a handler for the HTTP method of the given name</summary>
        /// <param name="name">The name of the HTTP request method</param>
        public Method(string name)
        {
            this.name = name;
        }

        /// <summary>Registers a handler for the HTTP method of the same name as the uppercased code method</summary>
        public Method()
        {
        }
        
        /// <summary>The name of the HTTP method, or <code>null</code> if the name of the attributed code method should be used instead</summary>
        public string? Name
        {
            get { return name; }
        }
    }

    public static class MethodExtensions
    {
        /// <summary>Get the instance method for this type that handles the given request method</summary>
        /// <param name="requestMethod">The HTTP request method</param>
        /// <remarks>
        /// Searches the class's methods for one that has been registered using a <code>[Method]</code>
        /// attribute that matches the given HTTP request method name.
        /// </remarks>
        public static MethodInfo? GetMethodForRequestMethod(this Type type, string requestMethod)
        {
            foreach (var methodInfo in type.GetMethods())
            {
                if (methodInfo.GetRequestMethodName() == requestMethod)
                {
                    return methodInfo;
                }
            }
            return null;
        }

        /// <summary>Get the registered HTTP request method name for this code method, or <code>null</code> if no <code>[Method]</code> attribute has been attached</summary>
        /// <remarks>
        /// If the method registration did not include a specific name, the uppercased name of the code method will be used
        /// </remarks>
        public static string? GetRequestMethodName(this MethodInfo methodInfo)
        {
            if (Attribute.GetCustomAttribute(methodInfo, typeof(Method)) is Method attr)
            {
                return attr.Name ?? methodInfo.Name.ToUpper();
            }
            return null;
        }
    }
}