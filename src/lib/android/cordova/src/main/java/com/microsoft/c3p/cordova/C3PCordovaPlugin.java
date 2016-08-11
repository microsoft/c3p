// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.cordova;

import android.app.Activity;
import android.app.Application;
import android.content.Context;
import android.content.Intent;
import android.text.TextUtils;
import android.util.Log;

import com.microsoft.c3p.JavaScriptApplicationContext;
import com.microsoft.c3p.JavaScriptBridge;
import com.microsoft.c3p.js.JSValue;
import com.microsoft.c3p.js.JavaScriptValue;
import com.microsoft.c3p.util.ChainablePromise;
import com.microsoft.c3p.util.Consumer;
import com.microsoft.c3p.util.Function;

import org.apache.cordova.CallbackContext;
import org.apache.cordova.CordovaArgs;
import org.apache.cordova.CordovaPlugin;
import org.apache.cordova.PluginResult;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;
import org.xmlpull.v1.XmlPullParser;
import org.xmlpull.v1.XmlPullParserException;

import java.io.IOException;
import java.lang.reflect.InvocationTargetException;
import java.util.concurrent.ConcurrentHashMap;

/**
 * A Cordova plugin that enables other Cordova plugins to easily bridge between
 * JavaScript and Java code.
 */
public final class C3PCordovaPlugin extends CordovaPlugin {
    private static final String TAG = "C3PCordovaPlugin";

    private JavaScriptBridge bridge;
    private ConcurrentHashMap<String, Consumer<JavaScriptValue>> eventListenerMap;

    private class ApplicationContext  implements JavaScriptApplicationContext {
        @Override
        public Application getApplication() {
            return C3PCordovaPlugin.this.cordova.getActivity().getApplication();
        }

        @Override
        public Activity getCurrentActivity() {
            return C3PCordovaPlugin.this.cordova.getActivity();
        }

        @Override
        public void interceptActivityResults() {
            C3PCordovaPlugin.this.cordova.setActivityResultCallback(C3PCordovaPlugin.this);
        }
    }

    @Override
    protected void pluginInitialize() {
        this.bridge = new JavaScriptBridge(new C3PCordovaPlugin.ApplicationContext());
        this.loadNamespaceMappingsFromConfig(this.cordova.getActivity());
        this.eventListenerMap = new ConcurrentHashMap<String, Consumer<JavaScriptValue>>();
    }

