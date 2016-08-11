// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

import { Promise } from "es6-promise";
import { Cordova } from "cordova";
import { NativeType, NativeObject, NativeReference } from "../C3P/NativeObject";
import { BridgeCallType, NativeAsyncBridge } from "../C3P/NativeBridge";
import { Marshaller } from "../C3P/Marshaller";
import { LoggingNativeAsyncBridge } from "../C3P/LoggingNativeBridge";
import { EventListenersCollection, EventListenerRecord } from "../C3P/NativeEventListeners";

/**
 * Used here for access to the Cordova JS-Native bridge. The implementation will be provided by the Cordova runtime.
 */
declare var cordova: Cordova;

/**
 * Implementation of a native bridge in the Cordova JavaScript environment.
 * The Cordova JS to native bridge is always asynchronous.
 */
class CordovaNativeBridge implements NativeAsyncBridge {
    static serviceName: string = "C3P";

    /**
     * Collection of event listeners added to any bridged object, tracked so that they can be removed via tokens.
     */
    private eventListeners: EventListenersCollection = new EventListenersCollection();

    /**
     * Registers a type as a known bridged type, enabling instances of the type to be automatically
     * constructed from handles or serialized values returned over the bridge.
     * @param type Full platform-independent type name.
     * @param constructor Constructor function for the type.
     */
    registerType(type: string, constructor: NativeType): void {
        Marshaller.registerType(type, constructor);
    }

