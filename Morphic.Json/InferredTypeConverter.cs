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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Morphic.Json
{

    /// <summary>Deserialize into standard types like <code>long, string, object[], or Dictionary&lt;string, object></code></summary>
    /// <remarks>
    /// For cases such as <code>Preferences</code> where we allow arbirary JSON and can't indicate the correct types up-front in a class definition.
    /// System.Text.Json doesn't do this automatically, so we need to write our own.
    /// This implementation simply leverages the <code>Utf8JsonReader.GetObject</code> extension found below.
    /// </remarks>
    public class InferredTypeConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetInferredTypeObject(options);
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            throw new InvalidOperationException("For JSON deserialization only");
        }
    }

    public static class JsonReaderExtensions
    {

        /// <summary>Return an appropriate primitive type for the current reader token</summary>
        public static object GetInferredTypeObject(this ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    {
                        var dictionary = new Dictionary<string, object>();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndObject)
                            {
                                return dictionary;
                            }
                            if (reader.TokenType != JsonTokenType.PropertyName)
                            {
                                throw new JsonException("Expecting property name");
                            }
                            var key = reader.GetString();
                            var value = JsonSerializer.Deserialize(ref reader, typeof(object), options);
                            dictionary.Add(key, value);
                        }
                        throw new JsonException("Expecting end of object");
                    }
                case JsonTokenType.StartArray:
                    {
                        var array = new List<object>();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray)
                            {
                                return array.ToArray();
                            }
                            var value = JsonSerializer.Deserialize(ref reader, typeof(object), options);
                            array.Add(value);
                        }
                        throw new JsonException("Expecting end of array");
                    }
                case JsonTokenType.Null:
                    return null!;
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var longValue))
                    {
                        return longValue;
                    }
                    return reader.GetDouble();
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.True:
                case JsonTokenType.False:
                    return reader.GetBoolean();
                default:
                    throw new JsonException("Unepxected token");
            }
        }
    }

}