    @Override
    public boolean execute(String action, CordovaArgs args, final CallbackContext callbackContext)
            throws JSONException {
        try {
            if (JavaScriptBridge.CallType.GET_STATIC_PROPERTY.equals(action)) {
                String type = args.getString(0);
                String property = args.getString(1);
                JavaScriptValue value = this.bridge.getStaticProperty(type, property);
                C3PCordovaPlugin.returnResult(value, callbackContext);
            } else if (JavaScriptBridge.CallType.SET_STATIC_PROPERTY.equals(action)) {
                String type = args.getString(0);
                String property = args.getString(1);
                Object value = args.opt(2);
                this.bridge.setStaticProperty(type, property, JSValue.fromObject(value));
                callbackContext.success();
            } else if (JavaScriptBridge.CallType.INVOKE_STATIC_METHOD.equals(action)) {
                String type = args.getString(0);
                String method = args.getString(1);
                JSONArray arguments = args.getJSONArray(2);
                ChainablePromise<JavaScriptValue> returnValue = this.bridge.invokeStaticMethod(
                        type, method, JSValue.fromObject(arguments));
                C3PCordovaPlugin.returnFutureResult(returnValue, callbackContext, false);
            } else if (JavaScriptBridge.CallType.ADD_STATIC_EVENT_LISTENER.equals(action)) {
                String type = args.getString(0);
                String event = args.getString(1);
                Consumer<JavaScriptValue> eventListener = new Consumer<JavaScriptValue>() {
                    @Override
                    public void accept(JavaScriptValue eventObject) {
                        C3PCordovaPlugin.returnResult(eventObject, callbackContext, true);
                    }
                };
                this.bridge.addStaticEventListener(type, event, eventListener);
                this.eventListenerMap.put(callbackContext.getCallbackId(), eventListener);
                C3PCordovaPlugin.returnResult(
                        JSValue.fromString(callbackContext.getCallbackId()), callbackContext, true);
            } else if (JavaScriptBridge.CallType.REMOVE_STATIC_EVENT_LISTENER.equals(action)) {
                String type = args.getString(0);
                String event = args.getString(1);
                String registrationToken = args.getString(2);
                Consumer<JavaScriptValue> eventListener = this.eventListenerMap.get(registrationToken);
                if (eventListener != null) {
                    this.bridge.removeStaticEventListener(type, event, eventListener);
                    this.eventListenerMap.remove(registrationToken);
                } else {
                    Log.w(TAG, "Event registration not found for callbackId: " + registrationToken);
                }
                callbackContext.success();
            } else if (JavaScriptBridge.CallType.CREATE_INSTANCE.equals(action)) {
                String type = args.getString(0);
                JSONArray arguments = args.getJSONArray(1);
                JavaScriptValue instance = this.bridge.createInstance(type, JSValue.fromObject(arguments));
                callbackContext.success((JSONObject) JSValue.toObject(instance));
            } else if (JavaScriptBridge.CallType.RELEASE_INSTANCE.equals(action)) {
                JSONObject instance = args.getJSONObject(0);
                this.bridge.releaseInstance(JSValue.fromObject(instance));
                callbackContext.success();
            } else if (JavaScriptBridge.CallType.GET_PROPERTY.equals(action)) {
                JSONObject instance = args.getJSONObject(0);
                String property = args.getString(1);
                JavaScriptValue value = this.bridge.getProperty(JSValue.fromObject(instance), property);
                C3PCordovaPlugin.returnResult(value, callbackContext);
            } else if (JavaScriptBridge.CallType.SET_PROPERTY.equals(action)) {
                JSONObject instance = args.getJSONObject(0);
                String property = args.getString(1);
                Object value = args.opt(2);
                this.bridge.setProperty(JSValue.fromObject(instance), property, JSValue.fromObject(value));
                callbackContext.success();
            } else if (JavaScriptBridge.CallType.INVOKE_METHOD.equals(action)) {
                JSONObject instance = args.getJSONObject(0);
                String method = args.getString(1);
                JSONArray arguments = args.getJSONArray(2);
                ChainablePromise<JavaScriptValue> returnValue = this.bridge.invokeMethod(
                        JSValue.fromObject(instance), method, JSValue.fromObject(arguments));
                C3PCordovaPlugin.returnFutureResult(returnValue, callbackContext, false);
            } else if (JavaScriptBridge.CallType.ADD_EVENT_LISTENER.equals(action)) {
                JSONObject instance = args.getJSONObject(0);
                String event = args.getString(1);
                Consumer<JavaScriptValue> eventListener = new Consumer<JavaScriptValue>() {
                    @Override
                    public void accept(JavaScriptValue eventObject) {
                        C3PCordovaPlugin.returnResult(eventObject, callbackContext, true);
                    }
                };
                this.bridge.addEventListener(JSValue.fromObject(instance), event, eventListener);
                this.eventListenerMap.put(callbackContext.getCallbackId(), eventListener);
                C3PCordovaPlugin.returnResult(
                        JSValue.fromString(callbackContext.getCallbackId()), callbackContext, true);
            } else if (JavaScriptBridge.CallType.REMOVE_EVENT_LISTENER.equals(action)) {
                JSONObject instance = args.getJSONObject(0);
                String event = args.getString(1);
                String registrationToken = args.getString(2);
                Consumer<JavaScriptValue> eventListener = this.eventListenerMap.get(registrationToken);
                if (eventListener != null) {
                    this.bridge.removeEventListener(JSValue.fromObject(instance), event, eventListener);
                    this.eventListenerMap.remove(registrationToken);
                } else {
                    Log.w(TAG, "Event registration not found for callbackId: " + registrationToken);
                }
                callbackContext.success();
            } else {
                throw new IllegalArgumentException("Invalid action: " + action);
            }
        } catch (IllegalArgumentException iaex) {
            throw new RuntimeException(iaex);
        } catch (InvocationTargetException itex) {
            throw new RuntimeException(itex.getTargetException());
        }
        return true;
    }

