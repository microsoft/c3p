// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

import { Promise } from "es6-promise";
import { Cordova } from "cordova";
import { NativeType, NativeObject, NativeReference } from "../C3P/NativeObject";
declare var Windows: any;

/**
 * Implements calls to native code on Windows using the automatic JavaScript bindings to WinRT components.
 */
class CordovaWindowsBridge {
    private handleMap: { [type: string]: { [handle: number]: any } } = {};
    private nextHandle: number = 1;

    private eventHandlerMap: { [token: string]: (event: { type: string, detail: Array<any> }) => void } = {};
    private nextEventListenerToken: number = 1;

    private pluginTypeInfoInitialized: boolean = false;
    private pluginNamespaces: Array<string> = [];
    private pluginMarshalByValueClasses: Array<string> = [];

    getStaticPropertyAsync(type: string, property: string): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            var pluginType: any = this.resolvePluginType(type);
            if (!pluginType) {
                reject(new Error("Plugin type not found: " + type));
                return;
            }

            property = property.substring(0, 1).toLowerCase() + property.substring(1);

            var value: any;
            try {
                if (typeof (pluginType[property]) === "undefined") {
                    reject(new Error("Plugin property not found: " + type + "." + property));
                    return;
                }

                value = pluginType[property];
            } catch (e) {
                reject(e);
                return;
            }

