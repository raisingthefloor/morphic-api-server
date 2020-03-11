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