// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.reactnative;

import android.app.Activity;
import android.app.Application;
import android.content.Intent;
import android.util.Log;

import com.facebook.react.bridge.ActivityEventListener;
import com.facebook.react.bridge.LifecycleEventListener;
import com.facebook.react.bridge.Promise;
import com.facebook.react.bridge.ReactContextBaseJavaModule;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.bridge.ReactMethod;
import com.facebook.react.bridge.ReadableArray;
import com.facebook.react.bridge.ReadableMap;
import com.facebook.react.bridge.WritableNativeArray;
import com.facebook.react.bridge.WritableNativeMap;
import com.facebook.react.modules.core.RCTNativeAppEventEmitter;

import com.microsoft.c3p.JavaScriptApplicationContext;
import com.microsoft.c3p.JavaScriptBridge;
import com.microsoft.c3p.js.JSValue;
import com.microsoft.c3p.js.JavaScriptValue;
import com.microsoft.c3p.util.ChainablePromise;
import com.microsoft.c3p.util.Consumer;
import com.microsoft.c3p.util.Function;

import java.lang.reflect.InvocationTargetException;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * A React Native module that enables other React Native modules to easily bridge between
 * JavaScript and Java code.
 */
public final class C3PReactModule extends ReactContextBaseJavaModule
        implements LifecycleEventListener, ActivityEventListener {
    private static final String TAG = "C3PReactModule";

    private JavaScriptBridge bridge;
    private ConcurrentHashMap<String, Consumer<JavaScriptValue>> eventListenerMap;
    private AtomicInteger nextEventRegistrationToken;
    private RCTNativeAppEventEmitter eventEmitter;

    public C3PReactModule(ReactApplicationContext reactContext) {
        super(reactContext);
        this.bridge = new JavaScriptBridge(new C3PReactModule.ApplicationContext());
        this.eventListenerMap = new ConcurrentHashMap<String, Consumer<JavaScriptValue>>();
        this.nextEventRegistrationToken = new AtomicInteger(1);
        reactContext.addLifecycleEventListener(this);
        reactContext.addActivityEventListener(this);
    }

    @Override
    public String getName() {
        return "C3P";
    }

    private class ApplicationContext  implements JavaScriptApplicationContext {
        @Override
        public Application getApplication() {
            return (Application)getReactApplicationContext().getApplicationContext();
        }

        @Override
        public Activity getCurrentActivity() {
            return C3PReactModule.this.getCurrentActivity();
        }

        @Override
        public void interceptActivityResults() {
            // The React module already intercepts activity results.
        }
    }

    @ReactMethod
    public void registerNamespaceMapping(String pluginNamespace, String javaPackage) {
        this.bridge.getNamespaceMapper().register(pluginNamespace, javaPackage);
    }

    @ReactMethod
    public void registerMarshalByValueClass(String className) {
        this.bridge.registerMarshalByValueClass(className);
    }

    @ReactMethod
    public void getStaticProperty(
            String type,
            String property,
            Promise promise) {
        try {
            JavaScriptValue value = bridge.getStaticProperty(type, property);
            C3PReactModule.resolvePromise(promise, value);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void setStaticProperty(
            String type,
            String property,
            ReadableArray valueContainer,
            Promise promise) {
        if (valueContainer.size() != 1) {
            throw new IllegalArgumentException("Value container must be an array of length 1.");
        }

        try {
            JavaScriptValue value = new ReadableArrayAdapter(valueContainer).getArrayItem(0);
            bridge.setStaticProperty(type, property, value);
            promise.resolve(null);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void invokeStaticMethod(
            String type,
            String method,
            ReadableArray arguments,
            Promise promise) {
        try {
            JavaScriptValue argumentsAdapter = new ReadableArrayAdapter(arguments);
            ChainablePromise<JavaScriptValue> promisedResult =
                    bridge.invokeStaticMethod(type, method, argumentsAdapter);
            C3PReactModule.resolvePromise(promise, promisedResult);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void addStaticEventListener(
            String type,
            final String event,
            Promise promise) {
        final String registrationToken =
                Integer.valueOf(this.nextEventRegistrationToken.getAndIncrement()).toString();
        final RCTNativeAppEventEmitter eventEmitter = this.getEventEmitter();
        Consumer<JavaScriptValue> eventListener = new Consumer<JavaScriptValue>() {
            @Override
            public void accept(JavaScriptValue eventObject) {
                WritableNativeMap eventMap = C3PReactModule.convertObjectResult(eventObject);
                eventEmitter.emit(event + ":" + registrationToken, eventMap);
            }
        };
        try {
            this.bridge.addStaticEventListener(type, event, eventListener);
            this.eventListenerMap.put(registrationToken, eventListener);
            promise.resolve(registrationToken);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void removeStaticEventListener(
            String type,
            String event,
            String registrationToken,
            Promise promise) {
        Consumer<JavaScriptValue> eventListener = this.eventListenerMap.get(registrationToken);
        if (eventListener != null) {
            try {
                this.bridge.removeStaticEventListener(type, event, eventListener);
                this.eventListenerMap.remove(registrationToken);
                promise.resolve(null);
            } catch (InvocationTargetException e) {
                promise.resolve(e.getTargetException());
            }
        } else {
            Log.w(TAG, "Event registration not found for token: " + registrationToken);
        }
    }

    @ReactMethod
    public void createInstance(
            String type,
            ReadableArray arguments,
            Promise promise) {
        try {
            JavaScriptValue argumentsAdapter = new ReadableArrayAdapter(arguments);
            JavaScriptValue result = bridge.createInstance(type, argumentsAdapter);
            C3PReactModule.resolvePromise(promise, result);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void releaseInstance(
            ReadableMap instance,
            Promise promise) {
        bridge.releaseInstance(new ReadableMapAdapter(instance));
        promise.resolve(null);
    }

    @ReactMethod
    public void getProperty(
            ReadableMap instance,
            String property,
            Promise promise) {
        try {
            JavaScriptValue instanceAdapter = new ReadableMapAdapter(instance);
            JavaScriptValue value = bridge.getProperty(instanceAdapter, property);
            C3PReactModule.resolvePromise(promise, value);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void setProperty(
            ReadableMap instance,
            String property,
            ReadableArray valueContainer,
            Promise promise) {
        if (valueContainer.size() != 1) {
            throw new IllegalArgumentException("Value container must be an array of length 1.");
        }

        try {
            JavaScriptValue instanceAdapter = new ReadableMapAdapter(instance);
            JavaScriptValue value = new ReadableArrayAdapter(valueContainer).getArrayItem(0);
            bridge.setProperty(instanceAdapter, property, value);
            promise.resolve(null);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void invokeMethod(
            ReadableMap instance,
            String method,
            ReadableArray arguments,
            Promise promise) {
        try {
            JavaScriptValue instanceAdapter = new ReadableMapAdapter(instance);
            JavaScriptValue argumentsAdapter = new ReadableArrayAdapter(arguments);
            ChainablePromise<JavaScriptValue> promisedResult =
                    bridge.invokeMethod(instanceAdapter, method, argumentsAdapter);
            C3PReactModule.resolvePromise(promise, promisedResult);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void addEventListener(
            ReadableMap instance,
            final String event,
            Promise promise) {
        final String registrationToken =
                Integer.valueOf(this.nextEventRegistrationToken.getAndIncrement()).toString();
        final RCTNativeAppEventEmitter eventEmitter = this.getEventEmitter();
        Consumer<JavaScriptValue> eventListener = new Consumer<JavaScriptValue>() {
            @Override
            public void accept(JavaScriptValue eventObject) {
                WritableNativeMap eventMap = C3PReactModule.convertObjectResult(eventObject);
                eventEmitter.emit(event + ":" + registrationToken, eventMap);
            }
        };
        try {
            JavaScriptValue instanceAdapter = new ReadableMapAdapter(instance);
            this.bridge.addEventListener(instanceAdapter, event, eventListener);
            this.eventListenerMap.put(registrationToken, eventListener);
            promise.resolve(registrationToken);
        } catch (InvocationTargetException e) {
            promise.reject(e.getTargetException());
        }
    }

    @ReactMethod
    public void removeEventListener(
            ReadableMap instance,
            String event,
            String registrationToken,
            Promise promise) {
        Consumer<JavaScriptValue> eventListener = this.eventListenerMap.get(registrationToken);
        if (eventListener != null) {
            try {
                JavaScriptValue instanceAdapter = new ReadableMapAdapter(instance);
                this.bridge.removeEventListener(instanceAdapter, event, eventListener);
                this.eventListenerMap.remove(registrationToken);
                promise.resolve(null);
            } catch (InvocationTargetException e) {
                promise.resolve(e.getTargetException());
            }
        } else {
            Log.w(TAG, "Event registration not found for token: " + registrationToken);
        }
    }

    private static void resolvePromise(Promise promise, JavaScriptValue result) {
        switch (result.getType()) {
            case Undefined:
            case Null:
            case Boolean:
            case Number:
            case String:
                promise.resolve(JSValue.toObject(result));
                break;
            case Object:
                promise.resolve(C3PReactModule.convertObjectResult(result));
                break;
            case Array:
                promise.resolve(C3PReactModule.convertArrayResult(result));
                break;
            default:
                promise.reject(
                        new IllegalStateException("Invalid JS value type: " + result.getType()));
        }
    }

    private static void resolvePromise(
            final Promise promise,
            ChainablePromise<JavaScriptValue> promisedResult) {
        promisedResult.then(
            new Function<JavaScriptValue, Void>() {
                @Override
                public Void apply(JavaScriptValue result) {
                    C3PReactModule.resolvePromise(promise, result);
                    return null;
                }
            },
            new Consumer<Exception>() {
                @Override
                public void accept(Exception exception) {
                    promise.reject(exception);
                }
            });
    }

    private static WritableNativeMap convertObjectResult(JavaScriptValue objectResult) {
        WritableNativeMap convertedResult = new WritableNativeMap();

        for (Map.Entry<String, JavaScriptValue> entry : objectResult.getObjectEntries()) {
            String key = entry.getKey();
            JavaScriptValue value = entry.getValue();
            switch (value.getType()) {
                case Null: convertedResult.putNull(key); break;
                case Boolean: convertedResult.putBoolean(key, value.getBoolean()); break;
                case Number: convertedResult.putDouble(key, value.getDouble()); break;
                case String: convertedResult.putString(key, value.getString()); break;
                case Object: convertedResult.putMap(
                        key, C3PReactModule.convertObjectResult(value)); break;
                case Array: convertedResult.putArray(
                        key, C3PReactModule.convertArrayResult(value)); break;
            }
        }

        return convertedResult;
    }

    private static WritableNativeArray convertArrayResult(JavaScriptValue arrayResult) {
        WritableNativeArray convertedResult = new WritableNativeArray();

        for (JavaScriptValue value : arrayResult.getArrayItems()) {
            switch (value.getType()) {
                case Null: convertedResult.pushNull(); break;
                case Boolean: convertedResult.pushBoolean(value.getBoolean()); break;
                case Number: convertedResult.pushDouble(value.getDouble()); break;
                case String: convertedResult.pushString(value.getString()); break;
                case Object: convertedResult.pushMap(
                        C3PReactModule.convertObjectResult(value)); break;
                case Array: convertedResult.pushArray(
                        C3PReactModule.convertArrayResult(value)); break;
            }
        }

        return convertedResult;
    }

    private RCTNativeAppEventEmitter getEventEmitter() {
        if (this.eventEmitter == null) {
            this.eventEmitter = this.getReactApplicationContext().getJSModule(
                    RCTNativeAppEventEmitter.class);
        }

        return this.eventEmitter;
    }

    @Override
    public void onHostPause() {
        this.bridge.onActivityPause();
    }

    @Override
    public void onHostResume() {
        this.bridge.onActivityResume();
    }

    @Override
    public void onHostDestroy() {
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        this.bridge.onActivityResult(requestCode, resultCode, data);
    }
}