            value = this.marshalFromNative(value);
            resolve(value);
        });
    }

    setStaticPropertyAsync(type: string, property: string, value: any): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            var pluginType: any = this.resolvePluginType(type);
            if (!pluginType) {
                reject(new Error("Plugin type not found: " + type));
                return;
            }

            property = property.substring(0, 1).toLowerCase() + property.substring(1);
            value = this.marshalToNative(value);

            if (typeof (pluginType[property]) === "undefined") {
                reject(new Error("Plugin property not found: " + type + "." + property));
                return;
            }

            try {
                pluginType[property] = value;
            } catch (e) {
                reject(e);
                return;
            }

            resolve();
        });
    }

    invokeStaticMethodAsync(type: string, method: string, args: Array<any>): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            var pluginType: any = this.resolvePluginType(type);
            if (!pluginType) {
                reject(new Error("Plugin type not found: " + type));
                return;
            }

            method = method.substring(0, 1).toLowerCase() + method.substring(1);

            var pluginMethod = pluginType[method];
            if (typeof (pluginMethod) !== "function") {
                reject(new Error("Plugin method not found: " + type + "." + method));
                return;
            }

            args = this.marshalToNativeArray(args);

            var result: any;
            try {
                result = pluginMethod.apply(null, args);
            } catch (e) {
                reject(e);
                return;
            }

            if (typeof (result) === "object" && typeof (result.then) === "function") {
                result.then(
                    (asyncResult: any) => {
                        asyncResult = this.marshalFromNative(asyncResult);
                        resolve(asyncResult);
                    },
                    reject);
            } else {
                result = this.marshalFromNative(result);
                resolve(result);
            }
        });
    }

    addStaticEventListenerAsync(type: string, event: string, callback: (eventObj: any) => void): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            var pluginType: any = this.resolvePluginType(type);
            if (!pluginType) {
                reject(new Error("Plugin type not found: " + type));
                return;
            }

            if (typeof (pluginType.addEventListener) != "function") {
                reject(new Error("Plugin method not found: " + type + ".addEventListener"));
                return;
            }

            var token: string = this.nextEventListenerToken.toString();
            this.nextEventListenerToken++;

            var eventHandler = (event: { type: string, detail: Array<any> }): void => {
                var eventObj: any = event.detail[0];
                eventObj = this.marshalFromNative(eventObj);
                callback(eventObj);
            };
            this.eventHandlerMap[token] = eventHandler;

            try {
                pluginType.addEventListener(event.toLowerCase(), eventHandler);
            } catch (e) {
                reject(e);
            }

            resolve(token);
        });
    }

    removeStaticEventListenerAsync(type: string, event: string, token: string): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            var pluginType: any = this.resolvePluginType(type);
            if (!pluginType) {
                reject(new Error("Plugin type not found: " + type));
                return;
            }

            if (typeof (pluginType.removeEventListener) != "function") {
                reject(new Error("Plugin method not found: " + type + ".removeEventListener"));
                return;
            }

            var eventHandler = this.eventHandlerMap[token];
            if (!eventHandler) {
                reject(new Error("Event handler not found for token: " + token));
                return;
            }

            try {
                pluginType.removeEventListener(event.toLowerCase(), eventHandler);
            } catch (e) {
                reject(e);
            }

            delete this.eventHandlerMap[token];
            resolve();
        });
    }

    createInstanceAsync(type: string, args: Array<any>): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            var pluginType: any = this.resolvePluginType(type);
            if (!pluginType) {
                reject(new Error("Plugin type not found: " + type));
                return;
            }

            args = this.marshalToNativeArray(args);

            var instance: any;
            try {
                instance = pluginType.apply(null, args);
            } catch (e) {
                reject(e);
                return;
            }

            var marshalledInstance: any = this.marshalFromNative(instance);
            resolve(marshalledInstance);
        });
    }

    releaseInstanceAsync(type: string, handle: number): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            var typeHandleMap = this.handleMap[type];
            if (typeHandleMap) {
                delete typeHandleMap[handle];
            }

            resolve();
        });
    }

    getPropertyAsync(marshalledInstance: { type: string, handle: number }, property: string): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            var instance: any = this.handleToObject(marshalledInstance.type, marshalledInstance.handle);
            if (!instance) {
                reject(new Error("Instance " + marshalledInstance.type + "#" + marshalledInstance.handle + " not found."));
                return;
            }

            property = property.substring(0, 1).toLowerCase() + property.substring(1);

            var value: any;
            try {
                value = instance[property];

                if (typeof (value) === "undefined") {
                    reject(new Error("Plugin property not found: " + marshalledInstance.type + "." + property));
                    return;
                }
            } catch (e) {
                reject(e);
                return;
            }

            value = this.marshalFromNative(value);
            resolve(value);
        });
    }

    setPropertyAsync(
            marshalledInstance: { type: string, handle: number }, property: string, value: string): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            var instance: any = this.handleToObject(marshalledInstance.type, marshalledInstance.handle);
            if (!instance) {
                reject(new Error("Instance " + marshalledInstance.type + "#" + marshalledInstance.handle + " not found."));
                return;
            }

            property = property.substring(0, 1).toLowerCase() + property.substring(1);
            value = this.marshalToNative(value);

            try {
                if (typeof (instance[property]) === "undefined") {
                    reject(new Error("Plugin property not found: " + marshalledInstance.type + "." + property));
                    return;
                }

                instance[property] = value;
            } catch (e) {
                reject(e);
                return;
            }

            resolve();
        });
    }

    invokeMethodAsync(
            marshalledInstance: { type: string, handle: number }, method: string, args: Array<any>): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            var instance: any = this.handleToObject(marshalledInstance.type, marshalledInstance.handle);
            if (!instance) {
                reject(new Error("Instance " + marshalledInstance.type + "#" + marshalledInstance.handle + " not found."));
                return;
            }

            method = method.substring(0, 1).toLowerCase() + method.substring(1);

            var pluginMethod = instance[method];
            if (typeof (pluginMethod) !== "function") {
                reject(new Error("Plugin method not found: " + marshalledInstance.type + "." + method));
                return;
            }

            args = this.marshalToNativeArray(args);

            var result: any;
            try {
                result = pluginMethod.apply(instance, args);
            } catch (e) {
                reject(e);
                return;
            }

            if (typeof (result) === "object" && typeof (result.then) === "function") {
                result.then(
                    (asyncResult: any) => {
                        asyncResult = this.marshalFromNative(asyncResult);
                        resolve(asyncResult);
                    },
                    (e: Error) => {
                        reject(e);
                    });
            } else {
                result = this.marshalFromNative(result);
                resolve(result);
            }
        });
    }

    addEventListenerAsync(
            marshalledInstance: { type: string, handle: number },
            event: string,
            callback: (eventObj: any) => void): Promise<string> {
        return new Promise<any>((resolve, reject) => {
            var instance: any = this.handleToObject(marshalledInstance.type, marshalledInstance.handle);
            if (!instance) {
                reject(new Error("Instance " + marshalledInstance.type + "#" + marshalledInstance.handle + " not found."));
                return;
            }

            if (typeof (instance.addEventListener) != "function") {
                reject(new Error("Plugin method not found: " + marshalledInstance.type + ".addEventListener"));
                return;
            }

            var token: string = this.nextEventListenerToken.toString();
            this.nextEventListenerToken++;

            var eventHandler = (event: { type: string, detail: Array<any> }) => {
                var eventObj: any = event.detail[0];
                eventObj = this.marshalFromNative(eventObj);
                callback(eventObj);
            };
            this.eventHandlerMap[token] = eventHandler;

            try {
                instance.addEventListener(event.toLowerCase(), eventHandler);
            } catch (e) {
                reject(e);
            }

            resolve(token);
        });
    }

    removeEventListenerAsync(
            marshalledInstance: { type: string, handle: number }, event: string, token: string): Promise<void> {
        return new Promise<any>((resolve, reject) => {
            var instance: any = this.handleToObject(marshalledInstance.type, marshalledInstance.handle);
            if (!instance) {
                reject(new Error("Instance " + marshalledInstance.type + "#" + marshalledInstance.handle + " not found."));
                return;
            }

            if (typeof (instance.removeEventListener) != "function") {
                reject(new Error("Plugin method not found: " + marshalledInstance.type + ".addEventListener"));
                return;
            }

            var eventHandler = this.eventHandlerMap[token];
            if (!eventHandler) {
                reject(new Error("Event handler not found for token: " + token));
                return;
            }

            try {
                instance.removeEventListener(event.toLowerCase(), eventHandler);
            } catch (e) {
                reject(e);
            }

            delete this.eventHandlerMap[token];
            resolve();
        });
    }


    //// HELPERS ////

    private objectToHandle(type: string, obj: any): number {
        var handle = this.nextHandle;
        this.nextHandle++;

        var typeHandleMap = this.handleMap[type];
        if (!typeHandleMap) {
            typeHandleMap = {};
            this.handleMap[type] = typeHandleMap;
        }

        // TODO: Check if the object is already in the map.
        typeHandleMap[handle] = obj;

        return handle;
    }

    private handleToObject(type: string, handle: number): any {
        var typeHandleMap = this.handleMap[type];
        if (typeHandleMap) {
            return typeHandleMap[handle] || null;
        }
        return null;
    }

    private resolvePluginType(pluginTypeFullName: string): any {
        // TODO: Validate the the type is in one of the configured plugin namespaces.

        function recursiveResolve(parent: any, subTypeName: string): any {
            var dotIndex = subTypeName.indexOf('.');
            if (dotIndex < 0) {
                var pluginType: any = parent[subTypeName];
                return pluginType || null;
            }

            var subTypePart: string = subTypeName.substring(0, dotIndex);
            var child: any = parent[subTypePart];
            if (!child) {
                return null;
            }

            return recursiveResolve(child, subTypeName.substring(dotIndex + 1));
        }

        return recursiveResolve(window, pluginTypeFullName);
    }

    private marshalToNativeArray(array: Array<any>): Array<any> {
        if (array.length > 0 &&
            typeof (array[0]) === "object" && array[0] !== null &&
            (array[0].type === "<application>" || array[0].type === "<window>")) {
            // Implicit context is not supported or necessary on Windows.
            array = array.slice(1);
        }

        for (var i = 0; i < array.length; i++) {
            array[i] = this.marshalToNative(array[i]);
        }

        return array;
    }

    private marshalToNative(obj: any): any {
        if (typeof (obj) === "object" && obj !== null && Array.isArray(obj)) {
            var array: Array<any> = new Array();
            for (var i: number = 0; i < obj.length; i++) {
                array[i] = this.marshalToNative(obj[i]);
            }
            return array;
        } else if (typeof (obj) === "object" && obj !== null && typeof (obj.type) === "string") {
            var type: string = obj.type;
            var handle: number = obj.handle;
            if (typeof (handle) === "number") {
                return this.handleToObject(type, handle);
            } else if (type === "<uuid>" && typeof(obj.value) === "string") {
                return obj.value;
            } else if (type === "<uri>" && typeof(obj.value) === "string") {
                return new Windows.Foundation.Uri(obj.value);
            } else if (type === "<date>" && typeof(obj.value) === "number") {
                return new Date(obj.value);
            } else {
                var pluginType: any = this.resolvePluginType(type);
                if (pluginType) {
                    var pluginTypeInstance: any = new pluginType();
                    Object.keys(obj).forEach((property) => {
                        if (property !== "type") {
                            pluginTypeInstance[property] = this.marshalToNative(obj[property]);
                        }
                    });
                    return pluginTypeInstance;
                }
            }
        }

        return obj;
    }

    private marshalFromNative(obj: any): any {
        if (typeof (obj) === "object" && obj !== null) {
            if (typeof (obj.size) === "number" && typeof (obj.getAt) === "function") {
                var marshalledArray = new Array();
                for (var i = 0; i < obj.size; i++) {
                    var item: any;
                    try {
                        item = obj.getAt(i);
                    } catch (e) {
                        item = undefined;
                    }
                    marshalledArray[i] = this.marshalFromNative(item);
                }
                return marshalledArray;
            } else if (obj.constructor === Windows.Foundation.Uri) {
                return { type: "<uri>", value: obj.absoluteUri };
            } else if (obj.constructor === Date) {
                return { type: "<date>", value: obj.getTime() };
            } else {
                // Plugin classes can include a TypeFullName property to provide type info to the marshaller.
                var type: string = obj.typeFullName;

                if (typeof (type) !== "string") {
                    // The default toString() implementation returns the type full name.
                    type = obj.toString();
                    if (/^\[object .*\]$/.test(type)) {
                        type = type.substr(8, type.length - 9);
                    }
                }

                var className: string = type.substr(type.lastIndexOf(".") + 1);
                var isMarshalByValueType: boolean = this.pluginMarshalByValueClasses.indexOf(className) >= 0;

                if (!isMarshalByValueType) {
                    var handle: number = this.objectToHandle(type, obj);
                    return { type: type, handle: handle };
                }
                else {
                    var marshalledObject: any = { type: type };
                    for (var property in obj) {
                        // Note neither Object.keys(obj) nor obj.hasOwnProperty(property) would work here because
                        // obj is a WinRT object. Just copy non-function properties to the marshal-by-value object.
                        if (typeof obj[property] !== "function" && property !== "typeFullName") {
                            marshalledObject[property] = this.marshalFromNative(obj[property]);
                        }
                    }
                    return marshalledObject;
                }
            }
        }

        return obj;
    }

    ensureInitializedAsync(): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            if (this.pluginTypeInfoInitialized) {
                resolve();
            }

            Windows.ApplicationModel.Package.current.installedLocation.getFileAsync("config.xml").done(
                (configFile: any) => {
                    Windows.Data.Xml.Dom.XmlDocument.loadFromFileAsync(configFile).then(
                        (xmlDoc: any) => {
                            var pluginFeature = xmlDoc.selectSingleNodeNS(
                                "/ns:widget/ns:feature[@name='C3P']",
                                'xmlns:ns="' + xmlDoc.lastChild.namespaceUri + '"');
                            if (pluginFeature !== null) {
                                this.parsePluginConfigXml(pluginFeature);
                            }

                            this.pluginTypeInfoInitialized = true;
                            resolve();
                        }, (e: Error) => {
                            reject(e);
                        });
                }, (e: Error) => {
                    reject(e);
                });
        });
    }

    private parsePluginConfigXml(pluginRoot: any) {
        this.pluginNamespaces = [];
        this.pluginMarshalByValueClasses = [];

        var paramNodes = pluginRoot.childNodes;
        for (var i = 0; i < paramNodes.length; i++) {
            if (paramNodes[i].localName === "param") {
                var nameAttribute = paramNodes[i].attributes.getNamedItem("name");
                var valueAttribute = paramNodes[i].attributes.getNamedItem("value");
                var paramName: string = nameAttribute && nameAttribute.value;
                var paramValue: string = valueAttribute && valueAttribute.value;

                if (/^plugin-namespace\:/.test(paramName)) {
                    var ns: string = paramName.substr("plugin-namespace:".length);
                    this.pluginNamespaces.push(ns);
                } else if (/^plugin-class\:/.test(paramName)) {
                    var cls: string = paramName.substr("plugin-class:".length);
                    this.pluginMarshalByValueClasses.push(cls);
                }
            }
        }
    }
}

