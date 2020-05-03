
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
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorphicServer
{

    /// <summary>Converter factory for all <code>MorphicServer</code> types.  Created converters throw exceptions for <code>null</code> values in non-nullable fields</summary>
    /// <remarks>
    /// Essentially, a non-nullable field is considered to be a required field for MorphicServer data types
    /// </remarks>
    public class NonNullableExceptionJsonConverter: JsonConverterFactory
    {

        /// <summary>Returns <code>true</code> for anything in the <code>MorphicServer</code> namespace
        public override bool CanConvert (Type typeToConvert)
        {
            return typeToConvert.Namespace == "MorphicServer";
        }

        public override System.Text.Json.Serialization.JsonConverter CreateConverter (Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            var converterGenericType = typeof(NonNullableExceptionConverter<>).MakeGenericType(new Type[] { typeToConvert });
            return (JsonConverter)Activator.CreateInstance(converterGenericType)!;
        }

        /// <summary>
        public class NonNullableExceptionConverter<T>: JsonConverter<T>
        {
            
            public override T Read (ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                // Create an instance using the empty constructor
                var instance = (T)Activator.CreateInstance(typeToConvert);
                if (instance == null)
                {
                    throw new JsonException();
                }

                // Get map of json property names to reflected PropertyInfo objects
                // - use the [JsonPropertyName] attribute if present
                // - use the [JsonExtensionData] attribute if present to place unmatched names
                // - respect the [JsonIgnore] attribute
                var propertiesByJsonName = new Dictionary<string, PropertyInfo>();
                Dictionary<string, object>? unknownPropertyDictionary = null;
                foreach (var propertyInfo in typeToConvert.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>() is JsonPropertyNameAttribute attr)
                    {
                        propertiesByJsonName.Add(attr.Name, propertyInfo);
                    }
                    else
                    {
                        propertiesByJsonName.Add(propertyInfo.Name, propertyInfo);
                    }
                    if (propertyInfo.GetCustomAttribute<JsonExtensionDataAttribute>() != null)
                    {
                        if (unknownPropertyDictionary != null)
                        {
                            throw new JsonException();
                        }
                        if (propertyInfo.PropertyType != typeof(Dictionary<string, object>))
                        {
                            throw new JsonException();
                        }
                        unknownPropertyDictionary = new Dictionary<string, object>();
                        propertyInfo.SetValue(instance, unknownPropertyDictionary);
                    }
                }

                // Read keys and values until the end of the object
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        CheckForNull(instance);
                        return instance;
                    }
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }
                    var propertyName = reader.GetString();
                    if (propertiesByJsonName.TryGetValue(propertyName, out var propertyInfo))
                    {
                        if (propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                        {
                            var value = JsonSerializer.Deserialize(ref reader, propertyInfo.PropertyType, options);
                            propertyInfo.SetValue(instance, value);
                        }
                        else
                        {
                            reader.Read();
                            reader.Skip();
                        }
                    }
                    else
                    {
                        if (unknownPropertyDictionary != null)
                        {
                            var value = JsonSerializer.Deserialize(ref reader, typeof(object), options);
                            unknownPropertyDictionary.Add(propertyName, value);
                        }
                        else
                        {
                            // Unknown property name and no JsonExtensionData. Instead of skipping, we could throw an exception.
                            reader.Read();
                            reader.Skip();
                        }
                    }
                }
                throw new JsonException();
            }

            public void CheckForNull(T instance)
            {
                var required = new List<string>();
                Type? type = instance!.GetType();
                while (type != null)
                {
                    foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    {
                        if (propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                        {
                            if (!propertyInfo.IsNullable())
                            {
                                if (propertyInfo.GetValue(instance) == null)
                                {
                                    var propertyName = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? propertyInfo.Name;
                                    required.Add(propertyName);
                                }
                            }
                        }
                    }
                    type = type.BaseType;
                }
                if (required.Count > 0){
                    throw new NullOrMissingProperties(required.ToArray());
                }
            }

            public override void Write (System.Text.Json.Utf8JsonWriter writer, T value, System.Text.Json.JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

        }

        public class NullOrMissingProperties: Exception
        {

            public string[] PropertyNames;

            public NullOrMissingProperties(string[] propertyNames): base()
            {
                PropertyNames = propertyNames;
            }

        }
    }

    public static class PropertyInfoExtensions
    {

        public static bool IsNullable(this PropertyInfo propertyInfo)
        {
            var type = propertyInfo.DeclaringType!;
            foreach (var attr in propertyInfo.CustomAttributes)
            {
                if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute")
                {
                    if (attr.ConstructorArguments.Count == 1)
                    {
                        byte flag = 0;
                        if (attr.ConstructorArguments[0].Value is byte[] infoBytes)
                        {
                            if (infoBytes.Length > 0)
                            {
                                flag = infoBytes[0];
                            }
                        }
                        else if (attr.ConstructorArguments[0].Value is byte info)
                        {
                            flag = info;
                        }
                        return flag == 2;
                    }
                    return false;
                }
            }
            foreach (var attr in type.CustomAttributes)
            {
                if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute")
                {
                    if (attr.ConstructorArguments.Count == 1)
                    {
                        if (attr.ConstructorArguments[0].Value is byte info)
                        {
                            return info == 2;
                        }
                    }
                    return false;
                }
            }
            return false;
        }

    }

}