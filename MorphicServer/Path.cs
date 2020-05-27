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
using System.Collections.Generic;

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
        public string Template
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

        /// <summary>Gets the registered URL path template for this class, or <code>null</code> if no <code>[Path()]</code> attribute was specified</summary>
        public static string? GetRoutePath(this Type type, Dictionary<string, string> pathParameters)
        {
            if (type.GetRoutePath() is string path)
            {
                foreach (var pair in pathParameters)
                {
                    path = path.Replace($"{pair.Key}", pair.Value);
                }
                return path;
            }
            return null;
        }
    }
}