    private static void returnResult(JavaScriptValue result, CallbackContext callbackContext) {
        C3PCordovaPlugin.returnResult(result, callbackContext, false);
    }
    private static void returnResult(
            JavaScriptValue result, CallbackContext callbackContext, boolean keepCallback) {
        PluginResult pluginResult;
        switch (result.getType()) {
            case Null:
            case String:
                pluginResult = new PluginResult(PluginResult.Status.OK, result.getString());
                break;
            case Number:
                // TODO: Fix Cordova to avoid this loss of precision?
                pluginResult = new PluginResult(PluginResult.Status.OK, (float) result.getDouble());
                break;
            case Boolean:
                pluginResult = new PluginResult(PluginResult.Status.OK, result.getBoolean());
                break;
            case Object:
                pluginResult = new PluginResult(PluginResult.Status.OK, (JSONObject) JSValue.toObject(result));
                break;
            case Array:
                pluginResult = new PluginResult(PluginResult.Status.OK, (JSONArray) JSValue.toObject(result));
                break;
            default:
            throw new RuntimeException("Result object was not of any expected type.");
        }

        if (keepCallback) {
            pluginResult.setKeepCallback(true);
        }

        callbackContext.sendPluginResult(pluginResult);
    }

    private static void returnFutureResult(
            ChainablePromise<JavaScriptValue> futureResult,
            final CallbackContext callbackContext,
            final boolean keepCallback) {
        futureResult.then(
            new Function<JavaScriptValue, Void>() {
                @Override
                public Void apply(JavaScriptValue result) {
                    C3PCordovaPlugin.returnResult(result, callbackContext, keepCallback);
                    return null;
                }
            },
            new Consumer<Exception>() {
                @Override
                public void accept(Exception exception) {
                    callbackContext.error(exception.getMessage());
                }
            });
    }

    private void loadNamespaceMappingsFromConfig(Context context) {
        int id = context.getResources().getIdentifier(
                "config", "xml", context.getClass().getPackage().getName());
        if (id == 0) {
            id = context.getResources().getIdentifier("config", "xml", context.getPackageName());
            if (id == 0) {
                Log.e(TAG, "Namespace mappings could not be loaded because " +
                        "res/xml/config.xml is missing!");
                return;
            }
        }

        this.parseNamespaceMappingsFromConfig(context.getResources().getXml(id));
    }

    private void parseNamespaceMappingsFromConfig(XmlPullParser xml) {
        int eventType = -1;
        boolean insideC3PFeature = false;

        while (eventType != XmlPullParser.END_DOCUMENT) {
            if (eventType == XmlPullParser.START_TAG) {
                String nodeName = xml.getName();
                if ("feature".equals(nodeName) &&
                        "C3P".equals(xml.getAttributeValue(null, "name"))) {
                    insideC3PFeature = true;
                } else if (insideC3PFeature && "param".equals(nodeName)) {
                    String paramName = xml.getAttributeValue(null, "name");
                    if (paramName != null && paramName.startsWith("plugin-namespace:")) {
                        String javaPackage = paramName.substring("plugin-namespace:".length());
                        String pluginNamespace = xml.getAttributeValue(null, "value");
                        if (!TextUtils.isEmpty(javaPackage) &&
                                !TextUtils.isEmpty(pluginNamespace)) {
                            this.bridge.getNamespaceMapper().register(pluginNamespace, javaPackage);
                        }
                    } else if (paramName != null && paramName.startsWith("plugin-class:")) {
                        String className = paramName.substring("plugin-class:".length());
                        String classAttributes = xml.getAttributeValue(null, "value");
                        if (!TextUtils.isEmpty(className) &&
                                "marshal-by-value".equals(classAttributes)) {
                            this.bridge.registerMarshalByValueClass(className);
                        }
                    }
                }
            }
            else if (eventType == XmlPullParser.END_TAG)
            {
                String nodeName = xml.getName();
                if (nodeName.equals("feature") && insideC3PFeature) {
                    insideC3PFeature = false;
                }
            }
            try {
                eventType = xml.next();
            } catch (XmlPullParserException e) {
                e.printStackTrace();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
    }

    @Override
    public void onPause(boolean multitasking) {
        this.bridge.onActivityPause();
    }

    @Override
    public void onResume(boolean multitasking) {
        this.bridge.onActivityResume();
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        this.bridge.onActivityResult(requestCode, resultCode, data);
    }
}
