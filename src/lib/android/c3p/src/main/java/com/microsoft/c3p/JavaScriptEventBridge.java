// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p;

import com.microsoft.c3p.js.JavaScriptValue;
import com.microsoft.c3p.util.Consumer;

import java.lang.reflect.InvocationHandler;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.lang.reflect.Proxy;
import java.util.Comparator;
import java.util.EventObject;

/**
 * Bridges Java events to JavaScript event handlers.
 */
class JavaScriptEventBridge implements InvocationHandler {
    private Object source;
    private String eventName;
    private Consumer<JavaScriptValue> listener;
    private JavaScriptMarshaller marshaller;
    private Method addMethod;
    private Method removeMethod;
    private Object proxyInstance;

    public JavaScriptEventBridge(
            Object source,
            String eventName,
            Consumer<JavaScriptValue> listener,
            JavaScriptMarshaller marshaller) {
        this.source = source;
        this.eventName = eventName;
        this.listener = listener;
        this.marshaller = marshaller;

        this.addMethod = this.findEventMethod(source, "add" + eventName + "Listener");
        this.removeMethod = this.findEventMethod(source, "remove" + eventName + "Listener");

        Class<?> listenerInterface = this.addMethod.getParameterTypes()[0];
        Method[] listenerInterfaceMethods = listenerInterface.getMethods();
        if (listenerInterfaceMethods.length != 1) {
            throw new UnsupportedOperationException(
                    "Event listener interfaces with multiple methods are not supported: " +
                            listenerInterface.getName());
        }

        Class<?>[] parameterTypes = listenerInterfaceMethods[0].getParameterTypes();
        if (parameterTypes.length != 1 || !EventObject.class.isAssignableFrom(parameterTypes[0])) {
            throw new UnsupportedOperationException("Event listener interface method must " +
                    "have a single parameter that is a subclass of EventObject: " +
                    listenerInterface.getName() + "." + listenerInterfaceMethods[0].getName());
        }

        // Create a proxy object that implements the event-listener interface along with Comparable.
        // Any invocation of interface methods is routed to the invoke() method below.
        this.proxyInstance = Proxy.newProxyInstance(
                listenerInterface.getClassLoader(),
                new Class<?>[] { listenerInterface, Comparable.class },
                this);
    }

    public Object getSource() {
        return this.source;
    }

    public String getEventName() {
        return this.eventName;
    }

    public Consumer<JavaScriptValue> getListener() {
        return this.listener;
    }

    public void addListener() throws IllegalAccessException, InvocationTargetException {
        this.addMethod.invoke(
                this.source instanceof Class<?> ? null : this.source,
                new Object[] { this.proxyInstance });
    }

    public void removeListener() throws IllegalAccessException, InvocationTargetException {
        this.removeMethod.invoke(
                this.source instanceof Class<?> ? null : this.source,
                new Object[] { this.proxyInstance });
    }

    /**
     * InvocationHandler interface method - handles any method invocations on the proxy object.
     */
    @Override
    public Object invoke(Object proxy, Method method, Object[] args) throws Throwable {
        // The class the listener is added to might want to track listeners in a HashMap
        // or other data structure that requires hashCode, equals, and/or compareTo.
        if (method.getName().equals("hashCode")) {
            return this.listener.hashCode();
        } else if (method.getName().equals("equals")) {
            return proxy == args[0];
        } else if (method.getName().equals("compareTo")) {
            Object other = args[0];
            if (other == null) {
                return 1;
            } else if (proxy == other) {
                return 0;
            }
            // Compare objects' hash codes for a mostly-stable ordering.
            return proxy.hashCode() > other.hashCode() ? 1 : -1;
        } else if (args.length == 1) {
            // The event listener interface was already validated to contain a single method which
            // has a single parameter that is an EventObject. But the method name can be anything.
            EventObject eventObject = (EventObject) args[0];
            JavaScriptValue eventJSON = marshaller.marshalToJavaScript(eventObject);
            this.listener.accept(eventJSON);
        }
        return null;
    }

    /**
     * Looks for an add[EventName]Listener or remove[EventName]Listener method on an object.
     */
    private static Method findEventMethod(Object source, String methodName) {
        Class<?> sourceClass = (source instanceof Class<?> ? (Class<?>) source : source.getClass());

        for (Method method : sourceClass.getMethods()) {
            if (method.getName().equals(methodName) && Modifier.isPublic(method.getModifiers()) &&
                    method.getParameterTypes().length == 1) {
                return method;
            }
        }

        throw new IllegalArgumentException(
                "Event method not found: " + sourceClass.getName() + "." + methodName);
    }
}
