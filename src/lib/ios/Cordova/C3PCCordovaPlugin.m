// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PCCordovaPlugin.h"
#import "C3PCConfigParser.h"
#import "C3PJavaScriptBridge.h"
#import "C3PNamespaceMapper.h"

@implementation C3PCCordovaPlugin {
    C3PJavaScriptBridge* _bridge;
    NSMutableDictionary<NSString*, C3PJavaScriptEventListener>* _eventListenerMap;
}

- (void) pluginInitialize {
    _bridge = [[C3PJavaScriptBridge alloc] initWithContext: self];
    [self loadNamespaceMappingsFromConfig];
    _eventListenerMap = [[NSMutableDictionary<NSString*, C3PJavaScriptEventListener> alloc] initWithCapacity: 10];

    [[NSNotificationCenter defaultCenter] addObserver: self
                                             selector: @selector(onPause)
                                                 name: UIApplicationDidEnterBackgroundNotification
                                               object: nil];
    [[NSNotificationCenter defaultCenter] addObserver: self
                                             selector: @selector(onResume)
                                                 name: UIApplicationWillEnterForegroundNotification
                                               object: nil];
}

- (UIApplication*) getApplication {
    return [UIApplication sharedApplication];
}

- (UIWindow*) getCurrentWindow {
    return [[UIApplication sharedApplication] keyWindow];
}

- (void) getStaticProperty: (CDVInvokedUrlCommand*) command {
    NSString* type = [command argumentAtIndex: 0 withDefault: nil andClass: [NSString class]];
    NSString* property = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];

    if (type == nil || property == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        NSError* error;
        NSObject* result = [_bridge getStaticProperty: property
                                              onType: type
                                               error: &error];
        if (error != nil) {
            [self sendError: error callbackId: command.callbackId];
        }
        else {
            [self sendResult: result callbackId: command.callbackId];
        }
    }
}

- (void) setStaticProperty: (CDVInvokedUrlCommand*) command {
    NSString* type = [command argumentAtIndex: 0 withDefault: nil andClass: [NSString class]];
    NSString* property = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];
    NSObject* value = [command argumentAtIndex: 2];

    if (type == nil || property == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        NSError* error;
        [_bridge setStaticProperty: property onType: type toValue: value error: &error];
        if (error != nil) {
            [self sendError: error callbackId: command.callbackId];
        }
        else {
            [self sendResult: nil callbackId: command.callbackId];
        }
    }
}

- (void) invokeStaticMethod: (CDVInvokedUrlCommand*) command {
    NSString* type = [command argumentAtIndex: 0 withDefault: nil andClass: [NSString class]];
    NSString* method = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];
    NSArray* arguments = [command argumentAtIndex: 2 withDefault: nil andClass: [NSArray class]];

    if (type == nil || method == nil || arguments == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        __weak __typeof(self) weakSelf = self;
        [_bridge invokeStaticMethod: method
                            onType: type
                     withArguments: arguments
            result: ^void (NSObject* result) {
                [weakSelf sendResult: result callbackId: command.callbackId];
            }
            catch: ^void (NSError* error) {
                [weakSelf sendError: error callbackId: command.callbackId];
            }];
    }
}


- (void) addStaticEventListener: (CDVInvokedUrlCommand*) command {
    NSString* type = [command argumentAtIndex: 0 withDefault: nil andClass: [NSString class]];
    NSString* event = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];

    if (type == nil || event == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        __weak __typeof(self) weakSelf = self;
        NSString* callbackId = command.callbackId;
        C3PJavaScriptEventListener listener = ^void (NSDictionary* eventDictionary) {
            [weakSelf sendResult: eventDictionary callbackId: callbackId keepCallback: YES];
        };

        NSError* error;
        [_bridge addListenerForStaticEvent: event onType: type listener: listener error: &error];
        if (error != nil) {
            [self sendError: error callbackId: callbackId];
        }
        else {
            @synchronized(_eventListenerMap) {
                [_eventListenerMap setObject: listener forKey: callbackId];
            }

            [self sendResult: callbackId callbackId: callbackId keepCallback: YES];
        }
    }
}


