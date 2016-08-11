// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace Microsoft.C3P.Test
{
    public sealed class TestOneWayStruct
    {
        internal TestOneWayStruct(string initialValue)
        {
            this.Value = initialValue;
        }

        public string Value { get; private set; }
    }
}
