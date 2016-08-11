// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p;

import android.app.Activity;
import android.app.Application;
import android.net.Uri;
import android.util.Log;

import com.microsoft.c3p.js.JSValue;
import com.microsoft.c3p.js.JavaScriptType;
import com.microsoft.c3p.js.JavaScriptValue;

import java.lang.reflect.Array;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.UUID;

/**
 * Marshals parameters from and return values to the JavaScript bridge.
 */
final class JavaScriptMarshaller {
    private static final String TAG = "JavaScriptBridge";
    private static final int INVALID_HANDLE_VALUE = -1;

    private JavaScriptApplicationContext context;
    private NamespaceMapper namespaceMapper;
    private HashMap<Class<?>, HashMap<Object, Integer>> objectsToHandles;
    private HashMap<Class<?>, HashMap<Integer, Object>> handlesToObjects;
    private int counter;
    private HashSet<String> marshalByValueClassNames;

    public JavaScriptMarshaller(
            JavaScriptApplicationContext context, NamespaceMapper namespaceMapper) {
        this.context = context;
        this.namespaceMapper = namespaceMapper;
        this.objectsToHandles = new HashMap<Class<?>, HashMap<Object, Integer>>();
        this.handlesToObjects = new HashMap<Class<?>, HashMap<Integer, Object>>();
        this.marshalByValueClassNames = new HashSet<String>();
    }

    public void registerMarshalByValueClass(String className) {
        this.marshalByValueClassNames.add(className);
    }

    public JavaScriptValue marshalToJavaScript(Object object) {
        if (object == null) {
            return JSValue.Null;
        }

        if (object.getClass().isArray()) {
            JSValue convertedArray = JSValue.createArrayValue();
            int length = Array.getLength(object);
            for (int i = 0; i < length; i++) {
                convertedArray.addArrayItem(this.marshalToJavaScript(Array.get(object, i)));
            }
            return convertedArray;
        } else if (List.class.isAssignableFrom(object.getClass())) {
            List listObject = (List)object;
            JSValue convertedArray = JSValue.createArrayValue();
            int length = listObject.size();
            for (int i = 0; i < length; i++) {
                convertedArray.addArrayItem(this.marshalToJavaScript(listObject.get(i)));
            }
            return convertedArray;
        }

        JavaScriptValue convertedValue = JSValue.fromObject(object);
        if (convertedValue != JSValue.Undefined) {
            return convertedValue;
        }

        String pluginTypeName;
        Class<?> objectClass = object.getClass();
        if (objectClass == Class.class) {
            pluginTypeName = this.namespaceMapper.getJavaScriptClassForJavaClass(
                    ((Class<?>) object).getName());
            return JSValue.fromString(pluginTypeName);
        }

        String classSimpleName = objectClass.getSimpleName();
        String classFullName = objectClass.getName();
        pluginTypeName = this.namespaceMapper.getJavaScriptClassForJavaClass(classFullName);

        if (this.marshalByValueClassNames.contains(classSimpleName)) {
            JSValue jsObject = JSValue.createObjectValue();
            jsObject.putObjectValue("type", pluginTypeName);
            this.marshalPropertiesToJavaScript(object, jsObject);
            return jsObject;
        } else if (NamespaceMapper.uuidClassPlaceholder.equals(pluginTypeName)) {
            JSValue jsObject = JSValue.createObjectValue();
            jsObject.putObjectValue("type", pluginTypeName);
            jsObject.putObjectValue("value", object.toString().toUpperCase());
            return jsObject;
        } else if (NamespaceMapper.uriClassPlaceholder.equals(pluginTypeName)) {
            JSValue jsObject = JSValue.createObjectValue();
            jsObject.putObjectValue("type", pluginTypeName);
            jsObject.putObjectValue("value", object.toString());
            return jsObject;
        } else if (NamespaceMapper.dateClassPlaceholder.equals(pluginTypeName)) {
            JSValue jsObject = JSValue.createObjectValue();
            jsObject.putObjectValue("type", pluginTypeName);
            jsObject.putObjectValue("value", ((Date) object).getTime());
            return jsObject;
        }

        HashMap<Object, Integer> classObjectsToHandles = this.objectsToHandles.get(object.getClass());
        if (classObjectsToHandles == null) {
            classObjectsToHandles = new HashMap<Object, Integer>();
            this.objectsToHandles.put(object.getClass(), classObjectsToHandles);
        }

        HashMap<Integer, Object> classHandlesToObjects = this.handlesToObjects.get(object.getClass());
        if (classHandlesToObjects == null) {
            classHandlesToObjects = new HashMap<Integer, Object>();
            this.handlesToObjects.put(object.getClass(), classHandlesToObjects);
        }

        Integer handle = classObjectsToHandles.get(object);
        if (handle == null) {
            handle = ++counter;
            classObjectsToHandles.put(object, handle);
            classHandlesToObjects.put(handle, object);
        }

        JSValue jsObject = JSValue.createObjectValue();
        jsObject.putObjectValue("type", pluginTypeName);
        jsObject.putObjectValue("handle", handle);
        return jsObject;
    }

