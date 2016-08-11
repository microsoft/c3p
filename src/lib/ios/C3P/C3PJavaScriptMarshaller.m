// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PJavaScriptMarshaller.h"
#import "C3PNamespaceMapper.h"
#import "C3PInvocationHelper.h"
#import "C3PApplicationContext.h"
#import <objc/runtime.h>


#define INVALID_HANDLE_VALUE -1

@interface C3PJavaScriptMarshaller ()

@property (readwrite) id<C3PApplicationContext> context;
@property (readwrite) C3PNamespaceMapper* namespaceMapper;

@end

@implementation C3PJavaScriptMarshaller {
    NSMapTable<Class, NSMapTable<NSObject*, NSNumber*>*>* _objectsToHandles;
    NSMapTable<Class, NSMapTable<NSNumber*, NSObject*>*>* _handlesToObjects;
    int counter;
    NSMutableSet<NSString*>* _marshalByValueClasses;
}

- (C3PJavaScriptMarshaller*) initWithContext: (id<C3PApplicationContext>) context
                          andNamespaceMapper: (C3PNamespaceMapper*) namespaceMapper {
    self = [super init];
    if (self)
    {
        self.context = context;
        self.namespaceMapper = namespaceMapper;
        _objectsToHandles = [NSMapTable mapTableWithKeyOptions: 0 valueOptions: NSMapTableObjectPointerPersonality];
        _handlesToObjects = [NSMapTable mapTableWithKeyOptions: 0 valueOptions: NSMapTableObjectPointerPersonality];
        _marshalByValueClasses = [[NSMutableSet<NSString*> alloc] init];
    }
    return self;
}

- (void) registerMarshalByValueClass: (NSString*) className {
    [_marshalByValueClasses addObject: className];
}

- (NSObject*) marshalToJavaScript: (NSObject*) object {
    if (object == nil) {
        return [NSNull null];
    }

    if ([object isKindOfClass: [NSArray class]]) {
        NSArray* array = (NSArray*)object;
        NSMutableArray* convertedArray = [[NSMutableArray alloc] initWithCapacity: array.count];
        for (NSUInteger i = 0; i < array.count; i++) {
            convertedArray[i] = [self marshalToJavaScript: array[i]];
        }
        return convertedArray;
    }

    if ([object isKindOfClass: [NSNumber class]] ||
        [object isKindOfClass: [NSString class]] ||
        [object isKindOfClass: [NSValue class]]) {
        return object;
    }

    Class class = [object class];
    NSString* pluginClassFullName = [_namespaceMapper getPluginClassForObjCClass: NSStringFromClass(class)];
    if (pluginClassFullName == nil) {
        return nil;
    }

    NSString* simpleClassName = pluginClassFullName;
    NSRange lastDotRange = [pluginClassFullName rangeOfString: @"." options: NSBackwardsSearch | NSLiteralSearch];
    if (lastDotRange.location != NSNotFound) {
        simpleClassName = [pluginClassFullName substringFromIndex: lastDotRange.location + 1];
    }

    if ([_marshalByValueClasses containsObject: simpleClassName]) {
        NSMutableDictionary* jsObject = [[NSMutableDictionary alloc] init];
        jsObject[@"type"] = pluginClassFullName;
        [self copyPropertiesFromObject: object toJavaScriptObject: jsObject];
        return jsObject;
    }
    else if ([pluginClassFullName isEqualToString: @"<uuid>"]) {
        NSMutableDictionary* jsObject = [[NSMutableDictionary alloc] init];
        jsObject[@"type"] = pluginClassFullName;
        jsObject[@"value"] = ((NSUUID*)object).UUIDString;
        return jsObject;
    }
    else if ([pluginClassFullName isEqualToString: @"<uri>"]) {
        NSMutableDictionary* jsObject = [[NSMutableDictionary alloc] init];
        jsObject[@"type"] = pluginClassFullName;
        jsObject[@"value"] = ((NSURL*)object).absoluteString;
        return jsObject;
    }
    else if ([pluginClassFullName isEqualToString: @"<date>"]) {
        NSMutableDictionary* jsObject = [[NSMutableDictionary alloc] init];
        jsObject[@"type"] = pluginClassFullName;
        jsObject[@"value"] = [NSNumber numberWithDouble: [((NSDate*)object) timeIntervalSince1970]];
        return jsObject;
    }

    NSMapTable<NSObject*, NSNumber*>* classObjectsToHandles = [_objectsToHandles objectForKey: class];
    if (classObjectsToHandles == nil) {
        classObjectsToHandles = [NSMapTable mapTableWithKeyOptions: NSMapTableObjectPointerPersonality valueOptions: 0];
        [_objectsToHandles setObject: classObjectsToHandles forKey: class];
    }

    NSMapTable<NSNumber*, NSObject*>* classHandlesToObjects = [_handlesToObjects objectForKey: class];
    if (classHandlesToObjects == nil) {
        classHandlesToObjects = [NSMapTable mapTableWithKeyOptions: 0 valueOptions: NSMapTableObjectPointerPersonality];
        [_handlesToObjects setObject: classHandlesToObjects forKey: class];
    }

    NSNumber* handle = [classObjectsToHandles objectForKey: object];
    if (handle == nil) {
        handle = [NSNumber numberWithInt: ++counter];
        [classObjectsToHandles setObject: handle forKey: object];
        [classHandlesToObjects setObject: object forKey: handle];
    }

    NSMutableDictionary* jsObject = [[NSMutableDictionary alloc] init];
    [jsObject setObject: pluginClassFullName forKey: @"type"];
    [jsObject setObject: handle forKey: @"handle"];

    return jsObject;
}

