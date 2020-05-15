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
using Xunit;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Morphic.Json.Tests
{
    public class ModelConverterTests
    {

        public ModelConverterTests()
        {
            options = new JsonSerializerOptions();
            options.Converters.Add(new ModelConverterFactory("Morphic.Json.Tests"));
        }

        private JsonSerializerOptions options;

        [Fact]
        public void TestDeserializePropertiesOnly()
        {
            var json = @"{""Id"": ""Hello"", ""OtherId"": ""World""}";
            var instance = JsonSerializer.Deserialize<PropertiesOnlyModel>(json, options);
            Assert.Equal("Hello", instance.Id);
            Assert.Equal("", instance.OtherId);
        }

        class PropertiesOnlyModel
        {
            public string Id { get; set; } = "";
            public string OtherId = "";
        }

        [Fact]
        public void TestDeserializeNullRequired()
        {
            var json = @"{""Id"": ""Hello""}";
            var instance = JsonSerializer.Deserialize<NullRequiredModel>(json, options);
            Assert.Equal(instance.Id, "Hello");
            Assert.Null(instance.OtherId);

            json = @"{""Id"": ""Hello"", ""OtherId"": ""World""}";
            instance = JsonSerializer.Deserialize<NullRequiredModel>(json, options);
            Assert.Equal("Hello", instance.Id);
            Assert.NotNull(instance.OtherId);
            Assert.Equal("World", instance.OtherId);

            json = @"{}";
            Assert.Throws<NullOrMissingProperties>(() =>
            {
                var instance = JsonSerializer.Deserialize<NullRequiredModel>(json, options);
            });

            json = @"{""OtherId"": ""World""}";
            Assert.Throws<NullOrMissingProperties>(() =>
            {
                var instance = JsonSerializer.Deserialize<NullRequiredModel>(json, options);
            });
        }

        #nullable enable

        class NullRequiredModel
        {
            public string Id { get; set; } = null!;
            public string? OtherId { get; set; }
        }

        #nullable disable

        [Fact]
        public void TestDeserializeJsonRequired()
        {
            var json = @"{""Id"": ""Hello"", ""Flag"": true}";
            var instance = JsonSerializer.Deserialize<JsonRequiredModel>(json, options);
            Assert.Equal(instance.Id, "Hello");
            Assert.True(instance.Flag);
            Assert.False(instance.OtherFlag);

            json = @"{""Id"": ""Hello"", ""Flag"": true, ""OtherFlag"": true}";
            instance = JsonSerializer.Deserialize<JsonRequiredModel>(json, options);
            Assert.Equal("Hello", instance.Id);
            Assert.True(instance.Flag);
            Assert.True(instance.OtherFlag);

            json = @"{}";
            Assert.Throws<NullOrMissingProperties>(() =>
            {
                var instance = JsonSerializer.Deserialize<JsonRequiredModel>(json, options);
            });

            json = @"{""OtherFlag"": true}";
            Assert.Throws<NullOrMissingProperties>(() =>
            {
                var instance = JsonSerializer.Deserialize<JsonRequiredModel>(json, options);
            });

            json = @"{""Id"": ""Hello"", ""OtherFlag"": true}";
            Assert.Throws<NullOrMissingProperties>(() =>
            {
                var instance = JsonSerializer.Deserialize<JsonRequiredModel>(json, options);
            });

            json = @"{""Flag"": true, ""OtherFlag"": true}";
            Assert.Throws<NullOrMissingProperties>(() =>
            {
                var instance = JsonSerializer.Deserialize<JsonRequiredModel>(json, options);
            });
        }

        #nullable enable

        class JsonRequiredModel
        {
            public string Id { get; set; } = null!;
            [JsonRequired]
            public bool Flag { get; set; } = false;
            public bool OtherFlag { get; set; } = false;
        }

        #nullable disable

        [Fact]
        public void TestDeserilaizeJsonIgnore()
        {
            var json = @"{""Id"": ""Hello"", ""OtherId"": ""World""}";
            var instance = JsonSerializer.Deserialize<JsonIgnoreModel>(json, options);
            Assert.Equal("Hello", instance.Id);
            Assert.Null(instance.OtherId);

            json = @"{""Id"": ""Hello""}";
            instance = JsonSerializer.Deserialize<JsonIgnoreModel>(json, options);
            Assert.Equal("Hello", instance.Id);
            Assert.Null(instance.OtherId);
        }

        #nullable enable

        class JsonIgnoreModel
        {
            public string Id { get; set; } = "";
            [JsonIgnore]
            public string OtherId { get; set; } = null!;
        }

        #nullable disable

        [Fact]
        public void TestDeserializeJsonPropertyName()
        {
            var json = @"{""identifier"": ""Hello"", ""other_id"": ""World""}";
            var instance = JsonSerializer.Deserialize<PropertyNameModel>(json, options);
            Assert.Equal("Hello", instance.Id);
            Assert.Equal("World", instance.OtherId);

            json = @"{""identifier"": ""Hello""}";
            instance = JsonSerializer.Deserialize<PropertyNameModel>(json, options);
            Assert.Equal("Hello", instance.Id);
            Assert.Null(instance.OtherId);

            json = @"{}";
            try
            {
                instance = JsonSerializer.Deserialize<PropertyNameModel>(json, options);
                Assert.Null(instance);
            }
            catch (NullOrMissingProperties e)
            {
                Assert.Equal(1, e.PropertyNames.Length);
                Assert.Equal("identifier", e.PropertyNames[0]);
            }

        }

        #nullable enable

        class PropertyNameModel
        {
            [JsonPropertyName("identifier")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("other_id")]
            public string? OtherId { get; set; }
        }

        #nullable disable

        [Fact]
        public void TestDeserializeJsonExtensionData()
        {
            var json = @"{""Id"": ""Hello"", ""One"": 1, ""Two"": ""World""}";
            var instance = JsonSerializer.Deserialize<JsonExtensionDataModel>(json, options);
            Assert.Equal("Hello", instance.Id);
            Assert.NotNull(instance.Extras);
            Assert.Equal(2, instance.Extras.Count);
            object value;
            Assert.True(instance.Extras.TryGetValue("One", out value));
            Assert.IsType<JsonElement>(value);
            var element = (JsonElement)value;
            Assert.Equal(JsonValueKind.Number, element.ValueKind);
            Assert.Equal(1, element.GetInt32());
            Assert.True(instance.Extras.TryGetValue("Two", out value));
            Assert.IsType<JsonElement>(value);
            element = (JsonElement)value;
            Assert.Equal(JsonValueKind.String, element.ValueKind);
            Assert.Equal("World", element.GetString());
        }

        #nullable enable

        class JsonExtensionDataModel
        {

            public string Id { get; set; } = null!;

            [JsonExtensionData]
            public Dictionary<string, object> Extras { get; set; } = null!;
        }

        #nullable disable

        // TestNested
    }
}
