using System;
using Xunit;
using MorphicServer;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorphicServer.Tests
{
    public class JsonExtensionTests
    {
        [Fact]
        public void TestBooleans()
        {
            var json = @"{""a"": true, ""b"": false}";
            var options = new JsonSerializerOptions();
            var converter = new JsonElementInferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object o;
            Assert.True(result.TryGetValue("a", out o));
            Assert.IsType(typeof(bool), o);
            Assert.StrictEqual((bool)o, true);
            Assert.True(result.TryGetValue("b", out o));
            Assert.IsType(typeof(bool), o);
            Assert.StrictEqual((bool)o, false);
        }

        [Fact]
        public void TestIntegers()
        {
            var json = @"{""a"": 12, ""b"": 0}";
            var options = new JsonSerializerOptions();
            var converter = new JsonElementInferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object o;
            Assert.True(result.TryGetValue("a", out o));
            Assert.IsType(typeof(long), o);
            Assert.Equal((long)o, 12);
            Assert.True(result.TryGetValue("b", out o));
            Assert.IsType(typeof(long), o);
            Assert.Equal((long)o, 0);
        }

        [Fact]
        public void TestFloats()
        {
            var json = @"{""a"": 12.5, ""b"": 0.1}";
            var options = new JsonSerializerOptions();
            var converter = new JsonElementInferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object o;
            Assert.True(result.TryGetValue("a", out o));
            Assert.IsType(typeof(double), o);
            Assert.True(Math.Abs(12.5 - (double)o) < 0.001);
            Assert.True(result.TryGetValue("b", out o));
            Assert.IsType(typeof(double), o);
            Assert.True(Math.Abs(0.1 - (double)o) < 0.001);
        }

        [Fact]
        public void TestStrings()
        {
            var json = @"{""a"": ""hello"", ""b"": """"}";
            var options = new JsonSerializerOptions();
            var converter = new JsonElementInferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object o;
            Assert.True(result.TryGetValue("a", out o));
            Assert.IsType(typeof(string), o);
            Assert.Equal((string)o, "hello");
            Assert.True(result.TryGetValue("b", out o));
            Assert.IsType(typeof(string), o);
            Assert.Equal((string)o, "");
        }

        [Fact]
        public void TestNull()
        {
            var json = @"{""a"": null}";
            var options = new JsonSerializerOptions();
            var converter = new JsonElementInferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object o;
            Assert.True(result.TryGetValue("a", out o));
            Assert.Null(o);
        }

        [Fact]
        public void TestArray()
        {
            var json = @"{""a"": [1, ""two"", null, 4.1, true]}";
            var options = new JsonSerializerOptions();
            var converter = new JsonElementInferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object o;
            Assert.True(result.TryGetValue("a", out o));
            Assert.IsType(typeof(object[]), o);
            var array = (object[])o;
            Assert.Equal(array.Length, 5);

            Assert.IsType(typeof(long), array[0]);
            Assert.Equal((long)array[0], 1);
            Assert.IsType(typeof(string), array[1]);
            Assert.Equal((string)array[1], "two");
            Assert.Null(array[2]);
            Assert.IsType(typeof(double), array[3]);
            Assert.True(Math.Abs(4.1 - (double)array[3]) < 0.001);
            Assert.IsType(typeof(bool), array[4]);
            Assert.Equal((bool)array[4], true);
        }

        [Fact]
        public void TestObject()
        {
            var json = @"{""a"": {""first"": 1, ""second"": ""two""}}";
            var options = new JsonSerializerOptions();
            var converter = new JsonElementInferredTypeConverter();
            options.Converters.Add(converter);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            object o;
            Assert.True(result.TryGetValue("a", out o));
            Assert.IsType(typeof(Dictionary<string, object>), o);
            var dictionary = (Dictionary<string, object>)o;
            Assert.True(dictionary.TryGetValue("first", out o));
            Assert.IsType(typeof(long), o);
            Assert.Equal((long)o, 1);
            Assert.True(dictionary.TryGetValue("second", out o));
            Assert.IsType(typeof(string), o);
            Assert.Equal((string)o, "two");
        }
    }
}
