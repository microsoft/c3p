// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

import { Promise } from "es6-promise";
import { NativeType, NativeObjectType, NativeReferenceType, NativeObject, NativeReference } from "./NativeObject";

/**
 * Helper class for converting arguments and return values of calls made over a JS to native bridge.
 */
export class Marshaller {
    /**
     * Collection of types that have been registered for bridging. Maps from platform-independent type full name
     * to constructor function.
     */
    private static typeMap: { [type: string]: NativeType } = {};

    /**
     * Registers a type for bridging.
     * @param type Platform-independent type full name.
     * @param constructor Constructor function for the type.
     */
    static registerType(type: string, constructor: NativeType): void {
        Marshaller.typeMap[type] = constructor;
    }

    /**
     * Marshals an object or array to a form suitable for passing to an asyncronous call over the bridge. This involves
     * awaiting resolution of any asynchronously-constructed instances that were marshalled by reference.
     * @param jsObject The object or array that will be passed to a call over the bridge.
     * @returns A promise for the marshalled object.
     */
    static marshalToNative(jsObject: any): Promise<any> {
        if (jsObject === null || typeof(jsObject) != "object") {
            // The value (null, boolean, number, or string) doesn't need marshalling.
            return Promise.resolve(jsObject);
        } else if (jsObject instanceof Date) {
            return Promise.resolve({ "type": "<date>", "value": jsObject.getTime() });
        } else if (jsObject.handle) {
            // Resolve the promised handle.
            var instanceType: string = jsObject.type;
            return jsObject.handle.then((resolvedHandle: number) => {
                return {
                    type: instanceType,
                    handle: resolvedHandle
                };
            });
        } else if (Array.isArray(jsObject)) {
            // Reduce the array to a promised marshalled array.
            var localArray: any[] = jsObject;
            return localArray.reduce(function (promisedResult: Promise<any[]>, currentValue: any) {
                return promisedResult.then(function (marshalledArray) {
                    return Marshaller.marshalToNative(currentValue).then(function (marshalledItem) {
                        marshalledArray.push(marshalledItem);
                        return marshalledArray;
                    });
                });
            }, Promise.resolve([]));
        } else {
            // Reduce the object to a promised object with all members marshalled.
            var typeConversions = jsObject.constructor.typeConversions || {};
            return Object.keys(jsObject).reduce(function (
                    promisedResult: Promise<any>, key: string) {
                return promisedResult.then(function (marshalledObject) {
                    if (typeConversions[key] === "uuid" &&
                            typeof (jsObject[key]) === "string") {
                        marshalledObject[key] = { "type": "<uuid>", "value": jsObject[key] };
                        return marshalledObject;
                    } else if (typeConversions[key] === "uri" &&
                            typeof(jsObject[key]) === "string") {
                        marshalledObject[key] = { "type": "<uri>", "value": jsObject[key] };
                        return marshalledObject;
                    } else {
                        return Marshaller.marshalToNative(jsObject[key]).then(function (
                                marshalledMember) {
                            marshalledObject[key] = marshalledMember;
                            return marshalledObject;
                        });
                    }
                });
            }, Promise.resolve({}));
        }
    }

    /**
     * Converts JSON objects returned by a call over the bridge into correctly-typed objects. This involves
     * instantiating any bridged types using their registered constructor functions.
     * @param nativeObject The JSON object received over the bridge.
     * @return The marshalled object.
     */
    static marshalFromNative(nativeObject: any): any {
        if (nativeObject === null || typeof(nativeObject) != "object") {
            return nativeObject;
        } else if (Array.isArray(nativeObject)) {
            var localArray: Array<any> = new Array<any>();
            for (var i: number = 0; i < nativeObject.length; i++) {
                localArray[i] = Marshaller.marshalFromNative(nativeObject[i]);
            }
            return localArray;
        } else if (typeof (nativeObject.type) != "string") {
            var str: string;
            try {
                str = JSON.stringify(nativeObject);
            } catch (e) {
                str = e.message || JSON.stringify(e);
            }
            console.log("Marshaller: Missing type property when marshalling from native: " + str);
            return null;
        }

        var localType: NativeType = Marshaller.typeMap[nativeObject.type];
        if (!localType) {
            if (nativeObject.type == "<uuid>" || nativeObject.type == "<uri>") {
                return nativeObject;
            } else if (nativeObject.type == "<date>") {
                return new Date(nativeObject.value);
            }

            console.log("Marshaller: Type not found when marshalling from native: " + nativeObject.type);
            return null;
        }

        var localObject: any;
        if (typeof (nativeObject.handle) == "number") {
            localObject = new (<NativeReferenceType>localType)(Promise.resolve(nativeObject.handle));
        } else {
            localObject = new (<NativeObjectType>localType)();
            Object.keys(nativeObject).forEach(function (propertyName) {
                if (propertyName != "type" && propertyName != "handle") {
                    localObject[propertyName] = Marshaller.marshalFromNative(nativeObject[propertyName]);
                }
            });
        }
        return localObject;
    }
}