    public Object marshalFromJavaScript(JavaScriptValue jsObject, Class<?> type) {
        if (jsObject == null || jsObject.getType() == JavaScriptType.Undefined) {
            throw new IllegalArgumentException("A proxy object is required.");
        } if (type == null) {
            throw new IllegalArgumentException("A proxy type is required.");
        }

        if (jsObject.getType() == JavaScriptType.Array && type.isArray()) {
            int length = jsObject.getArrayLength();
            Object localArray = Array.newInstance(type.getComponentType(), length);
            for (int i = 0; i < length; i++) {
                JavaScriptValue item = jsObject.getArrayItem(i);
                Class<?> itemClass = this.getJavaClassForJavaScriptObject(item);
                Array.set(localArray, i, this.marshalFromJavaScript(item, itemClass));
            }
            return localArray;
        } else if (jsObject.getType() == JavaScriptType.Array
                && List.class.isAssignableFrom(type)) {
            int length = jsObject.getArrayLength();
            ArrayList<Object> localArray = new ArrayList<Object>(length);
            for (int i = 0; i < length; i++) {
                JavaScriptValue item = jsObject.getArrayItem(i);
                Class<?> itemClass = this.getJavaClassForJavaScriptObject(item);
                localArray.add(this.marshalFromJavaScript(item, itemClass));
            }
            return localArray;
        } else if (jsObject.getType() != JavaScriptType.Object) {
            return this.convertFromJson(jsObject, type);
        }

        JavaScriptValue handleValue = jsObject.getObjectValue("handle");
        if (handleValue.getType() != JavaScriptType.Number) {
            if (type == UUID.class) {
                JavaScriptValue value = jsObject.getObjectValue("value");
                if (value.getType() == JavaScriptType.String) {
                    return UUID.fromString(value.getString());
                }
            } else if (type == Uri.class) {
                JavaScriptValue value = jsObject.getObjectValue("value");
                if (value.getType() == JavaScriptType.String) {
                    return Uri.parse(value.getString());
                }
            } else if (type == Date.class) {
                JavaScriptValue value = jsObject.getObjectValue("value");
                if (value.getType() == JavaScriptType.Number) {
                    return new Date(value.getLong());
                }
            }

            try {
                Object instance = type.getConstructor().newInstance();
                this.marshalPropertiesFromJavaScript(jsObject, instance);
                return instance;
            } catch (InstantiationException e) {
                Log.w(TAG, "Exception when instantiating object of type " + type.getName() +
                        " that was proxied by value.", e);
                return null;
            } catch (NoSuchMethodException e) {
                Log.w(TAG, "Missing method when converting object of type " + type.getName() +
                        " that was proxied by value.", e);
                return null;
            } catch (IllegalAccessException e) {
                Log.w(TAG, "Illegal access when converting object of type " + type.getName() +
                        " that was proxied by value.", e);
                return null;
            } catch (InvocationTargetException e) {
                Log.w(TAG, "Exception when converting object of type " + type.getName() +
                        " that was proxied by value.", e);
                return null;
            }
        }

        int handle = handleValue.getInteger();
        HashMap<Integer, Object> classHandlesToObjects = this.handlesToObjects.get(type);
        if (classHandlesToObjects != null) {
            Object object = classHandlesToObjects.get(handle);
            if (object != null) {
                return object;
            }
        }

        if (type == Application.class) {
            return this.context.getApplication();
        } else if (type == Activity.class) {
            return this.context.getCurrentActivity();
        }

        throw new IllegalArgumentException(
                "Proxied object with handle " + handle + " was not found.");
    }

