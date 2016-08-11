// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace Microsoft.C3P.Test
{
    // WinRT doesn't allow nested classes, but C3P can infer them based on underscore naming.

    public sealed class TestOuter
    {
        public TestOuter_InnerClass InnerClassProperty { get; set; }

        public TestOuter_InnerStruct InnerStructProperty { get; set; }

        public TestOuter_InnerEnum InnerEnumProperty { get; set; }
    }

    public sealed class TestOuter_InnerClass
    {
        public int Value { get; set; }
    }

    public sealed class TestOuter_InnerStruct
    {
        public int Value { get; set; }
    }

    public enum TestOuter_InnerEnum
    {
        Zero = 0,
        One,
        Two,
        Three,
    }
}
