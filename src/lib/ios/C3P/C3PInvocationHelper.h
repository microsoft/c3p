// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>

@class C3PJavaScriptMarshaller;


typedef NS_ENUM(NSUInteger, C3PInvocationAttributes) {
    C3PInvocationNormal = 0,
    C3PInvocationHasOutError = 1 << 0,
    C3PInvocationHasThenCallback = 1 << 1,
    C3PInvocationHasResultCallback = 1 << 2,
    C3PInvocationHasCatchCallback = 1 << 3,
};

#define C3PInvocationSpecialParameterCount(attrs) (\
    ((attrs & C3PInvocationHasOutError) ? 1 : 0) + \
    ((attrs & C3PInvocationHasThenCallback) ? 1 : 0) + \
    ((attrs & C3PInvocationHasResultCallback) ? 1 : 0) + \
    ((attrs & C3PInvocationHasCatchCallback) ? 1 : 0) )


@interface C3PInvocationHelper : NSObject

+ (NSInvocation*) getInvocationForClassInit: (Class) class
                                   instance: (NSObject*) instance
                                  arguments: (NSArray*) arguments
                       invocationAttributes: (C3PInvocationAttributes*) outInvocationAttributes
                                 marshaller: (C3PJavaScriptMarshaller*) marshaller
                                      error: (NSError**) outError;

+ (NSInvocation*) getInvocationForMethod: (NSString*) methodName
                                   class: (Class) class
                                instance: (NSObject*) instance
                               arguments: (NSArray*) arguments
                              marshaller: (C3PJavaScriptMarshaller*) marshaller
                                   error: (NSError**) outError;

+ (NSInvocation*) getInvocationForMethod: (NSString*) methodName
                                   class: (Class) class
                                instance: (NSObject*) instance
                               arguments: (NSArray*) arguments
                    invocationAttributes: (C3PInvocationAttributes*) outInvocationAttributes
                              marshaller: (C3PJavaScriptMarshaller*) marshaller
                                   error: (NSError**) outError;

+ (NSObject*) convertReturnValueFromInvocation: (NSInvocation*) invocation
                                    marshaller: (C3PJavaScriptMarshaller*) marshaller
                                         error: (NSError**) outError;

@end