/**
 * The following exported functions wrap promise methods on this bridge instance
 * in the Cordova async proxy call pattern.
 */
var bridge = new CordovaWindowsBridge();

export function getStaticProperty(success: (value: any) => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var type: string = args[0];
        var property: string = args[1];
        return bridge.getStaticPropertyAsync(type, property);
    }).then(success, fail);
}

export function setStaticProperty(success: () => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var type: string = args[0];
        var property: string = args[1];
        var value: any = args[2];
        return bridge.setStaticPropertyAsync(type, property, value);
    }).then(success, fail);
}

export function invokeStaticMethod(success: (result: any) => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var type: string = args[0];
        var method: string = args[1];
        var methodArgs: Array<any> = args[2];
        return bridge.invokeStaticMethodAsync(type, method, methodArgs);
    }).then(success, fail);
}

export function addStaticEventListener(
    success: (result: any, flags: { keepCallback: boolean }) => void,
    fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var type: string = args[0];
        var event: string = args[1];
        return bridge.addStaticEventListenerAsync(type, event, (eventObj: any) => {
            success(eventObj, { keepCallback: true });
        });
    }).then(
        (token: string) => {
            success(token, { keepCallback: true });
        },
        fail);
}

export function removeStaticEventListener(success: () => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var type: string = args[0];
        var event: string = args[1];
        var token: string = args[2];
        return bridge.removeStaticEventListenerAsync(type, event, token);
    }).then(success, fail);
}

