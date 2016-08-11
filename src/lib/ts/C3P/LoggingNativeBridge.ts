// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

import { Promise } from "es6-promise";
import { NativeType, NativeObject, NativeReference } from "./NativeObject";
import { BridgeCallType, NativeBridge, NativeAsyncBridge } from "./NativeBridge";
import { Marshaller } from "./Marshaller";

/**
 * Wraps a native bridge and automatically logs calls and results (or exceptions) that go over the bridge.
 */
export class LoggingNativeBridge implements NativeBridge {
    private bridge: NativeBridge;

    constructor(bridge: NativeBridge) {
        this.bridge = bridge;
    }

    registerType(type: string, constructor: NativeType): void {
        console.log("REGISTER " + type);
        Marshaller.registerType(type, constructor);
    }

    getStaticProperty(type: string, property: string): any {
        return LoggingNativeBridge.trace(BridgeCallType.getStaticProperty, type, property, null,
            () => this.bridge.getStaticProperty(type, property));
    }

    setStaticProperty(type: string, property: string, value: any): void {
        return LoggingNativeBridge.trace(BridgeCallType.setStaticProperty, type, property, [value],
            () => this.bridge.setStaticProperty(type, property, value));
    }

    invokeStaticMethod(type: string, method: string, args: any[]): any {
        return LoggingNativeBridge.trace(BridgeCallType.invokeStaticMethod, type, method, args,
            () => this.bridge.invokeStaticMethod(type, method, args));
    }

    addStaticEventListener(type: string, event: string, listener: (args: any) => void):void {
        return LoggingNativeBridge.trace(BridgeCallType.addStaticEventListener, type, event, null,
            () => this.bridge.addStaticEventListener(type, event, listener));
    }

    removeStaticEventListener(type: string, event: string, listener: (args: any) => void): void {
        return LoggingNativeBridge.trace(BridgeCallType.removeStaticEventListener, type, event, null,
            () => this.bridge.removeStaticEventListener(type, event, listener));
    }

    createInstance(type: string, args: any[]): number {
        return LoggingNativeBridge.trace(BridgeCallType.createInstance, type, null, args,
            () => this.bridge.createInstance(type, args));
    }

    releaseInstance(type: string, handle: number): void {
        return LoggingNativeBridge.trace(BridgeCallType.releaseInstance, type, null, null,
            () => this.bridge.releaseInstance(type, handle));
    }

    getProperty(instance: NativeReference, property: string): any {
        return LoggingNativeBridge.trace(BridgeCallType.getProperty, instance, property, null,
            () => this.bridge.getProperty(instance, property));
    }

    setProperty(instance: NativeReference, property: string, value: any): void {
        return LoggingNativeBridge.trace(BridgeCallType.setProperty, instance, property, [value],
            () => this.bridge.setProperty(instance, property, value));
    }

    invokeMethod(instance: NativeObject, method: string, args: any[]): any {
        return LoggingNativeBridge.trace(BridgeCallType.invokeMethod, instance, method, args,
            () => this.bridge.invokeMethod(instance, method, args));
    }

    addEventListener(instance: NativeReference, event: string, listener: (args: any) => void): void {
        return LoggingNativeBridge.trace(BridgeCallType.addEventListener, instance, event, null,
            () => this.bridge.addEventListener(instance, event, listener));
    }

    removeEventListener(instance: NativeReference, event: string, listener: (args: any) => void): void {
        return LoggingNativeBridge.trace(BridgeCallType.removeEventListener, instance, event, null,
            () => this.bridge.removeEventListener(instance, event, listener));
    }

    private static trace<T>(
        callType: BridgeCallType,
        typeOrInstance: (string | NativeObject),
        member: (string | null),
        args: (any[] | null),
        call: () => T): T {
        var type: string;
        if (typeOrInstance instanceof NativeObject) {
            type = (<NativeObject>typeOrInstance).type;
        } else {
            type = <string>typeOrInstance;
        }

        var dotMember = (member ? "." + member : "");
        console.log("CALL " + BridgeCallType[callType] + ": " + type + dotMember + "(" +
            (args ? JSON.stringify(args) : "") + ")");

        try {
            var result: T = call();
            console.log("SUCCESS " + BridgeCallType[callType] + ": " + type + dotMember + "(" +
                (args ? JSON.stringify(args) : "") + ") => " + JSON.stringify(result));
            return result;
        } catch (error) {
            console.log("ERROR " + BridgeCallType[callType] + ": " + type + dotMember + "(" +
                (args ? JSON.stringify(args) : "") + ") => " + JSON.stringify(error));
            throw error;
        }
    }
}

