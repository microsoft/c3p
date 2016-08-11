// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PRReactModule.h"
#import "C3PJavaScriptBridge.h"
#import "C3PNamespaceMapper.h"
#import "RCTEventDispatcher.h"
#include <libkern/OSAtomic.h>


@implementation C3PRReactModule {
    C3PJavaScriptBridge* _jsBridge;
    NSMutableDictionary<NSString*, C3PJavaScriptEventListener>* _eventListenerMap;
    int _eventRegistrationId;
}

RCT_EXPORT_MODULE(C3P);

@synthesize bridge = _bridge;

- (C3PRReactModule*) init {
    self = [super init];
    if (self)
    {
        _jsBridge = [[C3PJavaScriptBridge alloc] initWithContext: self];
        _eventListenerMap = [[NSMutableDictionary<NSString*, C3PJavaScriptEventListener> alloc] initWithCapacity: 10];
        _eventRegistrationId = 0;

        [[NSNotificationCenter defaultCenter] addObserver: self
                                                 selector: @selector(onPause)
                                                     name: UIApplicationDidEnterBackgroundNotification
                                                   object: nil];
        [[NSNotificationCenter defaultCenter] addObserver: self
                                                 selector: @selector(onResume)
                                                     name: UIApplicationWillEnterForegroundNotification
                                                   object: nil];
    }
    return self;
}

- (UIApplication*) getApplication {
    return [UIApplication sharedApplication];
}

- (UIWindow*) getCurrentWindow {
    return [[UIApplication sharedApplication] keyWindow];
}

RCT_EXPORT_METHOD(registerNamespaceMapping: (NSString*) namespace forPrefix: (NSString*) prefix) {
    [_jsBridge.namespaceMapper registerPluginNamespace: namespace forObjCPrefix: prefix];
}

RCT_EXPORT_METHOD(registerMarshalByValueClass: (NSString*) className) {
    [_jsBridge registerMarshalByValueClass: className];
}

RCT_EXPORT_METHOD(getStaticProperty: (NSString*) type
                           property: (NSString*) property
                           resolver: (RCTPromiseResolveBlock) resolve
                           rejecter: (RCTPromiseRejectBlock) reject) {
    NSError* error;
    NSObject* result = [_jsBridge getStaticProperty: property
                                          onType: type
                                           error: &error];
    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
    }
    else {
        resolve(result);
    }
}

RCT_EXPORT_METHOD(setStaticProperty: (NSString*) type
                           property: (NSString*) property
                              value: (NSArray*) valueContainer
                           resolver: (RCTPromiseResolveBlock) resolve
                           rejecter: (RCTPromiseRejectBlock) reject) {
    NSError* error;
    if (valueContainer == nil || valueContainer.count != 1) {
        [C3PJavaScriptBridge setError: &error
                               toCode: ERROR_INVALID_ARGUMENT
                          withMessage: @"Missing or invalid value container argument."];
    }
    else {
        [_jsBridge setStaticProperty: property
                           onType: type
                          toValue: valueContainer[0]
                            error: &error];
    }

    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
    }
    else {
        resolve(nil);
    }
}


RCT_EXPORT_METHOD(invokeStaticMethod: (NSString*) type
                              method: (NSString*) method
                           arguments: (NSArray*) arguments
                            resolver: (RCTPromiseResolveBlock) resolve
                            rejecter: (RCTPromiseRejectBlock) reject) {
    [_jsBridge invokeStaticMethod: method
                        onType: type
                 withArguments: arguments
        result: ^void (NSObject* result) {
            resolve(result);
        }
        catch: ^void (NSError* error) {
            reject(@(error.code).stringValue, nil, error);
        }];
}

RCT_EXPORT_METHOD(addStaticEventListener: (NSString*) type
                                   event: (NSString*) event
                                resolver: (RCTPromiseResolveBlock) resolve
                                rejecter: (RCTPromiseRejectBlock) reject) {
    NSString* eventRegistrationToken = @(OSAtomicIncrement32(&_eventRegistrationId)).stringValue;
    __weak __typeof(self.bridge.eventDispatcher) weakEventDispatcher = self.bridge.eventDispatcher;
    C3PJavaScriptEventListener listener = ^void (NSDictionary* eventDictionary) {
        [weakEventDispatcher sendAppEventWithName: [NSString stringWithFormat: @"%@:%@", event, eventRegistrationToken]
                                             body: eventDictionary];
    };

    NSError* error;
    [_jsBridge addListenerForStaticEvent: event onType: type listener: listener error: &error];
    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
    }
    else {
        @synchronized(_eventListenerMap) {
            [_eventListenerMap setObject: listener forKey: eventRegistrationToken];
        }

        resolve(eventRegistrationToken);
    }
}

