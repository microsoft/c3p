// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import java.util.HashSet;

public class TestEvents {
    private static HashSet<TestEventListener> _staticEventListeners =
            new HashSet<TestEventListener>();
    private static int _staticEventCounter;

    private HashSet<TestEventListener> _instanceEventListeners;
    private int _instanceEventCounter;

    public TestEvents() {
        _instanceEventListeners = new HashSet<TestEventListener>();
    }

    public static void addStaticEventListener(TestEventListener listener) {
        _staticEventListeners.add(listener);
    }

    public static void removeStaticEventListener(TestEventListener listener) {
        _staticEventListeners.remove(listener);
    }

    public static void raiseStaticEvent() {
        TestEvent e = new TestEvent(TestEvents.class, ++_staticEventCounter);
        for (TestEventListener listener : _staticEventListeners) {
            listener.testEventOccurred(e);
        }
    }

    public void addInstanceEventListener(TestEventListener listener) {
        _instanceEventListeners.add(listener);
    }

    public void removeInstanceEventListener(TestEventListener listener) {
        _instanceEventListeners.remove(listener);
    }

    public void raiseInstanceEvent() {
        TestEvent e = new TestEvent(this, ++_instanceEventCounter);
        for (TestEventListener listener : _instanceEventListeners) {
            listener.testEventOccurred(e);
        }
    }
}
