// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace Microsoft.C3P.Test
{
    public sealed class TestStruct
    {
        public TestStruct()
        {
        }

        public TestStruct(DateTimeOffset? initialValue)
        {
            this.Value = initialValue;
        }

        public DateTimeOffset? Value { get; set; }

        public void UpdateValue(DateTimeOffset? value)
        {
            this.Value = value;
        }

        public string ToXml()
        {
            return "<value>" + this.Value + "</value>";
        }
    }
}
