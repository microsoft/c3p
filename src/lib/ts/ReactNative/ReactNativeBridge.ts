// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

import { Promise } from "es6-promise";
import { NativeModules, NativeAppEventEmitter, EventSubscription } from "react-native";
import { NativeType, NativeObject, NativeReference } from "../C3P/NativeObject";
import { NativeAsyncBridge } from "../C3P/NativeBridge";
import { Marshaller } from "../C3P/Marshaller";
import { LoggingNativeAsyncBridge } from "../C3P/LoggingNativeBridge";
import { EventListenersCollection, EventListenerRecord } from "../C3P/NativeEventListeners";

var nativeBridge = NativeModules.C3P;

/**
 * Implementation of a native bridge in the React Native JavaScript environment.
 * The React Native JS to native bridge is always asynchronous.
 */
class ReactNativeBridge implements NativeAsyncBridge {
    /**
     * Collection of event listeners added to any bridged object, tracked so that they can be removed via tokens.
     */
    private eventListeners: EventListenersCollection = new EventListenersCollection();

    /**
     * Registers a type as a known bridged type, enabling instances of the type to be automatically
     * constructed from handles or serialized values returned over the bridge.
     * @param type Full platform-independent type name (including namespace).
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
        return new Promise<number>((resolve, reject) => {
            nativeBridge.getStaticProperty(type, property).then(
                (result: any) => {
                    resolve(Marshaller.marshalFromNative(result));
                },
                reject);
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
        return new Promise<void>((resolve, reject) => {
            nativeBridge.setStaticProperty(type, property, [ value ]).then(
                (result: any) => {
                    resolve();
                },
                reject);
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
        return new Promise<number>((resolve, reject) => {
            Marshaller.marshalToNative(args).then(
                marshalledArgs => {
                    nativeBridge.invokeStaticMethod(type, method, marshalledArgs).then(
                        (result: any) => {
                            resolve(Marshaller.marshalFromNative(result));
                        },
                        reject);
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
        return new Promise<void>((resolve, reject) => {
            nativeBridge.addStaticEventListener(type, event).then(
                (token: string) => {
                    var subscription: EventSubscription = NativeAppEventEmitter.addListener(
                        event + ":" + token,
                        (e: any) => {
                            listener(Marshaller.marshalFromNative(e));
                        });
                    listeners.add(type, event, listener, token, subscription);
                    resolve();
                },
                reject);
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

        var registrationToken: string = listenerRecord.token;
        var subscription = listenerRecord.subscription;
        return new Promise<void>((resolve, reject) => {
            nativeBridge.removeStaticEventListener(type, event, registrationToken).then(
                () => {
                    subscription.remove();
                    listeners.remove(type, event, listener, registrationToken);
                    resolve();
                },
                reject);
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
                    nativeBridge.createInstance(type, marshalledArgs).then(
                        (instanceInfo: any) => {
                            resolve(instanceInfo.handle);
                        },
                        reject);
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
                        nativeBridge.releaseInstance({ type: type, handle: handleValue }).then(
                            (result: any) => {
                                resolve();
                            },
                            reject);
                    },
                    reject);
            } else {
                resolve();
            }
        });
    }

    /**
     * Gets the value of a property on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param property Name of the property to get.
     * @returns A promise for the value of the property. The promise may fail with any exceptions thrown by
     * the asynchronous native constructor.
     */
    getProperty(instance: NativeReference, property: string): Promise<any> {
        return new Promise<number>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    nativeBridge.getProperty(marshalledInstance, property).then(
                        (result: any) => {
                            resolve(Marshaller.marshalFromNative(result));
                        },
                        reject);
                },
                reject);
        });
    }

    /**
     * Sets the value of a property on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
     * @param property Name of the property to set.
     * @param value The value to set.
     * @returns A promise for completion of setting the property. The promise may fail with any exceptions thrown by
     * the asynchronous native constructor.
     */
    setProperty(instance: NativeReference, property: string, value: any): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    nativeBridge.setProperty(marshalledInstance, property, [ value ]).then(
                        (result: any) => {
                            resolve();
                        },
                        reject);
                },
                reject);
        });
    }

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
    invokeMethod(instance: NativeObject, method: string, args: any[]): Promise<any> {
        return new Promise<number>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    return Marshaller.marshalToNative(args).then(
                        marshalledArgs => {
                            nativeBridge.invokeMethod(marshalledInstance, method, marshalledArgs).then(
                                (result: any) => {
                                    resolve(Marshaller.marshalFromNative(result));
                                },
                                reject);
                        },
                        reject);
                },
                reject);
        });
    }

    /**
     * Adds a listener to an event on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
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
        return new Promise<void>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    nativeBridge.addEventListener(marshalledInstance, event).then(
                        (token: string) => {
                            var subscription: EventSubscription = NativeAppEventEmitter.addListener(
                                event + ":" + token,
                                (e: any) => {
                                    listener(Marshaller.marshalFromNative(e));
                                });
                            listeners.add(instance, event, listener, token, subscription);
                            resolve();
                        },
                        reject);
                },
                reject);
        });
    }

    /**
     * Removes a listener to an event on the native instance.
     * @param instance An instance of a NativeReference subclass that includes a handle to a native instance.
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
        var subscription = listenerRecord.subscription;
        return new Promise<void>((resolve, reject) => {
            Marshaller.marshalToNative(instance).then(
                marshalledInstance => {
                    nativeBridge.removeEventListener(marshalledInstance, event, registrationToken).then(
                        () => {
                            subscription.remove();
                            listeners.remove(instance, event, listener, registrationToken);
                            resolve();
                        },
                        reject);
                },
                reject);
        });
    }
}

var bridge = new ReactNativeBridge();

// Uncomment this line to automatically log all calls over the bridge.
// bridge = new LoggingNativeAsyncBridge(bridge);

export { bridge, NativeObject, NativeReference }
