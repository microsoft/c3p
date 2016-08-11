// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace Microsoft.C3P.Test
{
    public sealed class TestEvents
    {
        static int _staticEventCounter;
        int _instanceEventCounter;

        public TestEvents()
        {
        }

        public static event EventHandler<TestEvent> StaticEvent;

        public static void RaiseStaticEvent()
        {
            TestEvent e = new TestEvent(++_staticEventCounter);
            EventHandler<TestEvent> handler = TestEvents.StaticEvent;
            if (handler != null)
            {
                handler(null, e);
            }
        }

        public event EventHandler<TestEvent> InstanceEvent;

        public void RaiseInstanceEvent()
        {
            TestEvent e = new TestEvent(++_instanceEventCounter);
            EventHandler<TestEvent> handler = this.InstanceEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