- (NSObject*) marshalFromJavaScript: (NSObject*) jsObject class: (Class) type {
    if (jsObject == nil || type == nil) {
        return nil;
    }

    if ([jsObject isKindOfClass: [NSArray class]]) {
        NSArray* jsArray = (NSArray*)jsObject;
        NSMutableArray* convertedArray = [[NSMutableArray alloc] initWithCapacity: jsArray.count];
        for (NSUInteger i = 0; i < jsArray.count; i++) {
            Class itemClass = [self getClassForJavaScriptObject: jsArray[i]];
            if (itemClass != nil) {
                convertedArray[i] = [self marshalFromJavaScript: jsArray[i] class: itemClass];
            }
        }
        return convertedArray;
    }
    else if (!([jsObject isKindOfClass: [NSDictionary class]])) {
        return jsObject;
    }

    NSDictionary* jsDictionary = (NSDictionary*)jsObject;
    id handleObject = jsDictionary[@"handle"];
    if (handleObject == nil || ![handleObject isKindOfClass: [NSNumber class]]) {
        if (type == [NSUUID class]) {
            id uuidValue = jsDictionary[@"value"];
            if ([uuidValue isKindOfClass: [NSString class]]) {
                return [[NSUUID alloc] initWithUUIDString: (NSString*)uuidValue];
            }
        }
        else if (type == [NSURL class]) {
            id uriValue = jsDictionary[@"value"];
            if ([uriValue isKindOfClass: [NSString class]]) {
                return [NSURL URLWithString: (NSString*)uriValue];
            }
        }
        else if (type == [NSDate class]) {
            id dateValue = jsDictionary[@"value"];
            if ([dateValue isKindOfClass: [NSNumber class]]) {
                return [NSDate dateWithTimeIntervalSince1970: [((NSNumber*)dateValue) doubleValue]];
            }
        }

        NSObject* instance = [[type alloc] init];
        [self copyPropertiesFromJavaScriptObject: jsDictionary toObject: instance];
        return instance;
    }

    NSNumber* handle = handleObject;
    NSMapTable<NSNumber*, NSObject*>* classHandlesToObjects = [_handlesToObjects objectForKey: type];
    if (classHandlesToObjects != nil) {
        NSObject* object = [classHandlesToObjects objectForKey: handle];
        if (object != nil) {
            return object;
        }
    }

    if (type == [UIApplication class]) {
        return [self.context getApplication];
    }
    else if (type == [UIWindow class]) {
        return [self.context getCurrentWindow];
    }

    NSLog(@"C3PJavaScriptMarshaller: Proxied object of class %@ with handle %@ was not found",
          NSStringFromClass(type), handle);
    return nil;
}

