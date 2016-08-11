// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

import { Promise } from "es6-promise";
import { NativeType, NativeObject, NativeReference } from "./NativeObject";

/**
 * Enumerates all types of calls over the JS to native bridge.
 */
export enum BridgeCallType {
    getStaticProperty = 1,
    setStaticProperty,
    invokeStaticMethod,
    addStaticEventListener,
    removeStaticEventListener,
    createInstance,
    releaseInstance,
    getProperty,
    setProperty,
    invokeMethod,
    addEventListener,
    removeEventListener,
}

/**
 * Base class for a JS-to-native bridge that provides synchronous access to native APIs.
 */
export interface NativeBridge {
    /**
     * Registers a type as a known bridged type, enabling instances of the type to be automatically
     * constructed from handles or serialized values returned over the bridge.
     * @param type Full platform-independent type name.
     * @param constructor Constructor function for the type.
     */
    registerType(type: string, constructor: NativeType): void;

    /**
     * Gets the value of a static property on the native class.
     * @param type Full platform-independent type name.
     * @param property Name of the property to get.
     * @returns The value of the property.
     */
    getStaticProperty(type: string, property: string): any;

    /**
     * Sets the value of a static property on the native class.
     * @param type Full platform-independent type name.
     * @param property Name of the property to set.
     * @param value The value to set.
     */
    setStaticProperty(type: string, property: string, value: any): void;

    /**
     * Invokes a static method on the native class.
     * @param type Full platform-independent type name.
     * @param method Name of the method to invoke.
     * @param args Arguments to pass to the method.
     * @returns The method's return value, or undefined if the method's return type is void.
     * @throws Any exceptions thrown by the native method.
     */
    invokeStaticMethod(type: string, method: string, args: any[]): any;

    /**
     * Adds a listener to a static event on the native class.
     * @param type Full platform-independent type name.
     * @param event Name of the event.
     * @param listener callback method to be invoked by the event. The callback takes a single parameter
     * which is an object with event-specific properties.
     */
    addStaticEventListener(type: string, event: string, listener: (e: any) => void): void;

    /**
     * Removes a listener to a static event on the native class.
     * @param type Full platform-independent type name.
     * @param event Name of the event.
     * @param listener Callback method which was previously added as a listener to the same event.
     */
    removeStaticEventListener(type: string, event: string, listener: (e: any) => void): void;

    /**
     * Creates an instance of a native class.
     * @param type Full platform-independent type name.
     * @param args Arguments to pass to the constructor.
     * @returns A handle to the native instance. (To be saved in the handle property of the NativeReference.)
     * @throws Any exceptions thrown by the native constructor.
     */
    createInstance(type: string, args: any[]): number;

    /**
     * Releases an instance of a native class, allowing native resources to be freed. Note only instances
     * of types marshalled by reference need to be released. (Attempting to release an instance that was
     * marshalled by value is a no-op.)
     * @param type Full platform-independent type name.
     * @param handle Handle to the native instance to be released.
     */
    releaseInstance(type: string, handle: number): void;

    /**
     * Gets the value of a property on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param property Name of the property to get.
     * @returns The value of the property.
     */
    getProperty(instance: NativeReference, property: string): any;

    /**
     * Sets the value of a property on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param property Name of the property to set.
     * @param value The value to set.
     */
    setProperty(instance: NativeReference, property: string, value: any): void;

    /**
     * Invokes a method on the native instance.
     * @param instance An instance of a NativeObject subclass that is marshalled-by-value from/to native code,
     * or a NativeReference subclass that includes a handle to a corresponding native instance.
     * @param method Name of the method to invoke.
     * @param args Arguments to pass to the method.
     * @returns The method's return value, or undefined if the method's return type is void.
     * @throws Any exceptions thrown by the native method.
     */
    invokeMethod(instance: NativeObject, method: string, args: any[]): any;

    /**
     * Adds a listener to an event on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param event Name of the event.
     * @param listener callback method to be invoked by the event. The callback takes a single parameter
     * which is an object with event-specific properties.
     */
    addEventListener(instance: NativeReference, event: string, listener: (e: any) => void): void;

    /**
     * Removes a listener to an event on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param event Name of the event.
     * @param listener Callback method which was previously added as a listener to the same event.
     */
    removeEventListener(instance: NativeReference, event: string, listener: (e: any) => void): void;
}

/**
 * Base class for a JS-to-native bridge that provides asynchronous access to native APIs.
 */