- (void) removeStaticEventListener: (CDVInvokedUrlCommand*) command {
    NSString* type = [command argumentAtIndex: 0 withDefault: nil andClass: [NSString class]];
    NSString* event = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];
    NSString* registrationToken = [command argumentAtIndex: 2 withDefault: nil andClass: [NSString class]];

    if (type == nil || event == nil || registrationToken == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        C3PJavaScriptEventListener listener;
        @synchronized(_eventListenerMap) {
            listener = [_eventListenerMap objectForKey: registrationToken];
        }

        if (listener == nil) {
            NSLog(@"C3PCCordovaPlugin: Event registration not found for callbackId: %@", registrationToken);
            [self sendResult: nil callbackId: command.callbackId];
            return;
        }

        NSError* error;
        [_bridge removeListenerForStaticEvent: event
                                      onType: type
                                    listener: listener
                                       error: &error];
        if (error != nil) {
            [self sendError: error callbackId: command.callbackId];
            return;
        }

        [self sendResult: nil callbackId: command.callbackId];

        @synchronized(_eventListenerMap) {
            [_eventListenerMap removeObjectForKey: registrationToken];
        }
    }
}


- (void) createInstance: (CDVInvokedUrlCommand*) command {
    NSString* type = [command argumentAtIndex: 0 withDefault: nil andClass: [NSString class]];
    NSArray* arguments = [command argumentAtIndex: 1 withDefault: nil andClass: [NSArray class]];

    if (type == nil || arguments == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        NSError* error;
        NSDictionary* result = [_bridge createInstanceOfType: type withArguments: arguments error: &error];
        if (error != nil) {
            [self sendError: error callbackId: command.callbackId];
        }
        else {
            [self sendResult: result callbackId: command.callbackId];
        }
    }
}

- (void) releaseInstance: (CDVInvokedUrlCommand*) command {
    NSDictionary* instance = [command argumentAtIndex: 0 withDefault: nil andClass: [NSDictionary class]];

    if (instance == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        NSError* error;
        [_bridge releaseInstance: instance error: &error];
        if (error != nil) {
            [self sendError: error callbackId: command.callbackId];
        }
        else {
            [self sendResult: nil callbackId: command.callbackId];
        }
    }
}

- (void) getProperty: (CDVInvokedUrlCommand*) command {
    NSDictionary* instance = [command argumentAtIndex: 0 withDefault: nil andClass: [NSDictionary class]];
    NSString* property = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];

    if (instance == nil || property == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        NSError* error;
        NSObject* result = [_bridge getProperty: property
                                    onInstance: instance
                                         error: &error];
        if (error != nil) {
            [self sendError: error callbackId: command.callbackId];
        }
        else {
            [self sendResult: result callbackId: command.callbackId];
        }
    }
}


- (void) setProperty: (CDVInvokedUrlCommand*) command {
    NSDictionary* instance = [command argumentAtIndex: 0 withDefault: nil andClass: [NSDictionary class]];
    NSString* property = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];
    NSObject* value = [command argumentAtIndex: 2];

    if (instance == nil || property == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        NSError* error;
        [_bridge setProperty: property onInstance: instance toValue: value error: &error];
        if (error != nil) {
            [self sendError: error callbackId: command.callbackId];
        }
        else {
            [self sendResult: nil callbackId: command.callbackId];
        }
    }
}


- (void) invokeMethod: (CDVInvokedUrlCommand*) command {
    NSDictionary* instance = [command argumentAtIndex: 0 withDefault: nil andClass: [NSDictionary class]];
    NSString* method = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];
    NSArray* arguments = [command argumentAtIndex: 2 withDefault: nil andClass: [NSArray class]];

    if (instance == nil || method == nil || arguments == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        __weak __typeof(self) weakSelf = self;
        [_bridge invokeMethod: method
                  onInstance: instance
               withArguments: arguments
            result: ^void (NSObject* result) {
                [weakSelf sendResult: result callbackId: command.callbackId];
            }
            catch: ^void (NSError* error) {
                [weakSelf sendError: error callbackId: command.callbackId];
            }];
    }
}

- (void) addEventListener: (CDVInvokedUrlCommand*) command {
    NSDictionary* instance = [command argumentAtIndex: 0 withDefault: nil andClass: [NSDictionary class]];
    NSString* event = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];

    if (instance == nil || event == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        __weak __typeof(self) weakSelf = self;
        NSString* callbackId = command.callbackId;
        C3PJavaScriptEventListener listener = ^void (NSDictionary* eventDictionary) {
            [weakSelf sendResult: eventDictionary callbackId: callbackId keepCallback: YES];
        };

        NSError* error;
        [_bridge addListenerForEvent: event onInstance: instance listener: listener error: &error];
        if (error != nil) {
            [self sendError: error callbackId: callbackId];
        }
        else {
            @synchronized(_eventListenerMap) {
                [_eventListenerMap setObject: listener forKey: callbackId];
            }

            [self sendResult: callbackId callbackId: callbackId keepCallback: YES];
        }
    }
}


