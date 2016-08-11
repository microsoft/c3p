// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>

@protocol C3PApplicationContext;
@class C3PNamespaceMapper;


/// Marshals parameters from and return values to the JavaScript bridge.
@interface C3PJavaScriptMarshaller : NSObject

@property (readonly) id<C3PApplicationContext> context;
@property (readonly) C3PNamespaceMapper* namespaceMapper;

- (C3PJavaScriptMarshaller*) initWithContext: (id<C3PApplicationContext>) context
                          andNamespaceMapper: (C3PNamespaceMapper*) namespaceMapper;

- (void) registerMarshalByValueClass: (NSString*) className;

- (NSObject*) marshalToJavaScript: (NSObject*) object;

- (NSObject*) marshalFromJavaScript: (NSObject*) jsObject
                              class: (Class) type;

- (void) releaseMarshalledObject: (NSDictionary*) jsObject
                           class: (Class) type;

@end
