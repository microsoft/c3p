// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

import { Promise } from "es6-promise";

/**
 * Represents a native class that is marshalled to the JS environment; base class for generated JavaScript
 * bindings to native classes. A native instance can be either marshalled by reference or by value.
 */
export abstract class NativeObject {
    /**
     * Creates a new instance that represents a native object.
     * @param type Full platform-independent type name of the instance.
     */
    constructor(public type: string) {
    }
}

/**
 * Represents a native class that is marshalled to the JS environment by reference. The handle can be resolved
 * (on the other side of the bridge) to a corresponding native instance.
 */
export abstract class NativeReference extends NativeObject {
    /**
     * Creates a new instance that represents a native type.
     * @param type Full platform-independent type name of the instance.
     * @param handle Handle to the corresponding instance on the native side. This is a promise because
     * construction of native objects may be (but is not necessarily) asyncronous. This means the JavaScript
     * reference object may be constructed before the native instance construction has completed asynchronously;
     * however in that case any access to the instance's properties, methods, or events will still asynchronously
     * await completion of the constructor.
     */
    constructor(type: string, public handle: Promise<number>) {
        super(type);
    }

    /**
     * Disposes the object and releases the native instance. Note references are NOT counted, so the object is
     * invalid after a single call to dispose(). Attempts to call native methods on the instance after disposing it
     * will result in an error.
     */
    dispose(): Promise<void> {
        this.handle = Promise.reject<number>(new Error("Object disposed: " + this.type));
        return Promise.resolve();
    }

    /**
     * Special reference to the current application, used with constructors and methods that take an
     * implicit application context as their first parameter. The actual type is platform-specific (such as
     * UIKit.UIApplication or Android.App.Application). The instance will be swapped in by the native
     * side of the bridge.
     */
    static implicitAppContext: NativeReference;

    /**
     * Special reference to the current window, used with constructors and methods that take an
     * implicit page context as their first parameter. The actual type is platform-specific (such as
     * UIKit.UIWindow or Android.App.Activity). The instance will be swapped in by the native
     * side of the bridge.
     */
    static implicitWindowContext: NativeReference;
}

/**
 * Represents a special context reference that serves as a placeholder and cannot be disposed.
 */
class ContextReference extends NativeReference {
    constructor(type: string) {
        super(type, Promise.resolve(0));
    }

    dispose(): Promise<void> {
        throw new Error("Cannot dispose a reference to the " + this.type + " context.");
    }
}

NativeReference.implicitAppContext = new ContextReference("<application>");
NativeReference.implicitWindowContext = new ContextReference("<window>");

/**
 * Function interface for a NativeObject constructor; represents the type of a non-reference native object.
 */
export interface NativeObjectType {
    new(): NativeObject;
    type: string;
}

/**
 * Function interface for a NativeReference constructor; represents the type of a native reference.
 */
export interface NativeReferenceType {
    new(handle: Promise<number>): NativeReference;
    type: string;
}

/**
 * Union interface for a NativeObject or NativeReference constructor; represents the type of any native object.
 */
export type NativeType = NativeObjectType | NativeReferenceType;
