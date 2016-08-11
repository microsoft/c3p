// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.js;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.lang.reflect.Array;
import java.util.ArrayList;
import java.util.Collection;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

/**
 * Represents any kind of serializable JavaScript value (that is everything but functions).
 * Most instances of this class are immutable once constructed, with the exception of
 * instances returned by the createObjectValue() or createArrayValue() methods.
 */
public final class JSValue implements JavaScriptValue {
    public static final JSValue Undefined = new JSValue(JavaScriptType.Undefined, null, true);
    public static final JSValue Null = new JSValue(JavaScriptType.Null, null, true);
    public static final JSValue False = new JSValue(JavaScriptType.Boolean, Boolean.FALSE, true);
    public static final JSValue True = new JSValue(JavaScriptType.Boolean, Boolean.TRUE, true);
    public static final JSValue Zero = new JSValue(JavaScriptType.Number, new Double(0), true);
    public static final JSValue EmptyString = new JSValue(JavaScriptType.String, "", true);
    public static final JSValue EmptyObject =
            new JSValue(JavaScriptType.Object, new HashMap<String, JavaScriptValue>(), true);
    public static final JSValue EmptyArray =
            new JSValue(JavaScriptType.Array, new ArrayList<JavaScriptValue>(), true);

    private JavaScriptType _type;
    private Object _value;
    private boolean _isImmutable;

    private JSValue(JavaScriptType type, Object value, boolean isImmutable) {
        _type = type;
        _value = value;
        _isImmutable = isImmutable;
    }

    @Override
    public JavaScriptType getType() {
        return _type;
    }

    @Override
    public boolean getBoolean() {
        this.validateType(JavaScriptType.Boolean);
        return ((Boolean)_value).booleanValue();
    }

    @Override
    public int getInteger() {
        this.validateType(JavaScriptType.Number);
        return (int)((Double)_value).doubleValue();
    }

    @Override
    public long getLong() {
        this.validateType(JavaScriptType.Number);
        return (long)((Double)_value).doubleValue();
    }

    @Override
    public double getDouble() {
        this.validateType(JavaScriptType.Number);
        return ((Double)_value).doubleValue();
    }

    @Override
    public String getString() {
        if (_type == JavaScriptType.Null) {
            return null;
        } else {
            this.validateType(JavaScriptType.String);
            return (String)_value;
        }
    }

    @Override
    public Iterable<String> getObjectKeys() {
        this.validateType(JavaScriptType.Object);
        return ((Map<String, JavaScriptValue>)_value).keySet();
    }

    @Override
    public JavaScriptValue getObjectValue(String key) {
        this.validateType(JavaScriptType.Object);
        if (!((Map<String, JavaScriptValue>)_value).containsKey(key)) {
            return JSValue.Undefined;
        } else {
            return ((Map<String, JavaScriptValue>)_value).get(key);
        }
    }

    public void putObjectValue(String key, boolean value) {
        this.putObjectValue(key, value ? JSValue.True : JSValue.False);
    }

    public void putObjectValue(String key, String value) {
        this.putObjectValue(key, JSValue.fromString(value));
    }

    public void putObjectValue(String key, int value) {
        this.putObjectValue(key, JSValue.fromInteger(value));
    }

    public void putObjectValue(String key, long value) {
        this.putObjectValue(key, JSValue.fromLong(value));
    }

    public void putObjectValue(String key, JavaScriptValue value) {
        this.validateMutable();
        if (key == null) {
            throw new IllegalArgumentException("Keys may not be null.");
        }
        if (_type != JavaScriptType.Object) {
            _value = new HashMap<String, JavaScriptValue>();
            _type = JavaScriptType.Object;
        }
        ((Map<String, JavaScriptValue>)_value).put(key, value);
    }

    public void putObjectValues(Map<String, JavaScriptValue> values) {
        this.validateMutable();
        if (_type != JavaScriptType.Object) {
            _value = new HashMap<String, JavaScriptValue>();
            _type = JavaScriptType.Object;
        }
        for (Map.Entry<String, JavaScriptValue> entry : values.entrySet()) {
            if (entry.getKey() == null) {
                throw new IllegalArgumentException("Keys may not be null.");
            }
            ((Map<String, JavaScriptValue>)_value).put(entry.getKey(), entry.getValue());
        }
    }

    @Override
    public Iterable<Map.Entry<String, JavaScriptValue>> getObjectEntries() {
        this.validateType(JavaScriptType.Object);
        return ((Map<String, JavaScriptValue>)_value).entrySet();
    }

    @Override
    public int getArrayLength() {
        this.validateType(JavaScriptType.Array);
        return ((List<JavaScriptValue>)_value).size();
    }

