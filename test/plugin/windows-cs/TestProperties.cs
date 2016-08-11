// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

namespace Microsoft.C3P.Test
{
    public sealed class TestProperties
    {
        IList<string> _readonlyList;

        public TestProperties()
        {
            _readonlyList = new List<string>(new[]
            {
                "One",
                "Two",
                "Three",
            }).AsReadOnly();
        }

        public static TestStruct StaticStructProperty { get; set; }

        public static IList<string> StaticListProperty { get; set; }

        public static double StaticDoubleProperty { get; set; }

        // Currently broken: does not get bound as a property on IOS.
        /*
        public static int StaticReadonlyIntProperty

            get
            {
                return 10;
            }
        }
        */

        public static TestEnum StaticEnumProperty { get; set; }

        public static bool StaticBoolProperty { get; set; }

        public TestStruct StructProperty { get; set; }

        public IList<string> ListProperty { get; set; }

        public IList<string> ReadonlyListProperty
        {
            get
            {
                return _readonlyList;
            }
        }

        public double DoubleProperty { get; set; }

        public int ReadonlyIntProperty
        {
            get
            {
                return 20;
            }
        }

        public TestEnum EnumProperty { get; set; }

        public bool BoolProperty { get; set; }

        public int? NullableIntProperty { get; set; }

        public double? NullableDoubleProperty { get; set; }

        public Guid? UuidProperty { get; set; }

        public Uri UriProperty { get; set; }

        public TestOneWayStruct OneWayStructProperty
        {
            get
            {
                return new TestOneWayStruct("test");
            }
        }
    }
}