    /**
     * Gets the value of a static property on the native class.
     * @param type Full platform-independent type name.
     * @param property Name of the property to get.
     * @returns A promise for the value of the property.
     */
    getStaticProperty(type: string, property: string): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            cordova.exec(
                function () {
                    var result = arguments[0];
                    resolve(Marshaller.marshalFromNative(result));
                },
                reject,
                CordovaNativeBridge.serviceName,
                BridgeCallType[BridgeCallType.getStaticProperty],
                <any[]>[ type, property ]);
        });
    }

    /**
     * Sets the value of a static property on the native class.
     * @param type Full platform-independent type name.
     * @param property Name of the property to set.
     * @param value The value to set.
     * @returns A promise for completion of setting the property.
     */
    setStaticProperty(type: string, property: string, value: any): Promise<void> {
        return new Promise<any>((resolve, reject) => {
            cordova.exec(
                () => {
                    resolve();
                },
                reject,
                CordovaNativeBridge.serviceName,
                BridgeCallType[BridgeCallType.setStaticProperty],
                [ type, property, value ]);
        });
    }

    /**
     * Invokes a static method on the native class.
     * @param type Full platform-independent type name.
     * @param method Name of the method to invoke.
     * @param args Arguments to pass to the method.
     * @returns A promise for the method's return value, or a promise for the method completion if the method's
     * return type is void. The promise may fail with any exceptions thrown by the asynchronous native method.
     */
    invokeStaticMethod(type: string, method: string, args: any[]): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            Marshaller.marshalToNative(args).then(
                marshalledArgs => {
                    cordova.exec(
                        function () {
                            var result = arguments[0];
                            resolve(Marshaller.marshalFromNative(result));
                        },
                        reject,
                        CordovaNativeBridge.serviceName,
                        BridgeCallType[BridgeCallType.invokeStaticMethod],
                        <any[]>[ type, method, marshalledArgs ]);
                },
                reject);
        });
    }

    /**
     * Adds a listener to a static event on the native class.
     * @param type Full platform-independent type name.
     * @param event Name of the event.
     * @param listener callback method to be invoked by the event. The callback takes a single parameter
     * which is an object with event-specific properties.
     * @return A promise for completion of adding the listener.
     */
    addStaticEventListener(type: string, event: string, listener: (e: any) => void): Promise<void> {
        var listeners: EventListenersCollection = this.eventListeners;
        if (listeners.find(type, event, listener)) {
            return Promise.resolve(undefined);
        }
        var firstCallback: boolean = true;
        return new Promise<void>((resolve, reject) => {
            cordova.exec(
                function () {
                    if (firstCallback) {
                        firstCallback = false;
                        var token: string = arguments[0];
                        listeners.add(type, event, listener, token, null);
                        resolve();
                    } else {
                        listener(Marshaller.marshalFromNative(arguments[0]));
                    }
                },
                reject,
                CordovaNativeBridge.serviceName,
                BridgeCallType[BridgeCallType.addStaticEventListener],
                <any[]>[type, event]);
        });
    }

    /**
     * Removes a listener to a static event on the native class.
     * @param type Full platform-independent type name.
     * @param event Name of the event.
     * @param listener Callback method which was previously added as a listener to the same event.
     * @return A promise for completion of removing the listner.
     */
    removeStaticEventListener(type: string, event: string, listener: (e: any) => void): Promise<void> {
        var listeners: EventListenersCollection = this.eventListeners;
        var listenerRecord: (EventListenerRecord | null) = listeners.find(type, event, listener);
        if (!listenerRecord) {
            return Promise.resolve(undefined);
        }

        var registrationToken = listenerRecord.token;
        return new Promise<void>((resolve, reject) => {
            cordova.exec(
                function () {
                    listeners.remove(type, event, listener, registrationToken);
                    resolve();
                },
                reject,
                CordovaNativeBridge.serviceName,
                BridgeCallType[BridgeCallType.removeStaticEventListener],
                <any[]>[type, event, registrationToken]);
        });
    }

    /**
     * Creates an instance of a native class.
     * @param type Full platform-independent type name.
     * @param args Arguments to pass to the constructor.
     * @returns A promise for a handle to the native instance. (To be saved in the handle property of the
     * NativeReference.) The promise may fail with any exceptions thrown by the asynchronous native constructor.
     */
    createInstance(type: string, args: any[]): Promise<number> {
        return new Promise<number>((resolve, reject) => {
            Marshaller.marshalToNative(args).then(
                marshalledArgs => {
                    cordova.exec(
                        function () {
                            try {
                                var instanceInfo: any = arguments[0];
                                resolve(instanceInfo.handle);
                            } catch (error) {
                                reject(error);
                            }
                        },
                        reject,
                        CordovaNativeBridge.serviceName,
                        BridgeCallType[BridgeCallType.createInstance],
                        <any[]>[ type, marshalledArgs ]);
                },
                reject);
        });
    }

    /**
     * Releases an instance of a native class, allowing native resources to be freed. Note only instances
     * of types marshalled by reference need to be released. (Attempting to release an instance that was
     * marshalled by value is a no-op.)
     * @param type Full platform-independent type name.
     * @param handle Handle to the native instance to be released.
     * @returns A promise for completion of releasing the native instance.
     */
    releaseInstance(type: string, handle: Promise<number>): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            if (handle) {
                handle.then(
                    handleValue => {
                        cordova.exec(
                            function () {
                                resolve();
                            },
                            reject,
                            CordovaNativeBridge.serviceName,
                            BridgeCallType[BridgeCallType.releaseInstance],
                            <any[]>[{ type: type, handle: handleValue }]);
                    },
                    reject);
            } else {
                resolve();
            }
        });
    }

    /**
     * Gets the value of a property on the native instance.
     * @param instance An instance of a NativeReference subclass that is a bridged native instance.
     * @param property Name of the property to get.
     * @returns A promise for the value of the property. The promise may fail with any exceptions thrown by
     * the asynchronous native constructor.
     */
    getProperty(instance: NativeReference, property: string): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    cordova.exec(
                        function () {
                            var result = arguments[0];
                            resolve(Marshaller.marshalFromNative(result));
                        },
                        reject,
                        CordovaNativeBridge.serviceName,
                        BridgeCallType[BridgeCallType.getProperty],
                        <any[]>[marshalledInstance, property]);
                },
                reject);
        });
    }

    /**
     * Sets the value of a property on the native instance.
     * @param instance An instance of a NativeReference subclass that is a bridged native instance.
     * @param property Name of the property to set.
     * @param value The value to set.
     * @returns A promise for completion of setting the property. The promise may fail with any exceptions thrown by
     * the asynchronous native constructor.
     */
    setProperty(instance: NativeReference, property: string, value: any): Promise<void> {
        return new Promise<any>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    cordova.exec(
                        () => {
                            resolve();
                        },
                        reject,
                        CordovaNativeBridge.serviceName,
                        BridgeCallType[BridgeCallType.setProperty],
                        [marshalledInstance, property, value]);
                },
                reject);
        });
    }

    /**
     * Invokes a method on the native instance.
     * @param instance An instance of a NativeObject subclass that is marshalled-by-value from/to native code,
     * or a NativeReference subclass that is a bridged native instance.
     * @param method Name of the method to invoke.
     * @param args Arguments to pass to the method.
     * @returns A promise for the method's return value, or a promise for the method completion if the method's
     * return type is void. The promise may fail with any exceptions thrown by the asynchronous native constructor
     * or native method.
     */
    invokeMethod(instance: NativeObject, method: string, args: any[]): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    return Marshaller.marshalToNative(args).then(
                        marshalledArgs => {
                            cordova.exec(
                                function () {
                                    var result = arguments[0];
                                    resolve(Marshaller.marshalFromNative(result));
                                },
                                reject,
                                CordovaNativeBridge.serviceName,
                                BridgeCallType[BridgeCallType.invokeMethod],
                                <any[]>[marshalledInstance, method, marshalledArgs]);
                        },
                        reject);
                },
                reject);
        });
    }

    /**
     * Adds a listener to an event on the native instance.
     * @param instance An instance of a NativeReference subclass that is a bridged native instance.
     * @param event Name of the event.
     * @param listener callback method to be invoked by the event. The callback takes a single parameter
     * which is an object with event-specific properties.
     * @return A promise for completion of adding the listener.
     */
    addEventListener(instance: NativeReference, event: string, listener: (e: any) => void): Promise<void> {
        var listeners: EventListenersCollection = this.eventListeners;
        if (listeners.find(instance, event, listener)) {
            return Promise.resolve(undefined);
        }
        var firstCallback: boolean = true;
        return new Promise<void>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    cordova.exec(
                        function () {
                            if (firstCallback) {
                                firstCallback = false;
                                var token: string = arguments[0];
                                listeners.add(instance, event, listener, token, null);
                                resolve();
                            } else {
                                listener(Marshaller.marshalFromNative(arguments[0]));
                            }
                        },
                        reject,
                        CordovaNativeBridge.serviceName,
                        BridgeCallType[BridgeCallType.addEventListener],
                        <any[]>[marshalledInstance, event]);
                },
                reject);
        });
    }

    /**
     * Removes a listener to an event on the native instance.
     * @param instance An instance of a NativeReference subclass that is a bridged native instance.
     * @param event Name of the event.
     * @param listener Callback method which was previously added as a listener to the same event.
     * @return A promise for completion of removing the listener.
     */
    removeEventListener(instance: NativeReference, event: string, listener: (e: any) => void): Promise<void> {
        var listeners: EventListenersCollection = this.eventListeners;
        var listenerRecord: (EventListenerRecord | null) = listeners.find(instance, event, listener);
        if (!listenerRecord) {
            return Promise.resolve(undefined);
        }

        var registrationToken: string = listenerRecord.token;
        return new Promise<void>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    cordova.exec(
                        function () {
                            listeners.remove(instance, event, listener, registrationToken);
                            resolve();
                        },
                        reject,
                        CordovaNativeBridge.serviceName,
                        BridgeCallType[BridgeCallType.removeEventListener],
                        <any[]>[marshalledInstance, event, registrationToken]);
                },
                reject);
        });
    }
}

var bridge = new CordovaNativeBridge();

// Uncomment this line to automatically log all calls over the bridge.
// bridge = new LoggingNativeAsyncBridge(bridge);

export { bridge, NativeObject, NativeReference, Promise }
