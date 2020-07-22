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
    public class EnumConverterTests
    {
        [Fact]
        public void TestEnumConverter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new EnumConverterFactory());
            var json = JsonSerializer.Serialize<TestEnum>(TestEnum.One, options);
            Assert.Equal("\"one\"", json);
            json = JsonSerializer.Serialize<TestEnum>(TestEnum.Two, options);
            Assert.Equal("\"two\"", json);
            json = JsonSerializer.Serialize<TestEnum>(TestEnum.ThirdOption, options);
            Assert.Equal("\"third_option\"", json);
            json = JsonSerializer.Serialize<TestEnum>(TestEnum.Fourth_Option, options);
            Assert.Equal("\"fourth_option\"", json);

            var value = JsonSerializer.Deserialize<TestEnum>("\"one\"", options);
            Assert.Equal(TestEnum.One, value);
            value = JsonSerializer.Deserialize<TestEnum>("\"two\"", options);
            Assert.Equal(TestEnum.Two, value);
            value = JsonSerializer.Deserialize<TestEnum>("\"third_option\"", options);
            Assert.Equal(TestEnum.ThirdOption, value);
            value = JsonSerializer.Deserialize<TestEnum>("\"fourth_option\"", options);
            Assert.Equal(TestEnum.Fourth_Option, value);

            Assert.Throws<JsonException>(() =>
            {
                var value = JsonSerializer.Deserialize<TestEnum>("\"notthere\"");
            });
        }
    }

    public enum TestEnum{
        Zero,
        One,
        Two,
        ThirdOption,
        Fourth_Option
    }
}