    private Class<?> getJavaClassForJavaScriptObject(JavaScriptValue jsObject) {
        if (jsObject.getType() != JavaScriptType.Object) {
            return Object.class;
        }

        JavaScriptValue typeValue = jsObject.getObjectValue("type");
        String itemType = typeValue.getType() ==
                JavaScriptType.String ? typeValue.getString() : null;
        if (itemType == null) {
            throw new IllegalArgumentException("Missing type field on proxied object.");
        }

        String itemClassFullName =
                this.namespaceMapper.getJavaClassForJavaScriptClass(itemType);
        try {
            return Class.forName(itemClassFullName);
        } catch (ClassNotFoundException e) {
            throw new IllegalArgumentException("Type not found: " + itemType, e);
        }
    }

    public Object[] marshalFromJavaScript(JavaScriptValue jsValues, Class<?>[] types) {
        if (jsValues.getType() == JavaScriptType.Array && jsValues.getArrayLength() == types.length)
        {
            Object[] convertedValues = new Object[types.length];

            for (int i = 0; i < types.length; i++) {
                convertedValues[i] = this.marshalFromJavaScript(jsValues.getArrayItem(i), types[i]);
            }

            return convertedValues;
        }

        // A null return value indicates the values could not be converted to the specified types.
        return null;
    }

    private Object convertFromJson(JavaScriptValue jsValue, Class<?> type) {
        JavaScriptType valueType = jsValue.getType();

        if (valueType == JavaScriptType.Null) {
            if (!type.isPrimitive()) {
                return null;
            } else if (type.equals(boolean.class)) {
                return Boolean.FALSE;
            } else if (type.equals(byte.class)) {
                return Byte.valueOf((byte)0);
            } else if (type.equals(short.class)) {
                return Short.valueOf((short)0);
            } else if (type.equals(int.class)) {
                return Integer.valueOf(0);
            } else if (type.equals(long.class)) {
                return Long.valueOf(0);
            } else if (type.equals(float.class)) {
                return Float.valueOf(0);
            } else if (type.equals(double.class)) {
                return Double.valueOf(0);
            }
        } else if ((type == Boolean.class || type == boolean.class) &&
                valueType == JavaScriptType.Boolean) {
            return jsValue.getBoolean();
        } else if ((type == Byte.class || type == byte.class) &&
                valueType == JavaScriptType.Number) {
            return Byte.valueOf((byte) jsValue.getInteger());
        } else if ((type == Character.class || type == char.class) &&
                valueType == JavaScriptType.Number) {
            return Character.valueOf((char) jsValue.getInteger());
        } else if ((type == Double.class || type == double.class) &&
                valueType == JavaScriptType.Number) {
            return Double.valueOf(jsValue.getDouble());
        } else if ((type == Float.class || type == float.class) &&
                valueType == JavaScriptType.Number) {
            return Float.valueOf((float) jsValue.getDouble());
        } else if ((type == Integer.class || type == int.class) &&
                valueType == JavaScriptType.Number) {
            return Integer.valueOf(jsValue.getInteger());
        } else if ((type == Long.class || type == long.class) &&
                valueType == JavaScriptType.Number) {
            return Long.valueOf((long) jsValue.getDouble());
        } else if ((type == Short.class || type == short.class) &&
                valueType == JavaScriptType.Number) {
            return Short.valueOf((short) jsValue.getInteger());
        } else if (type == String.class && valueType == JavaScriptType.String) {
            return jsValue.getString();
        } else if ((type.isArray() || type == Object.class) &&
                valueType == JavaScriptType.Array) {
            Class<?> componentType = type.getComponentType();
            int length = jsValue.getArrayLength();
            Object array = Array.newInstance(componentType, length);
            for (int i = 0; i < length; i++) {
                Array.set(array, i, this.marshalFromJavaScript(
                        jsValue.getArrayItem(i), componentType));
            }
            return array;
        } else if (type == java.util.List.class &&
                valueType == JavaScriptType.Array) {
            Class<?> componentType = type.getComponentType();
            int length = jsValue.getArrayLength();
            ArrayList<Object> list = new ArrayList<Object>(length);
            for (int i = 0; i < length; i++) {
                list.add(this.marshalFromJavaScript(
                        jsValue.getArrayItem(i), Object.class));
            }
            return list;
        } else if (type == Object.class && valueType != JavaScriptType.Object) {
            return JSValue.toObject(jsValue);
        }

        throw new IllegalArgumentException(
                "Could not convert " + valueType + " to expected type " + type.getName());
    }

