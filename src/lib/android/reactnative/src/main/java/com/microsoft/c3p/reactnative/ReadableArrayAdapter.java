// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.reactnative;

import com.facebook.react.bridge.ReadableArray;
import com.facebook.react.bridge.ReadableType;

import com.microsoft.c3p.js.JSAdapter;
import com.microsoft.c3p.js.JSValue;
import com.microsoft.c3p.js.JavaScriptType;
import com.microsoft.c3p.js.JavaScriptValue;

/**
 * Adapts React Native's ReadableArray to the JavaScriptValue interface used by C3P code.
 */
public final class ReadableArrayAdapter extends JSAdapter {
    private ReadableArray _array;

    public ReadableArrayAdapter(ReadableArray array) {
        if (array == null) {
            throw new IllegalArgumentException("A ReadableArray value is required.");
        }

        _array = array;
    }

    @Override
    public final JavaScriptType getType() {
        return JavaScriptType.Array;
    }

    @Override
    public final int getArrayLength() {
        return _array.size();
    }

    @Override
    public final JavaScriptValue getArrayItem(int index) {
        if (index >= _array.size()) {
            return JSValue.Undefined;
        }

        ReadableType valueType = _array.getType(index);
        switch (valueType) {
            case Null: return JSValue.Null;
            case Boolean: return JSValue.fromBoolean(_array.getBoolean(index));
            case Number: return JSValue.fromDouble(_array.getDouble(index));
            case String: return JSValue.fromString(_array.getString(index));
            case Map: return new ReadableMapAdapter(_array.getMap(index));
            case Array: return new ReadableArrayAdapter(_array.getArray(index));
            default:
                throw new IllegalStateException("Invalid ReadableArray value type: " + valueType);
        }
    }

    @Override
    public final Iterable<JavaScriptValue> getArrayItems() {
        return new JSAdapter.ItemsIterable();
    }
}
