// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.js;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.Map;

/**
 * Abstract base class for specialized JavaScriptValue implementations, particularly adapters
 * that wrap a native type in JavaScript semantics.
 */
public abstract class JSAdapter implements JavaScriptValue {

    @Override
    public boolean getBoolean() {
        this.validateType(JavaScriptType.Boolean);
        return false;
    }

    @Override
    public int getInteger() {
        this.validateType(JavaScriptType.Number);
        return 0;
    }

    @Override
    public long getLong() {
        this.validateType(JavaScriptType.Number);
        return 0;
    }

    @Override
    public double getDouble() {
        this.validateType(JavaScriptType.Number);
        return 0.0;
    }

    @Override
    public String getString() {
        this.validateType(JavaScriptType.String);
        return "";
    }

    @Override
    public Iterable<String> getObjectKeys() {
        this.validateType(JavaScriptType.Object);
        return new ArrayList<String>();
    }

    @Override
    public JavaScriptValue getObjectValue(String key) {
        this.validateType(JavaScriptType.Object);
        return JSValue.Undefined;
    }

    @Override
    public Iterable<Map.Entry<String, JavaScriptValue>> getObjectEntries() {
        this.validateType(JavaScriptType.Object);
        return new ArrayList<Map.Entry<String, JavaScriptValue>>();
    }

    @Override
    public int getArrayLength() {
        this.validateType(JavaScriptType.Array);
        return 0;
    }

    @Override
    public JavaScriptValue getArrayItem(int index) {
        this.validateType(JavaScriptType.Array);
        return JSValue.Undefined;
    }

    @Override
    public Iterable<JavaScriptValue> getArrayItems() {
        this.validateType(JavaScriptType.Array);
        return new ArrayList<JavaScriptValue>();
    }

    protected final void validateType(JavaScriptType requiredType) {
        if (requiredType != this.getType()) {
            throw new IllegalArgumentException("Invalid JS value type. " +
                    "Current type: " + this.getType() + "; required type: " + requiredType);
        }
    }

    protected final class EntriesIterator
            implements Iterator<Map.Entry<String, JavaScriptValue>> {
        private Iterator<String> _keysIterator;

        public EntriesIterator(Iterator<String> keysIterator) {
            _keysIterator = keysIterator;
        }

        @Override
        public final boolean hasNext() {
            return _keysIterator.hasNext();
        }

        @Override
        public final Map.Entry<String, JavaScriptValue> next() {
            String key = _keysIterator.next();
            JavaScriptValue value = getObjectValue(key);
            return new Entry(key, value);
        }

        @Override
        public final void remove() {
            throw new IllegalStateException("The JS object is immutable.");
        }
    }

    private static final class Entry implements Map.Entry<String, JavaScriptValue> {
        private String _key;
        private JavaScriptValue _value;

        public Entry(String key, JavaScriptValue value) {
            _key = key;
            _value = value;
        }

        @Override
        public String getKey() {
            return _key;
        }

        @Override
        public JavaScriptValue getValue() {
            return _value;
        }

        @Override
        public JavaScriptValue setValue(JavaScriptValue object) {
            throw new IllegalStateException("The JS object is immutable.");
        }
    }

    protected final class ItemsIterable implements Iterable<JavaScriptValue> {
        public ItemsIterable() {
        }

        @Override
        public Iterator<JavaScriptValue> iterator() {
            return new JSAdapter.ItemsIterator();
        }
    }

    protected final class ItemsIterator implements Iterator<JavaScriptValue> {
        private int _index;

        @Override
        public boolean hasNext() {
            return _index < getArrayLength();
        }

        @Override
        public JavaScriptValue next() {
            ++_index;
            return getArrayItem(_index);
        }

        @Override
        public void remove() {
            throw new IllegalStateException("The JS object is immutable.");
        }
    }
}