    @Override
    public JavaScriptValue getArrayItem(int index) {
        this.validateType(JavaScriptType.Array);
        if (index >= ((List<JavaScriptValue>)_value).size()) {
            return JSValue.Undefined;
        } else {
            return ((List<JavaScriptValue>)_value).get(index);
        }
    }

    public void addArrayItem(JavaScriptValue value) {
        this.validateMutable();
        if (_type != JavaScriptType.Array) {
            _value = new ArrayList<JavaScriptValue>();
            _type = JavaScriptType.Array;
        }
        ((List<JavaScriptValue>)_value).add(value);
    }

    public void addArrayItems(JavaScriptValue[] items) {
        this.validateMutable();
        if (_type != JavaScriptType.Array) {
            _value = new ArrayList<JavaScriptValue>();
            _type = JavaScriptType.Array;
        }

        List<JavaScriptValue> listValue = (List<JavaScriptValue>)_value;
        for (int i = 0; i < items.length; i++) {
            listValue.add(items[i]);
        }
    }

    public void addArrayItems(Collection<JavaScriptValue> items) {
        this.validateMutable();
        if (_type != JavaScriptType.Array) {
            _value = new ArrayList<JavaScriptValue>();
            _type = JavaScriptType.Array;
        }
        ((List<JavaScriptValue>)_value).addAll(items);
    }

    public void setArrayItem(int index, JavaScriptValue value) {
        this.validateMutable();
        if (_type != JavaScriptType.Array) {
            _value = new ArrayList<JavaScriptValue>();
            _type = JavaScriptType.Array;
        }
        ((List<JavaScriptValue>)_value).set(index, value);
    }

    @Override
    public Iterable<JavaScriptValue> getArrayItems() {
        this.validateType(JavaScriptType.Array);
        return (Iterable<JavaScriptValue>)_value;
    }

    private void validateType(JavaScriptType requiredType) {
        if (_type != requiredType) {
            throw new IllegalArgumentException("Invalid JS value type. " +
                    "Current type: " + _type + "; required type: " + requiredType);
        }
    }

    private void validateMutable() {
        if (_isImmutable) {
            throw new IllegalStateException("The JS value is immutable.");
        }
    }

    public static JSValue createObjectValue() {
        return new JSValue(JavaScriptType.Object, new HashMap<String, JavaScriptValue>(), false);
    }

    public static JSValue createArrayValue() {
        return new JSValue(JavaScriptType.Array, new ArrayList<JavaScriptValue>(), false);
    }

    public static JavaScriptValue fromObject(Object value) {
        if (value == null || value == JSONObject.NULL) {
            return JSValue.Null;
        } else if (value instanceof Boolean) {
            return JSValue.fromBoolean(((Boolean) value).booleanValue());
        } else if (value instanceof Short) {
            return JSValue.fromInteger(((Short) value).intValue());
        } else if (value instanceof Integer) {
            return JSValue.fromInteger(((Integer) value).intValue());
        } else if (value instanceof Long) {
            return JSValue.fromLong(((Long) value).longValue());
        } else if (value instanceof Double) {
            return JSValue.fromDouble(((Double) value).doubleValue());
        } else if (value instanceof String) {
            return JSValue.fromString((String) value);
        } else if (value instanceof Map<?,?>) {
            return JSValue.fromMap((Map<String, JavaScriptValue>) value);
        } else if (value instanceof List<?>) {
            return JSValue.fromList((List<JavaScriptValue>) value);
        } else if (value.getClass().isArray() &&
                JavaScriptValue.class.isAssignableFrom(value.getClass().getComponentType())) {
            // Don't use JSValue.fromArray, because the array component type might be a subclass.
            int length = Array.getLength(value);
            if (length == 0) {
                return JSValue.EmptyArray;
            } else {
                JSValue jsValue = JSValue.createArrayValue();
                for (int i = 0; i < length; i++) {
                    jsValue.addArrayItem((JavaScriptValue) Array.get(value, i));
                }
                jsValue._isImmutable = true;
                return jsValue;
            }
        } else if (value instanceof JSONObject) {
            return new JSONObjectAdapter((JSONObject) value);
        } else if (value instanceof JSONArray) {
            return new JSONArrayAdapter((JSONArray) value);
        } else {
            return JSValue.Undefined;
        }
    }

    public static JavaScriptValue fromBoolean(boolean value) {
        return value ? JSValue.True : JSValue.False;
    }

    public static JavaScriptValue fromInteger(int value) {
        if (value == 0) {
            return JSValue.Zero;
        } else {
            return new JSValue(JavaScriptType.Number, Double.valueOf(value), true);
        }
    }

    public static JavaScriptValue fromLong(long value) {
        if (value == 0) {
            return JSValue.Zero;
        } else {
            return new JSValue(JavaScriptType.Number, Double.valueOf(value), true);
        }
    }

