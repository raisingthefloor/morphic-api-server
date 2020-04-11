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

namespace MorphicServer.Attributes
{
    /// <summary>Registers an instance field to obtain its value from the <code>RouteData</code> extracted from the URL path template</summary>
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class Parameter : System.Attribute
    {
        readonly string? name;
        
        /// <summary>Registers an instance field to obtain its value from the URL path template parameter of the given name</summary>
        /// <param name="name">The name of the <code>RouteData.Value</code> entry to lookup the fields value.
        public Parameter(string name)
        {
            this.name = name;
        }

        /// <summary>Registers an instance field to obtain its value from the URL path template parameter of same name as the lowercased field name</summary>
        public Parameter()
        {
        }
        
        /// <summary>The name of URL path template parameter, or <code>null</code> if the attached field's name should be used</summary>
        public string? Name
        {
            get { return name; }
        }
    }

    public static class ParameterExtensions
    {

        /// <summary>Get the registered URL path template parameter name for this field, or <code>null</code> if no <code>[Parameter]</code> attribute has been attached</summary>
        /// <remarks>
        /// If the parameter registration did not include a specific name, the lowercased name of the field will be used
        /// </remarks>
        public static string? GetParameterName(this FieldInfo fieldInfo)
        {
            if (Attribute.GetCustomAttribute(fieldInfo, typeof(Parameter)) is Parameter attr)
            {
                return attr.Name ?? fieldInfo.Name.ToLower();
            }
            return null;
        }
    }
}