- (Class) getClassForJavaScriptObject: (NSObject*) jsObject {
    if (![jsObject isKindOfClass: [NSDictionary class]]) {
        return [NSObject class];
    }

    NSString* itemType = [((NSDictionary*)jsObject) objectForKey: @"type"];
    if (itemType == nil) {
        NSLog(@"C3PJavaScriptMarshaller: Missing type field of proxied object.");
        return nil;
    }

    NSString* itemClassName = [_namespaceMapper getObjCClassForPluginClass: itemType];
    if (itemClassName == nil) {
        return nil;
    }

    Class itemClass = NSClassFromString(itemClassName);
    if (itemClass == nil) {
        NSLog(@"C3PJavaScriptMarshaller: Class not found for proxied object: %@", itemClassName);
        return nil;
    }

    return itemClass;
}

- (void) releaseMarshalledObject: (NSDictionary*) jsObject class: (Class) type {
    if (jsObject == nil || type == nil) {
        return;
    }

    id handleObject = [jsObject objectForKey: @"handle"];
    if (handleObject == nil || ![handleObject isKindOfClass: [NSNumber class]]) {
        return;
    }

    NSNumber* handle = handleObject;
    NSMapTable<NSNumber*, NSObject*>* classHandlesToObjects = [_handlesToObjects objectForKey: type];
    if (classHandlesToObjects != nil) {
        NSObject* object = [classHandlesToObjects objectForKey: handle];
        if (object != nil) {
            [classHandlesToObjects removeObjectForKey: handle];
            NSMapTable<NSObject*, NSNumber*>* classObjectsToHandles = [_objectsToHandles objectForKey: type];
            if (classObjectsToHandles != nil) {
                [classObjectsToHandles removeObjectForKey: object];
            }
        }
    }
}

- (void) copyPropertiesFromJavaScriptObject: (NSDictionary*) jsObject toObject: (NSObject*) object {
    for (NSString* propertyName in jsObject) {
        if ([@"type" isEqualToString: propertyName] || [@"handle" isEqualToString: propertyName]) {
            continue;
        }

        NSObject* value = jsObject[propertyName];
        if ([value isKindOfClass: [NSDictionary class]]) {
            NSString* pluginClassFullName = [(NSDictionary*)value objectForKey: @"type"];
            if (pluginClassFullName != nil) {
                NSString* objCClassName = [_namespaceMapper getObjCClassForPluginClass: pluginClassFullName];
                if (objCClassName != nil) {
                    Class class = NSClassFromString(objCClassName);
                    if (class != nil) {
                        value = [self marshalFromJavaScript: value class: class];
                    }
                }
            }
        }

        NSArray* arguments = [[NSArray alloc] initWithObjects: (value != nil ? value : [NSNull null]), nil];
        NSString* method = [NSString stringWithFormat: @"set%@%@",
                            [[propertyName substringToIndex: 1] uppercaseString],
                            [propertyName substringFromIndex: 1]];

        NSError* error;
        NSInvocation* invocation = [C3PInvocationHelper getInvocationForMethod: method
                                                                         class: [object class]
                                                                      instance: object
                                                                     arguments: arguments
                                                                marshaller: self
                                                                         error: &error];
        if (invocation != nil) {
            [invocation invoke];
        }
    }
}

- (void) copyPropertiesFromObject: (NSObject*) object toJavaScriptObject: (NSMutableDictionary*) jsObject {
    uint propertyCount;
    objc_property_t* propertyList = class_copyPropertyList([object class], &propertyCount);
    for (uint i = 0; i < propertyCount; i++) {
        objc_property_t property = propertyList[i];
        NSString* propertyName = [[NSString alloc] initWithUTF8String: property_getName(property)];

        NSError* error = nil;
        NSInvocation* invocation = [C3PInvocationHelper getInvocationForMethod: propertyName
                                                                         class: [object class]
                                                                      instance: object
                                                                     arguments: nil
                                                                marshaller: self
                                                                         error: &error];
        if (invocation == nil) {
            error = nil;
            invocation = [C3PInvocationHelper getInvocationForMethod: [@"is" stringByAppendingString: propertyName]
                                                               class: [object class]
                                                            instance: object
                                                           arguments: nil
                                                      marshaller: self
                                                               error: &error];
        }

        if (invocation != nil && error == nil) {
            [invocation invoke];

            NSObject* value = [C3PInvocationHelper convertReturnValueFromInvocation: invocation
                                                                     marshaller: self
                                                                              error: &error];
            if (error == nil) {
                jsObject[propertyName] = value;
            }
        }
    }
}

@end
