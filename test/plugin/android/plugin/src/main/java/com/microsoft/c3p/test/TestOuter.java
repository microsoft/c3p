// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

/**
 * Tests a class that contains inner types.
 */
public class TestOuter {
    private InnerClass _innerClass;
    private InnerStruct _innerStruct;
    private int _innerEnum;

    public InnerClass getInnerClassProperty() {
        return _innerClass;
    }

    public void setInnerClassProperty(InnerClass value) {
        _innerClass = value;
    }

    public InnerStruct getInnerStructProperty() {
        return _innerStruct;
    }

    public void setInnerStructProperty(InnerStruct value) {
        _innerStruct = value;
    }

    /**
     * Tests an inner class.
     */
    public static class InnerClass {
        private int _value;

        public int getValue() {
            return _value;
        }

        public void setValue(int value) {
            _value = value;
        }
    }

    /**
     * Tests an inner class that is proxied by value.
     */
    public static class InnerStruct {
        private int _value;

        public int getValue() {
            return _value;
        }

        public void setValue(int value) {
            _value = value;
        }
    }

    /**
     * Tests an inner enumeration.
     */
    public static class InnerEnum {
        public static final int INNER_ENUM_ZERO = 0;
        public static final int INNER_ENUM_ONE = 1;
        public static final int INNER_ENUM_TWO = 2;
        public static final int INNER_ENUM_THREE = 3;
    }

    public int getInnerEnumProperty() {
        return _innerEnum;
    }

    public void setInnerEnumProperty(int value) {
        _innerEnum = value;
    }
}