    public static JavaScriptValue fromDouble(double value) {
        if (value == 0) {
            return JSValue.Zero;
        } else {
            return new JSValue(JavaScriptType.Number, Double.valueOf(value), true);
        }
    }

    public static JavaScriptValue fromString(String value) {
        if (value == null) {
            return JSValue.Null;
        } else if (value.length() == 0) {
            return JSValue.EmptyString;
        } else {
            return new JSValue(JavaScriptType.String, value, true);
        }
    }

    public static JavaScriptValue fromMap(Map<String, JavaScriptValue> value) {
        if (value == null) {
            return JSValue.Null;
        } else if (value.size() == 0) {
            return JSValue.EmptyObject;
        } else {
            JSValue jsValue = JSValue.createObjectValue();
            jsValue.putObjectValues(value);
            jsValue._isImmutable = true;
            return jsValue;
        }
    }

    public static JavaScriptValue fromList(List<JavaScriptValue> value) {
        if (value == null) {
            return JSValue.Null;
        } else if (value.size() == 0) {
            return JSValue.EmptyArray;
        } else {
            JSValue jsValue = JSValue.createArrayValue();
            jsValue.addArrayItems(value);
            jsValue._isImmutable = true;
            return jsValue;
        }
    }

    public static JavaScriptValue fromArray(JavaScriptValue[] value) {
        if (value == null) {
            return JSValue.Null;
        } else if (value.length == 0) {
            return JSValue.EmptyArray;
        } else {
            JSValue jsValue = JSValue.createArrayValue();
            jsValue.addArrayItems(value);
            jsValue._isImmutable = true;
            return jsValue;
        }
    }

    public static Object toObject(JavaScriptValue value) {
        if (value == null ){
            throw new IllegalArgumentException("Value cannot be null.");
        }

        switch (value.getType()) {
            case Undefined:
            case Null:
                return null;
            case Boolean:
                return Boolean.valueOf(value.getBoolean());
            case Number:
                return value.getInteger() == value.getDouble() ?
                        Integer.valueOf(value.getInteger()) : Double.valueOf(value.getDouble());
            case String:
                return value.getString();
            case Array:
                JSONArray array = new JSONArray();
                int length = value.getArrayLength();
                for (int i = 0; i < length; i++) {
                    array.put(JSValue.toObject(value.getArrayItem(i)));
                }
                return array;
            case Object:
                JSONObject object = new JSONObject();
                for (Map.Entry<String, JavaScriptValue> entry : value.getObjectEntries()) {
                    try {
                        object.put(entry.getKey(), JSValue.toObject(entry.getValue()));
                    } catch (JSONException e) {
                        // Duplicate keys should never occur but can be ignored anyway.
                    }
                }
                return object;
            default:
                throw new IllegalArgumentException("Invalid JS value type: " + value.getType());
        }
    }

    private static final class JSONObjectAdapter extends JSAdapter {
        private JSONObject _json;

        public JSONObjectAdapter(JSONObject json) {
            _json = json;
        }

        @Override
        public JavaScriptType getType() {
            return JavaScriptType.Object;
        }

        @Override
        public Iterable<String> getObjectKeys() {
            return new KeysIterable();
        }

        @Override
        public JavaScriptValue getObjectValue(String key) {
            if (!_json.has(key)) {
                return JSValue.Undefined;
            } else {
                return JSValue.fromObject(_json.opt(key));
            }
        }

        @Override
        public Iterable<Map.Entry<String, JavaScriptValue>> getObjectEntries() {
            return new EntriesIterable();
        }

        private class KeysIterable implements Iterable<String> {
            @Override
            public Iterator<String> iterator() {
                return _json.keys();
            }
        }

        private class EntriesIterable
                implements Iterable<Map.Entry<String, JavaScriptValue>> {
            @Override
            public Iterator<Map.Entry<String, JavaScriptValue>> iterator() {
                return new JSAdapter.EntriesIterator(_json.keys());
            }
        }
    }

    private static final class JSONArrayAdapter extends JSAdapter {
        private JSONArray _json;

        public JSONArrayAdapter(JSONArray json) {
            _json = json;
        }

        @Override
        public JavaScriptType getType() {
            return JavaScriptType.Array;
        }

        @Override
        public int getArrayLength() {
            return _json.length();
        }

        @Override
        public JavaScriptValue getArrayItem(int index) {
            if (index >= _json.length()) {
                return JSValue.Undefined;
            } else {
                return JSValue.fromObject(_json.opt(index));
            }
        }

        @Override
        public Iterable<JavaScriptValue> getArrayItems() {
            return new JSAdapter.ItemsIterable();
        }
    }
}
