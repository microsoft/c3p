// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>

@class C3PJavaScriptMarshaller;

typedef void (^C3PJavaScriptEventListener)(NSDictionary* jsEvent);


/// Bridges Obj-C multicast events to JavaScript event handlers.
@interface C3PJavaScriptEventBridge : NSObject

@property (readonly, copy) C3PJavaScriptEventListener listener;
@property (readonly) Class sourceClass;
@property (readonly) NSObject* sourceInstance;
@property (readonly) NSString* eventName;

- (C3PJavaScriptEventBridge*) initWithListener: (C3PJavaScriptEventListener) listener
                                  forEventName: (NSString*) eventName
                                 onSourceClass: (Class) sourceClass
                              onSourceInstance: (NSObject*) sourceInstance
                                    marshaller: (C3PJavaScriptMarshaller*) marshaller
                                         error: (NSError**) outError;

- (void) addListener;
- (void) removeListener;

@end