- (void) removeEventListener: (CDVInvokedUrlCommand*) command {
    NSDictionary* instance = [command argumentAtIndex: 0 withDefault: nil andClass: [NSDictionary class]];
    NSString* event = [command argumentAtIndex: 1 withDefault: nil andClass: [NSString class]];
    NSString* registrationToken = [command argumentAtIndex: 2 withDefault: nil andClass: [NSString class]];

    if (instance == nil || event == nil || registrationToken == nil) {
        [self sendStatus: CDVCommandStatus_ERROR callbackId: command.callbackId];
    } else {
        C3PJavaScriptEventListener listener;
        @synchronized(_eventListenerMap) {
            listener = [_eventListenerMap objectForKey: registrationToken];
        }

        if (listener == nil) {
            NSLog(@"C3PCCordovaPlutin: Event registration not found for callbackId: %@", registrationToken);
            [self sendResult: nil callbackId: command.callbackId];
            return;
        }

        NSError* error;
        [_bridge removeListenerForEvent: event onInstance: instance listener: listener error: &error];
        if (error != nil) {
            [self sendError: error callbackId: command.callbackId];
            return;
        }

        [self sendResult: nil callbackId: command.callbackId];

        @synchronized(_eventListenerMap) {
            [_eventListenerMap removeObjectForKey: registrationToken];
        }
    }
}

- (void) sendResult: (id) result callbackId: (NSString*) callbackId {
    [self sendResult: result callbackId: callbackId keepCallback: NO];
}

- (void) sendResult: (id) result callbackId: (NSString*) callbackId keepCallback: (BOOL) keepCallback {
    CDVPluginResult* pluginResult;

    if (result == nil || result == [NSNull null]) {
        pluginResult = [CDVPluginResult resultWithStatus: CDVCommandStatus_OK];
    } else if ([result isKindOfClass:[NSString class]]) {
        pluginResult = [CDVPluginResult resultWithStatus: CDVCommandStatus_OK messageAsString: result];
    } else if ([result isKindOfClass:[NSNumber class]]) {
        pluginResult = [CDVPluginResult resultWithStatus: CDVCommandStatus_OK
                                         messageAsDouble: ((NSNumber*)result).doubleValue];
    } else if ([result isKindOfClass:[NSArray class]]) {
        pluginResult = [CDVPluginResult resultWithStatus: CDVCommandStatus_OK messageAsArray: result];
    } else if ([result isKindOfClass:[NSDictionary class]]) {
        pluginResult = [CDVPluginResult resultWithStatus: CDVCommandStatus_OK messageAsDictionary: result];
    } else {
        pluginResult = [CDVPluginResult resultWithStatus: CDVCommandStatus_OK];
    }

    if (keepCallback) {
        pluginResult.keepCallback = [NSNumber numberWithBool: YES];
    }
    [self.commandDelegate sendPluginResult: pluginResult callbackId: callbackId];
}

- (void) sendError: (NSError*) error callbackId: (NSString*) callbackId {
    NSDictionary* errorDict = @{
        @"code" : [NSNumber numberWithInt: (int)error.code],
        @"message" : error.localizedDescription
    };
    CDVPluginResult* pluginResult = [CDVPluginResult resultWithStatus: CDVCommandStatus_ERROR messageAsDictionary: errorDict];
    [self.commandDelegate sendPluginResult: pluginResult callbackId: callbackId];
}

- (void) sendStatus: (CDVCommandStatus) status callbackId: (NSString*) callbackId {
    CDVPluginResult* pluginResult = [CDVPluginResult resultWithStatus: status];
    [self.commandDelegate sendPluginResult: pluginResult callbackId: callbackId];
}

- (void) loadNamespaceMappingsFromConfig {
    NSString* configFilePath = [[NSBundle mainBundle] pathForResource: @"config.xml" ofType: nil];
    NSURL* configUrl = [NSURL fileURLWithPath: configFilePath];
    NSXMLParser* configParser = [[NSXMLParser alloc] initWithContentsOfURL: configUrl];
    if (configParser == nil) {
        NSLog(@"Failed to initialize config XML parser.");
        return;
    }

    C3PCConfigParser* cordovaParser = [[C3PCConfigParser alloc] init];
    [configParser setDelegate: cordovaParser];
    [configParser parse];

    for (NSString* prefix in cordovaParser.prefixMappings) {
        NSString* pluginNamespace = cordovaParser.prefixMappings[prefix];
        [_bridge.namespaceMapper registerPluginNamespace: pluginNamespace forObjCPrefix: prefix];
    }

    for (NSString* marshalByValueClass in cordovaParser.marshalByValueClasses) {
        [_bridge registerMarshalByValueClass: marshalByValueClass];
    }
}

- (void) onPause {
    [_bridge onPause];
}

- (void) onResume {
    [_bridge onResume];
}

@end