RCT_EXPORT_METHOD(removeStaticEventListener: (NSString*) type
                                      event: (NSString*) event
                                      token: (NSString*) registrationToken
                                   resolver: (RCTPromiseResolveBlock) resolve
                                   rejecter: (RCTPromiseRejectBlock) reject) {
    C3PJavaScriptEventListener listener = nil;
    @synchronized(_eventListenerMap) {
        [_eventListenerMap objectForKey: registrationToken];
    }

    if (listener == nil) {
        NSLog(@"C3PRReactModule: Event registration not found for token: %@", registrationToken);
        resolve(nil);
        return;
    }

    NSError* error;
    [_jsBridge removeListenerForStaticEvent: event
                                   onType: type
                                 listener: listener
                                    error: &error];
    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
        return;
    }

    resolve(nil);

    @synchronized(_eventListenerMap) {
        [_eventListenerMap removeObjectForKey: registrationToken];
    }
}

RCT_EXPORT_METHOD(createInstance: (NSString*) type
                       arguments: (NSArray*) arguments
                        resolver: (RCTPromiseResolveBlock) resolve
                        rejecter: (RCTPromiseRejectBlock) reject) {
    NSError* error;
    NSDictionary* instance = [_jsBridge createInstanceOfType: type
                                            withArguments: arguments
                                                    error: &error];
    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
    }
    else {
        resolve(instance);
    }
}

RCT_EXPORT_METHOD(releaseInstance: (NSDictionary*) instance
                         resolver: (RCTPromiseResolveBlock) resolve
                         rejecter: (RCTPromiseRejectBlock) reject) {
    NSError* error;
    [_jsBridge releaseInstance: instance error: &error];
    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
    }
    else {
        resolve(nil);
    }
}

RCT_EXPORT_METHOD(getProperty: (NSDictionary*) instance
                     property: (NSString*) property
                     resolver: (RCTPromiseResolveBlock) resolve
                     rejecter: (RCTPromiseRejectBlock) reject) {
    NSError* error;
    NSObject* result = [_jsBridge getProperty: property
                                onInstance: instance
                                     error: &error];
    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
    }
    else {
        resolve(result);
    }
}

RCT_EXPORT_METHOD(setProperty: (NSDictionary*) instance
                   onInstance: (NSString*) property
                        value: (NSArray*) valueContainer
                     resolver: (RCTPromiseResolveBlock) resolve
                     rejecter: (RCTPromiseRejectBlock) reject) {
    NSError* error;
    if (valueContainer == nil || valueContainer.count != 1) {
        [C3PJavaScriptBridge setError: &error
                               toCode: ERROR_INVALID_ARGUMENT
                          withMessage: @"Missing or invalid value container argument."];
    }
    else {
        [_jsBridge setProperty: property
                  onInstance: instance
                     toValue: valueContainer[0]
                       error: &error];
    }

    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
    }
    else {
        resolve(nil);
    }
}

RCT_EXPORT_METHOD(invokeMethod: (NSDictionary*) instance
                        method: (NSString*) method
                     arguments: (NSArray*) arguments
                      resolver: (RCTPromiseResolveBlock) resolve
                      rejecter: (RCTPromiseRejectBlock) reject) {
    [_jsBridge invokeMethod: method
              onInstance: instance
           withArguments: arguments
        result: ^void (NSObject* result) {
            resolve(result);
        }
        catch: ^void (NSError* error) {
            reject(@(error.code).stringValue, nil, error);
        }];
}

RCT_EXPORT_METHOD(addEventListener: (NSDictionary*) instance
                             event: (NSString*) event
                          resolver: (RCTPromiseResolveBlock) resolve
                          rejecter: (RCTPromiseRejectBlock) reject) {
    NSString* eventRegistrationToken = @(OSAtomicIncrement32(&_eventRegistrationId)).stringValue;
    __weak __typeof(self.bridge.eventDispatcher) weakEventDispatcher = self.bridge.eventDispatcher;
    C3PJavaScriptEventListener listener = ^void (NSDictionary* eventDictionary) {
        [weakEventDispatcher sendAppEventWithName: [NSString stringWithFormat: @"%@:%@", event, eventRegistrationToken]
                                             body: eventDictionary];
    };

    NSError* error;
    [_jsBridge addListenerForEvent: event onInstance: instance listener: listener error: &error];
    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
    }
    else {
        @synchronized(_eventListenerMap) {
            [_eventListenerMap setObject: listener forKey: eventRegistrationToken];
        }

        resolve(eventRegistrationToken);
    }
}

RCT_EXPORT_METHOD(removeEventListener: (NSDictionary*) instance
                                event: (NSString*) event
                                token: (NSString*) registrationToken
                             resolver: (RCTPromiseResolveBlock) resolve
                             rejecter: (RCTPromiseRejectBlock) reject) {
    C3PJavaScriptEventListener listener = nil;
    @synchronized(_eventListenerMap) {
        [_eventListenerMap objectForKey: registrationToken];
    }

    if (listener == nil) {
        NSLog(@"C3PRReactModule: Event registration not found for token: %@", registrationToken);
        resolve(nil);
        return;
    }

    NSError* error;
    [_jsBridge removeListenerForEvent: event
                         onInstance: instance
                           listener: listener
                              error: &error];
    if (error != nil) {
        reject(@(error.code).stringValue, nil, error);
        return;
    }

    resolve(nil);

    @synchronized(_eventListenerMap) {
        [_eventListenerMap removeObjectForKey: registrationToken];
    }
}

- (void) onPause {
    [_jsBridge onPause];
}

- (void) onResume {
    [_jsBridge onResume];
}

@end
