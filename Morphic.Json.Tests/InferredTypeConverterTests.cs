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
using Morphic.Json;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Morphic.Json.Tests
{
    public class InferredTypeConverterTests
    {
        [Fact]
        public void TestBooleans()
        {
            var json = @"{""a"": true, ""b"": false}";
            var options = new JsonSerializerOptions();
            var converter = new InferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object value;
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<bool>(value);
            Assert.True((bool)value);
            Assert.True(result.TryGetValue("b", out value));
            Assert.IsType<bool>(value);
            Assert.False((bool)value);
        }

        [Fact]
        public void TestIntegers()
        {
            var json = @"{""a"": 12, ""b"": 0}";
            var options = new JsonSerializerOptions();
            var converter = new InferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object value;
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<long>(value);
            Assert.Equal(12, (long)value);
            Assert.True(result.TryGetValue("b", out value));
            Assert.IsType<long>(value);
            Assert.Equal(0, (long)value);
        }

        [Fact]
        public void TestFloats()
        {
            var json = @"{""a"": 12.5, ""b"": 0.1}";
            var options = new JsonSerializerOptions();
            var converter = new InferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object value;
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<double>(value);
            Assert.True(Math.Abs(12.5 - (double)value) < 0.001);
            Assert.True(result.TryGetValue("b", out value));
            Assert.IsType<double>(value);
            Assert.True(Math.Abs(0.1 - (double)value) < 0.001);
        }

        [Fact]
        public void TestStrings()
        {
            var json = @"{""a"": ""hello"", ""b"": """"}";
            var options = new JsonSerializerOptions();
            var converter = new InferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object value;
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<string>(value);
            Assert.Equal("hello", (string)value);
            Assert.True(result.TryGetValue("b", out value));
            Assert.IsType<string>(value);
            Assert.Equal("", (string)value);
        }

        [Fact]
        public void TestNull()
        {
            var json = @"{""a"": null}";
            var options = new JsonSerializerOptions();
            var converter = new InferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object value;
            Assert.True(result.TryGetValue("a", out value));
            Assert.Null(value);
        }

        [Fact]
        public void TestArray()
        {
            var json = @"{""a"": [1, ""two"", null, 4.1, true]}";
            var options = new JsonSerializerOptions();
            var converter = new InferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object value;
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<object[]>(value);
            var array = (object[])value;
            Assert.Equal(5, array.Length);

            Assert.IsType<long>(array[0]);
            Assert.Equal(1, (long)array[0]);
            Assert.IsType<string>(array[1]);
            Assert.Equal("two", (string)array[1]);
            Assert.Null(array[2]);
            Assert.IsType<double>(array[3]);
            Assert.True(Math.Abs(4.1 - (double)array[3]) < 0.001);
            Assert.IsType<bool>(array[4]);
            Assert.True((bool)array[4]);
            
            json = @"{""a"": []}";
            result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<object[]>(value);
            array = (object[])value;
            Assert.Equal(0, array.Length);
            
            json = @"{""a"": [[1,true],{""b"": ""hi""}]}";
            result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<object[]>(value);
            array = (object[])value;
            Assert.Equal(2, array.Length);

            Assert.IsType<object[]>(array[0]);
            Assert.Equal(2, ((object[])array[0]).Length);
            Assert.IsType<long>(((object[])array[0])[0]);
            Assert.Equal(1, (long)((object[])array[0])[0]);
            Assert.IsType<bool>(((object[])array[0])[1]);
            Assert.True((bool)((object[])array[0])[1]);
            Assert.IsType<Dictionary<string, object>>(array[1]);
            Assert.Equal("hi", ((Dictionary<string, object>)array[1])["b"]);
        }

        [Fact]
        public void TestObject()
        {
            var json = @"{""a"": {""first"": 1, ""second"": ""two""}}";
            var options = new JsonSerializerOptions();
            var converter = new InferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object value;
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<Dictionary<string, object>>(value);
            var dictionary = (Dictionary<string, object>)value;
            Assert.True(dictionary.TryGetValue("first", out value));
            Assert.IsType<long>(value);
            Assert.Equal(1, (long)value);
            Assert.True(dictionary.TryGetValue("second", out value));
            Assert.IsType<string>(value);
            Assert.Equal("two", (string)value);

            json = @"{""a"": {}}";
            result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<Dictionary<string, object>>(value);
            dictionary = (Dictionary<string, object>)value;
            Assert.Equal(0, dictionary.Count);

            json = @"{""a"": {""first"": [1, false], ""second"": {""b"": ""hi""}}}";
            result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            Assert.True(result.TryGetValue("a", out value));
            Assert.IsType<Dictionary<string, object>>(value);
            dictionary = (Dictionary<string, object>)value;

            Assert.True(dictionary.TryGetValue("first", out value));
            Assert.IsType<object[]>(value);
            var first = (object[])value;
            Assert.Equal(2, first.Length);
            Assert.IsType<long>(first[0]);
            Assert.Equal(1, (long)first[0]);
            Assert.IsType<bool>(first[1]);
            Assert.False((bool)first[1]);

            Assert.True(dictionary.TryGetValue("second", out value));
            Assert.IsType<Dictionary<string, object>>(value);
            var second = (Dictionary<string, object>)value;
            Assert.True(second.TryGetValue("b", out value));
            Assert.IsType<string>(value);
            Assert.Equal("hi", (string)value);
        }
    }
}
