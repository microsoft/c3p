// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import java.util.EventObject;

public class TestEvent extends EventObject {
    private int _counter;

    TestEvent(Object source, int counter) {
        super(source);
        _counter = counter;
    }

    public int getCounter() {
        return _counter;
    }
}
