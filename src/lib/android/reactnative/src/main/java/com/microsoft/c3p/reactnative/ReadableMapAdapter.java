// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.reactnative;

import com.facebook.react.bridge.ReadableMap;
import com.facebook.react.bridge.ReadableMapKeySetIterator;
import com.facebook.react.bridge.ReadableType;

import com.microsoft.c3p.js.JSAdapter;
import com.microsoft.c3p.js.JSValue;
import com.microsoft.c3p.js.JavaScriptType;
import com.microsoft.c3p.js.JavaScriptValue;

import java.util.Iterator;
import java.util.Map;

/**
 * Adapts React Native's ReadableMap to the JavaScriptValue interface used by C3P code.
 */
public final class ReadableMapAdapter extends JSAdapter {
    private ReadableMap _map;

    public ReadableMapAdapter(ReadableMap map) {
        if (map == null) {
            throw new IllegalArgumentException("A ReadableMap value is required.");
        }

        _map = map;
    }

    @Override
    public final JavaScriptType getType() {
        return JavaScriptType.Object;
    }

    @Override
    public final Iterable<String> getObjectKeys() {
        return new KeysIterable();
    }

    @Override
    public final JavaScriptValue getObjectValue(String key) {
        if (!_map.hasKey(key)) {
            return JSValue.Undefined;
        }

        ReadableType valueType = _map.getType(key);
        switch (valueType) {
            case Null: return JSValue.Null;
            case Boolean: return JSValue.fromBoolean(_map.getBoolean(key));
            case Number: return JSValue.fromDouble(_map.getDouble(key));
            case String: return JSValue.fromString(_map.getString(key));
            case Map: return new ReadableMapAdapter(_map.getMap(key));
            case Array: return new ReadableArrayAdapter(_map.getArray(key));
            default:
                throw new IllegalStateException("Invalid ReadableMap value type: " + valueType);
        }
    }

    @Override
    public final Iterable<Map.Entry<String, JavaScriptValue>> getObjectEntries() {
        return new EntriesIterable();
    }

    private class KeysIterable implements Iterable<String> {
        @Override
        public Iterator<String> iterator() {
            return new KeysIterator();
        }
    }

    private final class KeysIterator implements Iterator<String> {
        private ReadableMapKeySetIterator _keySetIterator;

        public KeysIterator() {
            _keySetIterator = _map.keySetIterator();
        }

        @Override
        public final boolean hasNext() {
            return _keySetIterator.hasNextKey();
        }

        @Override
        public final String next() {
            return _keySetIterator.nextKey();
        }

        @Override
        public final void remove() {
            throw new IllegalStateException("The JS object is immutable.");
        }
    }

    private final class EntriesIterable
            implements Iterable<Map.Entry<String, JavaScriptValue>> {
        @Override
        public final Iterator<Map.Entry<String, JavaScriptValue>> iterator() {
            return new JSAdapter.EntriesIterator(new KeysIterator());
        }
    }
}
