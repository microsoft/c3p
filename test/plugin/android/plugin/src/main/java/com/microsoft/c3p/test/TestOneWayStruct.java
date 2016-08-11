// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

/**
 * Tests a class that should be proxied by value only from native to JS.
 */
public class TestOneWayStruct {
    private String _value;

    /**
     * Creates an instance with a read-only value.
     */
    TestOneWayStruct(String value) {
        _value = value;
    }

    /**
     * Tests a read-only property getter on a one-way struct.
     */
    public String getValue() {
        return _value;
    }
}