export function createInstance(success: (instance: any) => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var type: string = args[0];
        var constructorArgs: Array<any> = args[1];
        return bridge.createInstanceAsync(type, constructorArgs);
    }).then(success, fail);
}

export function releaseInstance(success: () => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var type: string = args[0];
        var handle: number = args[1];
        return bridge.releaseInstanceAsync(type, handle);
    }).then(success, fail);
}

export function getProperty(success: (value: any) => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var marshalledInstance: { type: string, handle: number } = args[0];
        var property: string = args[1];
        return bridge.getPropertyAsync(marshalledInstance, property);
    }).then(success, fail);
}

export function setProperty(success: () => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var marshalledInstance: { type: string, handle: number } = args[0];
        var property: string = args[1];
        var value: any = args[2];
        return bridge.setPropertyAsync(marshalledInstance, property, value);
    }).then(success, fail);
}

export function invokeMethod(success: (result: any) => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var marshalledInstance: { type: string, handle: number } = args[0];
        var method: string = args[1];
        var methodArgs: Array<any> = args[2];
        return bridge.invokeMethodAsync(marshalledInstance, method, methodArgs);
    }).then(success, fail);
}

export function addEventListener(
    success: (result: any, flags: { keepCallback: boolean }) => void,
    fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var marshalledInstance: { type: string, handle: number } = args[0];
        var event: string = args[1];
        return bridge.addEventListenerAsync(marshalledInstance, event, (eventObj: any) => {
            success(eventObj, { keepCallback: true });
        });
    }).then(
        (token: string) => {
            success(token, { keepCallback: true });
        },
        fail);
}

export function removeEventListener(success: () => void, fail: (e: Error) => void, args: Array<any>): void {
    bridge.ensureInitializedAsync().then(() => {
        var marshalledInstance: { type: string, handle: number } = args[0];
        var event: string = args[1];
        var token: string = args[2];
        return bridge.removeEventListenerAsync(marshalledInstance, event, token);
    }).then(success, fail);
}

// Register this module as a Cordova bridging proxy with the C3P service name.
declare var require: (moduleName: string) => { add(service: string, moduleExports: Array<any>): void };
declare var exports: Array<any>;
require("cordova/exec/proxy").add("C3P", exports);