/**
 * Wraps a native async bridge and automatically logs calls and results (or exceptions) that go over the bridge.
 */
export class LoggingNativeAsyncBridge implements NativeAsyncBridge {
    private bridge: NativeAsyncBridge;

    constructor(bridge: NativeAsyncBridge) {
        this.bridge = bridge;
    }

    registerType(type: string, constructor: NativeType): void {
        console.log("REGISTER " + type);
        Marshaller.registerType(type, constructor);
    }

    getStaticProperty(type: string, property: string): Promise<any> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.getStaticProperty, type, property, null,
            () => this.bridge.getStaticProperty(type, property));
    }

    setStaticProperty(type: string, property: string, value: any): Promise<void> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.setStaticProperty, type, property, [value],
            () => this.bridge.setStaticProperty(type, property, value));
    }

    invokeStaticMethod(type: string, method: string, args: any[]): Promise<any> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.invokeStaticMethod, type, method, args,
            () => this.bridge.invokeStaticMethod(type, method, args));
    }

    addStaticEventListener(type: string, event: string, listener: (args: any) => void): Promise<void> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.addStaticEventListener, type, event, null,
            () => this.bridge.addStaticEventListener(type, event, listener));
    }

    removeStaticEventListener(type: string, event: string, listener: (args: any) => void): Promise<void> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.removeStaticEventListener, type, event, null,
            () => this.bridge.removeStaticEventListener(type, event, listener));
    }

    createInstance(type: string, args: any[]): Promise<number> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.createInstance, type, null, args,
            () => this.bridge.createInstance(type, args));
    }

    releaseInstance(type: string, handle: Promise<number>): Promise<void> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.releaseInstance, type, null, null,
            () => this.bridge.releaseInstance(type, handle));
    }

    getProperty(instance: NativeReference, property: string): Promise<any> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.getProperty, instance, property, null,
            () => this.bridge.getProperty(instance, property));
    }

    setProperty(instance: NativeReference, property: string, value: any): Promise<void> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.setProperty, instance, property, [value],
            () => this.bridge.setProperty(instance, property, value));
    }

    invokeMethod(instance: NativeObject, method: string, args: any[]): Promise<any> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.invokeMethod, instance, method, args,
            () => this.bridge.invokeMethod(instance, method, args));
    }

    addEventListener(instance: NativeReference, event: string, listener: (args: any) => void): Promise<void> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.addEventListener, instance, event, null,
            () => this.bridge.addEventListener(instance, event, listener));
    }

    removeEventListener(instance: NativeReference, event: string, listener: (args: any) => void): Promise<void> {
        return LoggingNativeAsyncBridge.trace(BridgeCallType.removeEventListener, instance, event, null,
            () => this.bridge.removeEventListener(instance, event, listener));
    }

    private static trace<T>(
        callType: BridgeCallType,
        typeOrInstance: (string | NativeObject),
        member: (string | null),
        args: (any[] | null),
        call: () => Promise<T>): Promise<T> {

        var type: string;
        if (typeOrInstance instanceof NativeObject) {
            type = (<NativeObject>typeOrInstance).type;
        } else {
            type = <string>typeOrInstance;
        }

        var dotMember = (member ? "." + member : "");
        console.log("CALL " + BridgeCallType[callType] + ": " + type + dotMember + "(" +
            (args ? JSON.stringify(args) : "") + ")");

        return new Promise<T>((resolve, reject) => {
            return call()
                .then((result: T) => {
                    console.log("SUCCESS " + BridgeCallType[callType] + ": " + type + dotMember + "(" +
                        (args ? JSON.stringify(args) : "") + ") => " + JSON.stringify(result));
                    resolve(result);
                })
                .catch((error: any) => {
                    console.log("ERROR " + BridgeCallType[callType] + ": " + type + dotMember + "(" +
                        (args ? JSON.stringify(args) : "") + ") => " + JSON.stringify(error));
                    reject(error);
                });
        });
    }
}
