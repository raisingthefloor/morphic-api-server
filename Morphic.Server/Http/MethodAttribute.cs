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
using System.Reflection;

namespace Morphic.Server.Http
{
    /// <summary>Registers a handler for an HTTP method</summary>
    /// <remarks>
    /// Intended for use on instance methods of <code>Endpoint</code> subclasses, indicating which
    /// code method should be called for which HTTP request method.
    /// </remarks>
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class MethodAttribute : System.Attribute
    {
        readonly string? name;
        
        /// <summary>Registers a handler for the HTTP method of the given name</summary>
        /// <param name="name">The name of the HTTP request method</param>
        public MethodAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>Registers a handler for the HTTP method of the same name as the uppercased code method</summary>
        public MethodAttribute()
        {
        }
        
        /// <summary>The name of the HTTP method, or <code>null</code> if the name of the attributed code method should be used instead</summary>
        public string? Name
        {
            get { return name; }
        }

        /// <summary>Indicates the entire method should be part of a database transaction
        public bool RunInTransaction { get; set; } = false;
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
            if (Attribute.GetCustomAttribute(methodInfo, typeof(MethodAttribute)) is MethodAttribute attr)
            {
                return attr.Name ?? methodInfo.Name.ToUpper();
            }
            return null;
        }

        /// <summary>Get the value of the <code>RunInTransaction</code> property of the <code>[Method]</code> attribute</summary>
        public static bool GetRunInTransaction(this MethodInfo methodInfo)
        {
            if (Attribute.GetCustomAttribute(methodInfo, typeof(MethodAttribute)) is MethodAttribute attr)
            {
                return attr.RunInTransaction;
            }
            return false;
        }
    }
}