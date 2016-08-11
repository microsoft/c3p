// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p;

import android.app.Activity;
import android.content.Intent;
import android.text.TextUtils;
import android.util.Log;

import com.microsoft.c3p.js.JavaScriptType;
import com.microsoft.c3p.js.JavaScriptValue;
import com.microsoft.c3p.util.ChainablePromise;
import com.microsoft.c3p.util.Consumer;
import com.microsoft.c3p.util.Function;

import java.lang.reflect.*;
import java.util.ArrayList;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

/**
 * Bridge for JavaScript callers into Java code. Instantiates and invokes arbitrary
 * classes and members using reflection, and converts arguments and results from/to JSON.
 */
public final class JavaScriptBridge {
    public static class CallType {
        public static final String GET_STATIC_PROPERTY = "getStaticProperty";
        public static final String SET_STATIC_PROPERTY = "setStaticProperty";
        public static final String INVOKE_STATIC_METHOD = "invokeStaticMethod";
        public static final String ADD_STATIC_EVENT_LISTENER = "addStaticEventListener";
        public static final String REMOVE_STATIC_EVENT_LISTENER = "removeStaticEventListener";
        public static final String CREATE_INSTANCE = "createInstance";
        public static final String RELEASE_INSTANCE = "releaseInstance";
        public static final String GET_PROPERTY = "getProperty";
        public static final String SET_PROPERTY = "setProperty";
        public static final String INVOKE_METHOD = "invokeMethod";
        public static final String ADD_EVENT_LISTENER = "addEventListener";
        public static final String REMOVE_EVENT_LISTENER = "removeEventListener";

        private CallType() { }
    }

    private static final String TAG = "JavaScriptBridge";

    protected JavaScriptApplicationContext context;

    private NamespaceMapper namespaceMapper;
    private JavaScriptMarshaller marshaller;
    private final ExecutorService executor;
    private ArrayList<JavaScriptEventBridge> eventBridges;
    private Object activityResultHandler;

    public JavaScriptBridge(JavaScriptApplicationContext context) {
        if (context == null) {
            throw new IllegalArgumentException("A C3P context is required.");
        }

        this.context = context;
        this.namespaceMapper = new NamespaceMapper();
        this.marshaller = new JavaScriptMarshaller(context, this.namespaceMapper);
        this.executor = Executors.newCachedThreadPool();
        this.eventBridges = new ArrayList<JavaScriptEventBridge>();
    }

    public NamespaceMapper getNamespaceMapper() {
        return this.namespaceMapper;
    }

    /**
     * Java objects returned to JavaScript are marshalled by reference by default, unless
     * the class full name is registered here.
     */
    public void registerMarshalByValueClass(String javaScriptClassName) {
        this.marshaller.registerMarshalByValueClass(javaScriptClassName);
    }

