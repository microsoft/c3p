// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace Microsoft.C3P.Test
{
    public sealed class TestEvent
    {
        internal TestEvent(int counter)
        {
            this.Counter = counter;
        }

        public int Counter { get; private set; }
    }
}
