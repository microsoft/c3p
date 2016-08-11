// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>
#import <Cordova/CDVPlugin.h>
#import "C3PApplicationContext.h"


/// A Cordova plugin that enables other Cordova plugins to easily bridge between
/// JavaScript and Obj-C code.
@interface C3PCCordovaPlugin : CDVPlugin <C3PApplicationContext>

- (void) getStaticProperty: (CDVInvokedUrlCommand*) command;
- (void) setStaticProperty: (CDVInvokedUrlCommand*) command;
- (void) invokeStaticMethod: (CDVInvokedUrlCommand*) command;
- (void) addStaticEventListener: (CDVInvokedUrlCommand*) command;
- (void) removeStaticEventListener: (CDVInvokedUrlCommand*) command;
- (void) createInstance: (CDVInvokedUrlCommand*) command;
- (void) releaseInstance: (CDVInvokedUrlCommand*) command;
- (void) getProperty: (CDVInvokedUrlCommand*) command;
- (void) setProperty: (CDVInvokedUrlCommand*) command;
- (void) invokeMethod: (CDVInvokedUrlCommand*) command;
- (void) addEventListener: (CDVInvokedUrlCommand*) command;
- (void) removeEventListener: (CDVInvokedUrlCommand*) command;

@end