    public JavaScriptValue getStaticProperty(String type, String property)
            throws InvocationTargetException {
        if (TextUtils.isEmpty(type)) {
            throw new IllegalArgumentException("A type is required.");
        }
        if (TextUtils.isEmpty(property)) {
            throw new IllegalArgumentException("A property is required.");
        }

        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> targetClass = Class.forName(classFullName);

            Method targetMethod = this.resolveMethod(targetClass, "get" + property, true, 0);
            if (targetMethod == null) {
                targetMethod = this.resolveMethod(targetClass, "is" + property, true, 0);
                if (targetMethod == null) {
                    throw new IllegalArgumentException(
                            "Property getter not found: " + type + "." + property);
                }
            }

            Object returnValue = targetMethod.invoke(null);
            return this.marshaller.marshalToJavaScript(returnValue);
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to property: " + type + "." + property, iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by get static property invocation target: " +
                            type + "." + property,
                    itex.getTargetException());
            throw itex;
        }
    }

    public void setStaticProperty(String type, String property, JavaScriptValue value)
            throws InvocationTargetException {
        if (TextUtils.isEmpty(type)) {
            throw new IllegalArgumentException("A type is required.");
        }
        if (TextUtils.isEmpty(property)) {
            throw new IllegalArgumentException("A property is required.");
        }

        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> targetClass = Class.forName(classFullName);

            Method targetMethod = this.resolveMethod(targetClass, "set" + property, true, 1);
            if (targetMethod == null) {
                throw new IllegalArgumentException(
                        "Property setter not found: " + type + "." + property);
            }

            Object convertedValue = this.marshaller.marshalFromJavaScript(
                    value, targetMethod.getParameterTypes()[0]);
            targetMethod.invoke(null, convertedValue);
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to property: " + type + "." + property ,iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by set static property invocation target: " +
                            type + "." + property,
                    itex.getTargetException());
            throw itex;
        }
    }

    public ChainablePromise<JavaScriptValue> invokeStaticMethod(
            String type, String method, JavaScriptValue arguments)
            throws InvocationTargetException {
        if (TextUtils.isEmpty(type)) {
            throw new IllegalArgumentException("A type is required.");
        }
        if (TextUtils.isEmpty(method)) {
            throw new IllegalArgumentException("A method is required.");
        }
        if (arguments == null) {
            throw new IllegalArgumentException("JSON arguments are required.");
        }
        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> targetClass = Class.forName(classFullName);

            String methodName = this.namespaceMapper.getJavaMemberForJavaScriptMember(method);
            Method targetMethod = this.resolveMethod(targetClass, methodName, true, arguments);
            if (targetMethod == null) {
                throw new IllegalArgumentException("Method not found or invalid argument count: " +
                        type + "." + method);
            }

            Object[] convertedArguments = this.marshaller.marshalFromJavaScript(
                    arguments, targetMethod.getParameterTypes());
            if (convertedArguments == null) {
                throw new IllegalArgumentException("Supplied arguments could not be converted " +
                        "to expected types for method " + type + "." + method);
            }

            Object returnValue = targetMethod.invoke(null, convertedArguments);
            return this.convertToFutureJson(returnValue);
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to method: " + type + "." + method, iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by static method invocation target: " + type + "." + method,
                    itex.getTargetException());
            throw itex;
        }
    }

    public void addStaticEventListener(
            String type, String event, Consumer<JavaScriptValue> eventListener)
            throws InvocationTargetException {
        if (TextUtils.isEmpty(type)) {
            throw new IllegalArgumentException("A type is required.");
        }
        if (TextUtils.isEmpty(event)) {
            throw new IllegalArgumentException("An event is required.");
        }
        if (eventListener == null) {
            throw new IllegalArgumentException("An event listener is required.");
        }

        String methodName = "add" + event + "Listener";
        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> sourceClass = Class.forName(classFullName);
            JavaScriptEventBridge eventBridge = new JavaScriptEventBridge(
                    sourceClass, event, eventListener, this.marshaller);
            eventBridge.addListener();

            synchronized (this.eventBridges) {
                this.eventBridges.add(eventBridge);
            }
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to method: " + type + "." + methodName, iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by static method invocation target: " +
                            type + "." + methodName,
                    itex.getTargetException());
            throw itex;
        }
    }

    public void removeStaticEventListener(
            String type, String event, Consumer<JavaScriptValue> eventListener)
            throws InvocationTargetException {
        if (TextUtils.isEmpty(type)) {
            throw new IllegalArgumentException("A type is required.");
        }
        if (TextUtils.isEmpty(event)) {
            throw new IllegalArgumentException("An event is required.");
        }
        if (eventListener == null) {
            throw new IllegalArgumentException("An event listener is required.");
        }

        String methodName = "remove" + event + "Listener";
        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> sourceClass = Class.forName(classFullName);

            JavaScriptEventBridge eventBridge = null;
            synchronized (this.eventBridges) {
                for (JavaScriptEventBridge p : this.eventBridges) {
                    if (p.getSource().equals(sourceClass) &&
                            p.getEventName().equals(event) &&
                            p.getListener() == eventListener) {
                        eventBridge = p;
                        break;
                    }
                }
            }

            if (eventBridge != null) {
                eventBridge.removeListener();
                synchronized (this.eventBridges) {
                    this.eventBridges.remove(eventBridge);
                }
            } else {
                Log.w(TAG, "Event listener not found to remove: " + classFullName + "." + event);
            }
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to method: " + type + "." + methodName, iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by static method invocation target: " +
                            type + "." + methodName,
                    itex.getTargetException());
            throw itex;
        }
    }

    public JavaScriptValue createInstance(String type, JavaScriptValue arguments)
            throws InvocationTargetException {
        if (TextUtils.isEmpty(type)) {
            throw new IllegalArgumentException("A type is required.");
        }
        if (arguments == null) {
            throw new IllegalArgumentException("JSON arguments are required.");
        }

        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> targetClass = Class.forName(classFullName);
            Constructor<?> constructor = this.resolveConstructor(targetClass, arguments);
            if (constructor == null) {
                throw new IllegalArgumentException("Constructor not found or invalid " +
                        "argument count: " + type);
            }

            Object[] convertedArguments = this.marshaller.marshalFromJavaScript(
                    arguments, constructor.getParameterTypes());
            if (convertedArguments == null) {
                throw new IllegalArgumentException("Supplied arguments could not be converted " +
                        "to expected types for constructor for type " + type);
            }

            Object newInstance = constructor.newInstance(convertedArguments);
            JavaScriptValue jsInstance = this.marshaller.marshalToJavaScript(newInstance);
            return jsInstance;
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to constructor for type: " + type, iaex);
        } catch (InstantiationException iex) {
            throw new IllegalArgumentException("Cannot instantiate type: " + type, iex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by constructor invocation target for type: " + type,
                    itex.getTargetException());
            throw itex;
        }
    }

    public void releaseInstance(JavaScriptValue instance) {
        if (instance == null) {
            throw new IllegalArgumentException("An instance is required.");
        }

        String type = this.getInstanceType(instance);

        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> targetClass = Class.forName(classFullName);
            Object releasedObject = this.marshaller.releaseMarshalledObject(instance, targetClass);
            if (releasedObject != null && releasedObject == this.activityResultHandler) {
                this.activityResultHandler = null;
            }
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        }
    }

    public JavaScriptValue getProperty(JavaScriptValue instance, String property)
            throws InvocationTargetException {
        if (instance == null) {
            throw new IllegalArgumentException("An instance is required.");
        }
        if (TextUtils.isEmpty(property)) {
            throw new IllegalArgumentException("A property is required.");
        }

        String type = this.getInstanceType(instance);

        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> targetClass = Class.forName(classFullName);
            Object targetInstance = this.marshaller.marshalFromJavaScript(instance, targetClass);

            Method targetMethod = this.resolveMethod(targetClass, "get" + property, false, 0);
            if (targetMethod == null) {
                targetMethod = this.resolveMethod(targetClass, "is" + property, false, 0);
                if (targetMethod == null) {
                    throw new IllegalArgumentException(
                            "Property getter not found: " + type + "." + property);
                }
            }

            Object returnValue = targetMethod.invoke(targetInstance);
            return this.marshaller.marshalToJavaScript(returnValue);
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to property: " + type + "." + property ,iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by get property invocation target: " +
                            type + "." + property,
                    itex.getTargetException());
            throw itex;
        }
    }

    public void setProperty(JavaScriptValue instance, String property, JavaScriptValue value)
            throws InvocationTargetException {
        if (instance == null) {
            throw new IllegalArgumentException("An instance is required.");
        }
        if (TextUtils.isEmpty(property)) {
            throw new IllegalArgumentException("A property is required.");
        }

        String type = this.getInstanceType(instance);

        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> targetClass = Class.forName(classFullName);
            Object targetInstance = this.marshaller.marshalFromJavaScript(instance, targetClass);

            Method targetMethod = this.resolveMethod(targetClass, "set" + property, false, 1);
            if (targetMethod == null) {
                throw new IllegalArgumentException(
                        "Property setter not found: " + type + "." + property);
            }

            Object convertedValue = this.marshaller.marshalFromJavaScript(
                    value, targetMethod.getParameterTypes()[0]);
            targetMethod.invoke(targetInstance, convertedValue);
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to property: " + type + "." + property ,iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by set static property invocation target: " +
                            type + "." + property,
                    itex.getTargetException());
        }
    }

    public ChainablePromise<JavaScriptValue> invokeMethod(
            JavaScriptValue instance, String method, JavaScriptValue arguments)
            throws InvocationTargetException {
        if (instance == null) {
            throw new IllegalArgumentException("An instance is required.");
        }
        if (TextUtils.isEmpty(method)) {
            throw new IllegalArgumentException("A method is required.");
        }

        String type = this.getInstanceType(instance);

        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> targetClass = Class.forName(classFullName);
            Object targetInstance = this.marshaller.marshalFromJavaScript(instance, targetClass);

            String methodName = this.namespaceMapper.getJavaMemberForJavaScriptMember(method);
            Method targetMethod = this.resolveMethod(targetClass, methodName, false, arguments);
            if (targetMethod == null) {
                throw new IllegalArgumentException("Method not found or invalid argument count: " +
                        type + "." + method);
            }

            Object[] convertedArguments = this.marshaller.marshalFromJavaScript(
                    arguments, targetMethod.getParameterTypes());
            if (convertedArguments == null) {
                throw new IllegalArgumentException("Supplied arguments could not be converted " +
                        "to expected types for method " + type + "." + method);
            }


            this.saveActivityResultHandler(targetInstance, convertedArguments);

            Object returnValue = targetMethod.invoke(targetInstance, convertedArguments);
            return this.convertToFutureJson(returnValue);
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to method: " + type + "." + method, iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by method invocation target: " + type + "." + method,
                    itex.getTargetException());
            throw itex;
        }
    }

    public void addEventListener(
            JavaScriptValue instance, String event, Consumer<JavaScriptValue> eventListener)
            throws InvocationTargetException {
        if (instance == null) {
            throw new IllegalArgumentException("An instance is required.");
        }
        if (TextUtils.isEmpty(event)) {
            throw new IllegalArgumentException("A method is required.");
        }
        if (eventListener == null) {
            throw new IllegalArgumentException("An event listener is required.");
        }

        String type = this.getInstanceType(instance);

        String methodName = "add" + event + "Listener";
        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> sourceClass = Class.forName(classFullName);
            Object sourceInstance = this.marshaller.marshalFromJavaScript(instance, sourceClass);
            JavaScriptEventBridge eventBridge = new JavaScriptEventBridge(
                    sourceInstance, event, eventListener, this.marshaller);
            eventBridge.addListener();

            synchronized (this.eventBridges) {
                this.eventBridges.add(eventBridge);
            }
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to method: " + type + "." + methodName, iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by static method invocation target: " +
                            type + "." + methodName,
                    itex.getTargetException());
            throw itex;
        }
    }

    public void removeEventListener(
            JavaScriptValue instance, String event, Consumer<JavaScriptValue> eventListener)
            throws InvocationTargetException {
        if (instance == null) {
            throw new IllegalArgumentException("An instance is required.");
        }
        if (TextUtils.isEmpty(event)) {
            throw new IllegalArgumentException("A method is required.");
        }
        if (eventListener == null) {
            throw new IllegalArgumentException("An event listener is required.");
        }

        String type = this.getInstanceType(instance);

        String methodName = "remove" + event + "Listener";
        try {
            String classFullName = this.namespaceMapper.getJavaClassForJavaScriptClass(type);
            Class<?> sourceClass = Class.forName(classFullName);
            Object sourceInstance = this.marshaller.marshalFromJavaScript(instance, sourceClass);

            JavaScriptEventBridge eventBridge = null;
            synchronized (this.eventBridges) {
                for (JavaScriptEventBridge p : this.eventBridges) {
                    if (p.getSource().equals(sourceInstance) &&
                            p.getEventName().equals(event) &&
                            p.getListener() == eventListener) {
                        eventBridge = p;
                        break;
                    }
                }
            }

            if (eventBridge != null) {
                eventBridge.removeListener();
                synchronized (this.eventBridges) {
                    this.eventBridges.remove(eventBridge);
                }
            } else {
                Log.w(TAG, "Event listener not found to remove: " + classFullName + "." + event);
            }
        } catch (ClassNotFoundException cnfex) {
            throw new IllegalArgumentException("Type not found: " + type, cnfex);
        } catch (IllegalAccessException iaex) {
            throw new IllegalArgumentException(
                    "Illegal access to method: " + type + "." + methodName, iaex);
        } catch (InvocationTargetException itex) {
            Log.e(TAG,
                    "Exception thrown by static method invocation target: " +
                            type + "." + methodName,
                    itex.getTargetException());
            throw itex;
        }
    }

    private String getInstanceType(JavaScriptValue instance) {
        JavaScriptValue typeValue = instance.getObjectValue("type");
        String type = typeValue.getType() == JavaScriptType.String ? typeValue.getString() : null;
        if (TextUtils.isEmpty(type)) {
            throw new IllegalArgumentException("The instance must have a type field.");
        }
        return type;
    }

    private Constructor<?> resolveConstructor(
            Class<?> constructorClass, JavaScriptValue arguments) {
        int argumentsCount = 0;
        if (arguments != null && arguments.getType() == JavaScriptType.Array) {
            argumentsCount = arguments.getArrayLength();
        }

        return this.resolveConstructor(constructorClass, argumentsCount);
    }

    private Constructor<?> resolveConstructor(Class<?> constructorClass, int argumentsCount) {
        for (Constructor<?> constructor: constructorClass.getConstructors()) {
            if (Modifier.isPublic(constructor.getModifiers()) &&
                    constructor.getParameterTypes().length == argumentsCount) {
                return constructor;
            }
        }

        return null;
    }

    private Method resolveMethod(
            Class<?> methodClass, String methodName, boolean isStatic, JavaScriptValue arguments) {
        int argumentsCount = 0;
        if (arguments != null && arguments.getType() == JavaScriptType.Array) {
            argumentsCount = arguments.getArrayLength();
        }

        return this.resolveMethod(methodClass, methodName, isStatic, argumentsCount);
    }

    private Method resolveMethod(
            Class<?> methodClass, String methodName, boolean isStatic, int argumentsCount) {
        for (Method method: methodClass.getMethods()) {
            int methodModifiers = method.getModifiers();
            if (Modifier.isPublic(methodModifiers) && method.getName().equals(methodName) &&
                    Modifier.isStatic(methodModifiers) == isStatic &&
                    method.getParameterTypes().length == argumentsCount) {
                return method;
            }
        }
        return null;
    }

    private ChainablePromise<JavaScriptValue> convertToFutureJson(Object value) {
        final JavaScriptBridge self = this;
        if (value instanceof ChainablePromise<?>) {
            return ((ChainablePromise<Object>)value).then(
                    new Function<Object, JavaScriptValue>() {
                        @Override
                        public JavaScriptValue apply(Object result) {
                            JavaScriptValue convertedValue =
                                self.marshaller.marshalToJavaScript(result);
                            return convertedValue;
                        }
                    });
        } else if (value instanceof Future<?>) {
            final ChainablePromise<JavaScriptValue> promise = new ChainablePromise<JavaScriptValue>();
            final Future<?> futureValue = (Future<?>) value;
            executor.submit(new Runnable() {
                @Override
                public void run() {
                    try {
                        Object result = futureValue.get();
                        JavaScriptValue convertedValue =
                                self.marshaller.marshalToJavaScript(result);
                        promise.resolve(convertedValue);
                    } catch (Exception ex) {
                        promise.reject(ex);
                    }
                }
            });
            return promise;
        }
        else {
            JavaScriptValue convertedValue =
                    this.marshaller.marshalToJavaScript(value);
            return new ChainablePromise<JavaScriptValue>(convertedValue);
        }
    }

    private void saveActivityResultHandler(Object handlerInstance, Object[] arguments) {
        Method onActivityResultMethod = null;
        if (arguments.length > 0 && arguments[0] != null && arguments[0] instanceof Activity) {
            onActivityResultMethod = this.resolveMethod(
                    handlerInstance.getClass(), "onActivityResult", false, 3);
        }

        if (onActivityResultMethod != null) {
            this.context.interceptActivityResults();
            this.activityResultHandler = handlerInstance;
        } else {
            this.activityResultHandler = null;
        }
    }

    public void onActivityPause() {
        // TODO: Notify listeners.
    }

    public void onActivityResume() {
        // TODO: Notify listeners.
    }

    /**
     * Handles an activity result that was intercepted by the application context.
     */
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        Object resultHandler = this.activityResultHandler;
        if (resultHandler != null) {
            Method onActivityResultMethod = this.resolveMethod(
                    resultHandler.getClass(), "onActivityResult", false, 3);
            if (onActivityResultMethod != null) {
                try {
                    onActivityResultMethod.invoke(
                            resultHandler,
                            Integer.valueOf(requestCode),
                            Integer.valueOf(resultCode),
                            data);
                } catch (IllegalAccessException iaex) {
                    Log.e(TAG, "Illegal access to " +
                            resultHandler.getClass().getSimpleName() +
                            ".onActivityResult.", iaex);
                } catch (InvocationTargetException itex) {
                    Log.e(TAG, "Exception thrown by " +
                                    resultHandler.getClass().getSimpleName() +
                                    ".onActivityResult invocation.",
                            itex.getTargetException());
                }
            }
        }
    }
}