export interface NativeAsyncBridge {
    /**
     * Registers a type as a known bridged type, enabling instances of the type to be automatically
     * constructed from handles or serialized values returned over the bridge.
     * @param type Full platform-independent type name (including namespace).
     * @param constructor Constructor function for the type.
     */
    registerType(type: string, constructor: NativeType): void;

    /**
     * Gets the value of a static property on the native class.
     * @param type Full platform-independent type name.
     * @param property Name of the property to get.
     * @returns A promise for the value of the property.
     */
    getStaticProperty(type: string, property: string): Promise<any>;

    /**
     * Sets the value of a static property on the native class.
     * @param type Full platform-independent type name.
     * @param property Name of the property to set.
     * @param value The value to set.
     * @returns A promise for completion of setting the property.
     */
    setStaticProperty(type: string, property: string, value: any): Promise<void>;

    /**
     * Invokes a static method on the native class.
     * @param type Full platform-independent type name.
     * @param method Name of the method to invoke.
     * @param args Arguments to pass to the method.
     * @returns A promise for the method's return value, or a promise for the method completion if the method's
     * return type is void. The promise may fail with any exceptions thrown by the asynchronous native method.
     */
    invokeStaticMethod(type: string, method: string, args: any[]): Promise<any>;

    /**
     * Adds a listener to a static event on the native class.
     * @param type Full platform-independent type name.
     * @param event Name of the event.
     * @param listener callback method to be invoked by the event. The callback takes a single parameter
     * which is an object with event-specific properties.
     * @return A promise for completion of adding the listener.
     */
    addStaticEventListener(type: string, event: string, listener: (e: any) => void): Promise<void>;

    /**
     * Removes a listener to a static event on the native class.
     * @param type Full platform-independent type name.
     * @param event Name of the event.
     * @param listener Callback method which was previously added as a listener to the same event.
     * @return A promise for completion of removing the listner.
     */
    removeStaticEventListener(type: string, event: string, listener: (e: any) => void): Promise<void>;

    /**
     * Creates an instance of a native class.
     * @param type Full platform-independent type name.
     * @param args Arguments to pass to the constructor.
     * @returns A promise for a handle to the native instance. (To be saved in the handle property of the
     * NativeReference.) The promise may fail with any exceptions thrown by the asynchronous native constructor.
     */
    createInstance(type: string, args: any[]): Promise<number>;

    /**
     * Releases an instance of a native class, allowing native resources to be freed. Note only instances
     * of types marshalled by reference need to be released. (Attempting to release an instance that was
     * marshalled by value is a no-op.)
     * @param type Full platform-independent type name.
     * @param handle Handle to the native instance to be released.
     * @returns A promise for completion of releasing the native instance.
     */
    releaseInstance(type: string, handle: Promise<number>): Promise<void>;

    /**
     * Gets the value of a property on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param property Name of the property to get.
     * @returns A promise for the value of the property. The promise may fail with any exceptions thrown by
     * the asynchronous native constructor.
     */
    getProperty(instance: NativeReference, property: string): Promise<any>;

    /**
     * Sets the value of a property on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param property Name of the property to set.
     * @param value The value to set.
     * @returns A promise for completion of setting the property. The promise may fail with any exceptions thrown by
     * the asynchronous native constructor.
     */
    setProperty(instance: NativeReference, property: string, value: any): Promise<void>;

    /**
     * Invokes a method on the native instance.
     * @param instance An instance of a NativeObject subclass that is marshalled-by-value from/to native code,
     * or a NativeReference subclass that includes a handle to a corresponding native instance.
     * @param method Name of the method to invoke.
     * @param args Arguments to pass to the method.
     * @returns A promise for the method's return value, or a promise for the method completion if the method's
     * return type is void. The promise may fail with any exceptions thrown by the asynchronous native constructor
     * or native method.
     */
    invokeMethod(instance: NativeObject, method: string, args: any[]): Promise<any>;

    /**
     * Adds a listener to an event on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param event Name of the event.
     * @param listener callback method to be invoked by the event. The callback takes a single parameter
     * which is an object with event-specific properties.
     * @return A promise for completion of adding the listener.
     */
    addEventListener(instance: NativeReference, event: string, listener: (args: any) => void): Promise<void>;

    /**
     * Removes a listener to an event on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param event Name of the event.
     * @param listener Callback method which was previously added as a listener to the same event.
     * @return A promise for completion of removing the listener.
     */
    removeEventListener(instance: NativeReference, event: string, listener: (args: any) => void): Promise<void>;
}
