// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import java.util.Date;

/**
 * Tests a class that should be proxied by value instead of by reference.
 */
public class TestStruct {
    private Date _value;

    /**
     * Classes proxied by value must have a public parameter-less constructor.
     */
    public TestStruct() {
        this(null);
    }

    /**
     * Classes proxied by value may optionally have convenience constructors
     * with additional parameters, but they are not available in the JS interface.
     */
    public TestStruct(Date value) {
        _value = value;
    }

    /**
     * Tests a property getter on a struct.
     */
    public Date getValue() {
        return _value;
    }

    /**
     * Tests a property setter on a struct.
     */
    public void setValue(Date value) {
        _value = value;
    }

    /**
     * Tests calling a method on a struct that mutates the object state.
     */
    public void updateValue(Date newValue) {
        this._value = newValue;
    }

    /**
     * Tests calling a method on a struct that uses but does not mutate the object state.
     */
    public String toXml() {
        return "<value>" + _value + "</value>";
    }
}

