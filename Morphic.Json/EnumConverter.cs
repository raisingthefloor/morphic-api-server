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

    public class EnumConverterFactory: JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterGenericType = typeof(Converter<>).MakeGenericType(new Type[] { typeToConvert });
            return (JsonConverter)Activator.CreateInstance(converterGenericType)!;
        }

        public class Converter<E>: JsonConverter<E> where E: struct
        {

            private JsonNamingPolicy NamingPolicy = new UnderscoreNamingPolicy();

            public override E Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var stringValue = reader.GetString();
                E value;
                if (Enum.TryParse<E>(stringValue.Replace("_", ""), true, out value))
                {
                    return value;
                }
                if (Enum.TryParse<E>(stringValue, true, out value))
                {
                    return value;
                }
                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, E instance, JsonSerializerOptions options)
            {
                var stringValue = instance.ToString();
                stringValue = NamingPolicy.ConvertName(stringValue);
                writer.WriteStringValue(stringValue);
            }
        }
    }

}