    public Object releaseMarshalledObject(JavaScriptValue jsObject, Class<?> type) {
        if (jsObject == null) {
            throw new IllegalArgumentException("Object to be relased cannot be null.");
        }
        if (type == null) {
            throw new IllegalArgumentException("A type is required.");
        }

        JavaScriptValue handleValue = jsObject.getObjectValue("handle");
        if (handleValue.getType() != JavaScriptType.Number) {
            return null;
        }

        int handle = handleValue.getInteger();
        HashMap<Integer, Object> classHandlesToObjects = this.handlesToObjects.get(type);
        if (classHandlesToObjects != null) {
            Object object = classHandlesToObjects.get(handle);
            if (object != null) {
                classHandlesToObjects.remove(handle);

                HashMap<Object, Integer> classObjectsToHandles = this.objectsToHandles.get(type);
                if (classObjectsToHandles != null) {
                    classObjectsToHandles.remove(object);
                }

                return object;
            }
        }

        return null;
    }

    private void marshalPropertiesFromJavaScript(JavaScriptValue from, Object to)
            throws NoSuchMethodException, InvocationTargetException, IllegalAccessException {
        for (String propertyName : from.getObjectKeys()) {
            if ("type".equals(propertyName) || "handle".equals(propertyName)) {
                continue;
            }

            String methodName = "set" + Character.toUpperCase(propertyName.charAt(0)) +
                    propertyName.substring(1);
            JavaScriptValue value = from.getObjectValue(propertyName);

            for (Method targetMethod: to.getClass().getMethods()) {
                if (Modifier.isPublic(targetMethod.getModifiers()) &&
                        targetMethod.getName().equals(methodName) &&
                        Modifier.isPublic(targetMethod.getModifiers()) &&
                        targetMethod.getParameterTypes().length == 1) {
                    Object convertedValue = this.marshalFromJavaScript(
                            value, targetMethod.getParameterTypes()[0]);
                    targetMethod.invoke(to, convertedValue);
                    break;
                }
            }
        }
    }

    private void marshalPropertiesToJavaScript(Object from, JSValue to) {
        for (Method method : from.getClass().getMethods()) {
            if (Modifier.isPublic(method.getModifiers())) {
                String propertyName;
                if (method.getName().startsWith("get")) {
                    if (method.getName().equals("getClass") ||
                            (method.getName().equals("getSource") &&
                            from.getClass().getName().endsWith("Event"))) {
                        // Omit any objects' Class property and events' Source property.
                        continue;
                    }

                    propertyName = method.getName().substring(3);
                } else if (method.getName().startsWith("is")) {
                    propertyName = method.getName().substring(2);
                } else {
                    continue;
                }

                Object propertyValue;
                try {
                    propertyValue = method.invoke(from, (Object[])null);
                } catch (IllegalAccessException e) {
                    propertyValue = null;
                } catch (InvocationTargetException e) {
                    propertyValue = null;
                }

                propertyName = Character.toLowerCase(propertyName.charAt(0)) +
                        propertyName.substring(1);
                to.putObjectValue(propertyName, this.marshalToJavaScript(propertyValue));
            }
        }
    }
}
