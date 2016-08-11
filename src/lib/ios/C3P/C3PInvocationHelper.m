// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PInvocationHelper.h"
#import "C3PJavaScriptBridge.h"
#import "C3PJavaScriptMarshaller.h"
#import "C3PNamespaceMapper.h"
#import <objc/runtime.h>


@implementation C3PInvocationHelper

+ (NSInvocation*) getInvocationForClassInit: (Class) class
                                   instance: (NSObject*) instance
                                  arguments: (NSArray*) arguments
                       invocationAttributes: (C3PInvocationAttributes*) outInvocationAttributes
                                 marshaller: (C3PJavaScriptMarshaller*) marshaller
                                      error: (NSError**) outError {
    return [C3PInvocationHelper getInvocationForMethod: nil
                                                 class: class
                                              instance: instance
                                             arguments: arguments
                                  invocationAttributes: outInvocationAttributes
                                            marshaller: marshaller
                                                 error: outError];
}

+ (NSInvocation*) getInvocationForMethod: (NSString*) methodName
                                   class: (Class) class
                                instance: (NSObject*) instance
                               arguments: (NSArray*) arguments
                              marshaller: (C3PJavaScriptMarshaller*) marshaller
                                   error: (NSError**) outError {
    return [C3PInvocationHelper getInvocationForMethod: methodName
                                                 class: class
                                              instance: instance
                                             arguments: arguments
                                  invocationAttributes: nil
                                        marshaller: marshaller
                                                 error: outError];
}

+ (NSInvocation*) getInvocationForMethod: (NSString*) methodName
                                   class: (Class) class
                                instance: (NSObject*) instance
                               arguments: (NSArray*) arguments
                    invocationAttributes: (C3PInvocationAttributes*) outInvocationAttributes
                              marshaller: (C3PJavaScriptMarshaller*) marshaller
                                   error: (NSError**) outError {
    id target = (instance != nil ? instance : class);
    SEL selector = [self findMatchingMethod: methodName
                                     target: target
                                  arguments: arguments
                       invocationAttributes: outInvocationAttributes];

    BOOL memberExists = (selector != NULL && [target respondsToSelector: selector]);
    if (!memberExists) {
        if (methodName == nil)
        {
            [C3PJavaScriptBridge setError: outError
                        toCode: ERROR_MEMBER_NOT_FOUND
                   withMessage: @"Init method not found on class %@", NSStringFromClass(class)];
        }
        else
        {
            [C3PJavaScriptBridge setError: outError
                        toCode: ERROR_MEMBER_NOT_FOUND
                   withMessage: @"Method %@ not found on class %@", methodName, NSStringFromClass(class)];
        }
        return nil;
    }

    NSMethodSignature* signature = [target methodSignatureForSelector: selector];
    NSInvocation* invocation = [NSInvocation invocationWithMethodSignature: signature];
    [invocation retainArguments];
    [invocation setSelector: selector];
    [invocation setTarget: target];

    // [0] = self, [1] = cmd
    NSUInteger parameterCount = [signature numberOfArguments] -
        (outInvocationAttributes ? C3PInvocationSpecialParameterCount(*outInvocationAttributes) : 0);
    NSUInteger argumentCount = arguments.count;

    for (NSUInteger i = 2; i < parameterCount && i < argumentCount + 2; i++) {
        id argument = [arguments objectAtIndex: i - 2];
        BOOL matched = [C3PInvocationHelper convertInvocationArgument: argument
                                                           invocation: invocation
                                                                index: i
                                                            signature: signature
                                                           marshaller: marshaller
                                                                error: outError];
        if (!matched) {
            if (outError != nil && *outError == nil) {
                NSString* argumentType = NSStringFromClass([argument class]);
                const char* expectedType = [signature getArgumentTypeAtIndex: i];
                [C3PJavaScriptBridge setError: outError
                            toCode: ERROR_INVALID_ARGUMENT
                       withMessage: @"Argument %d (%@) is incorrect type for method %@. Expected type: %s",
                                    (int)(i - 2), argumentType, methodName, expectedType];
            }
            return nil;
        }
    }

    return invocation;
}

+ (BOOL) convertInvocationArgument: (id) argument
                        invocation: (NSInvocation*) invocation
                             index: (NSUInteger) index
                         signature: (NSMethodSignature*) signature
                        marshaller: (C3PJavaScriptMarshaller*) marshaller
                             error: (NSError**) outError {
    // Reference: "Objective-C Runtime Type Encodings"
    const char* encodedType = [signature getArgumentTypeAtIndex: index];
    char encodedTypeFirstChar = encodedType[0];

    if ([argument isKindOfClass: [NSNumber class]]) {
        NSNumber* number = argument;
        switch (encodedTypeFirstChar) {
            case _C_BOOL: {
                bool value = [number boolValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_CHR: {
                char value = [number charValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_SHT: {
                short value = [number shortValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_INT: {
                int value = [number intValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_LNG: {
                long value = [number longValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_LNG_LNG: {
                long long value = [number longLongValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_UCHR: {
                unsigned char value = [number unsignedCharValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_USHT: {
                ushort value = [number unsignedShortValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_UINT: {
                uint value = [number unsignedIntValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_ULNG: {
                unsigned long value = [number unsignedLongValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_ULNG_LNG: {
                unsigned long long value = [number unsignedLongLongValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_FLT: {
                float value = [number floatValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_DBL: {
                double value = [number doubleValue];
                [invocation setArgument: &value atIndex: index];
                return YES;
            }
            case _C_ID: {
                [invocation setArgument: &number atIndex: index];
                return YES;
            }
        }
    }
    else if ([argument isKindOfClass: [NSArray class]] &&
             encodedTypeFirstChar == _C_ID) {
        NSArray* array = argument;
        NSMutableArray* convertedArray = [[NSMutableArray alloc] initWithCapacity: array.count];
        for (NSUInteger i = 0; i < array.count; i++) {
            if ([array[i] isKindOfClass: [NSDictionary class]])
            {
                NSDictionary* argumentInstance = array[i];
                NSString* pluginClassFullName = [argumentInstance objectForKey: @"type"];
                if (pluginClassFullName == nil || pluginClassFullName.length == 0) {
                    [C3PJavaScriptBridge setError: outError
                                toCode: ERROR_INVALID_ARGUMENT
                           withMessage: @"Array item is missing required type field"];
                    return NO;
                }

                NSString* objCClassName = [marshaller.namespaceMapper getObjCClassForPluginClass: pluginClassFullName];
                if (objCClassName == nil) {
                    [C3PJavaScriptBridge setError: outError
                                toCode: ERROR_CLASS_NOT_FOUND
                           withMessage: @"Class mapping for %@ not found", pluginClassFullName];
                    return NO;
                }

                Class argumentClass = NSClassFromString(objCClassName);
                if (argumentClass == nil) {
                    [C3PJavaScriptBridge setError: outError
                                toCode: ERROR_CLASS_NOT_FOUND
                           withMessage: @"Class %@ not found", objCClassName];
                    return NO;
                }

                convertedArray[i] = [marshaller marshalFromJavaScript: array[i] class: argumentClass];
            }
            else
            {
                convertedArray[i] = array[i];
            }
        }
        [invocation setArgument: &convertedArray atIndex: index];
        return YES;
    }
    else if ([argument isKindOfClass: [NSDictionary class]] &&
             encodedTypeFirstChar == _C_ID) {
        NSDictionary* argumentInstance = argument;
        NSString* pluginClassFullName = [argumentInstance objectForKey: @"type"];
        if (pluginClassFullName == nil || pluginClassFullName.length == 0) {
            [C3PJavaScriptBridge setError: outError
                        toCode: ERROR_INVALID_ARGUMENT
                   withMessage: @"Instance argument is missing required type field"];
            return NO;
        }

        NSString* objCClassName = [marshaller.namespaceMapper getObjCClassForPluginClass: pluginClassFullName];

        if (objCClassName == nil) {
            [C3PJavaScriptBridge setError: outError
                        toCode: ERROR_CLASS_NOT_FOUND
                   withMessage: @"Class mapping for %@ not found", pluginClassFullName];
            return NO;
        }

        Class argumentClass = NSClassFromString(objCClassName);
        if (argumentClass == nil) {
            [C3PJavaScriptBridge setError: outError
                        toCode: ERROR_CLASS_NOT_FOUND
                   withMessage: @"Class %@ not found", objCClassName];
            return NO;
        }

        NSObject* argumentObject = [marshaller marshalFromJavaScript: argumentInstance class: argumentClass];
        [invocation setArgument: &argumentObject atIndex: index];
        return YES;
    }
    else if (argument == [NSNull null]) {
        // A null/undefined value from JavaScript is converted to the default value for any argument type
        // by not setting any value for the argument.
        return YES;
    }
    else if (encodedTypeFirstChar == _C_ID) {
        [invocation setArgument: &argument atIndex: index];
        return YES;
    }

    return NO;
}

+ (SEL) findMatchingMethod: (NSString*) methodName
                    target: (id) target
                 arguments: (NSArray*) arguments
      invocationAttributes: (C3PInvocationAttributes*) outInvocationAttributes {
    // Analyze the list of selectors on the class and pick the best one
    SEL selector = NULL;
    uint methodCount;
    Method* methodList = class_copyMethodList(object_getClass(target), &methodCount);
    for (uint i = 0; i < methodCount; i++) {
        Method method = methodList[i];
        SEL methodSelector = method_getName(method);
        NSString* methodSelectorString = NSStringFromSelector(methodSelector);

        // Subtract 2 for the implicit self and cmd parameters.
        uint methodParameterCount = method_getNumberOfArguments(method) - 2;

        NSString* matchMethodName = methodSelectorString;
        if (methodParameterCount > 0)
        {
            NSRange colonRange = [methodSelectorString rangeOfString: @":" options: NSLiteralSearch];
            matchMethodName = [methodSelectorString substringToIndex: colonRange.location];
        }

        if (outInvocationAttributes != nil) {
            // Check for out error or async callback parameters; if so ignore them when matching the method name and argument count.
            if (methodParameterCount == 1 && [methodSelectorString hasSuffix: @"WithError:"]) {
                // methodNameWithError:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 9];
                *outInvocationAttributes = C3PInvocationHasOutError;
            }
            else if (methodParameterCount == 1 && [methodSelectorString hasSuffix: @"Error:"]) {
                // methodNameError:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 5];
                *outInvocationAttributes = C3PInvocationHasOutError;
            }
            else if (methodParameterCount >= 2 && ([methodSelectorString hasSuffix: @":withError:"] ||
                                                   [methodSelectorString hasSuffix: @":error:"])) {
                // methodName:error:, methodName:parameter:withError:, ...
                *outInvocationAttributes = C3PInvocationHasOutError;
            }
            else if (methodParameterCount == 1 && [methodSelectorString hasSuffix: @"AndThen:"]) {
                // methodNameAndThen:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 7];
                *outInvocationAttributes = C3PInvocationHasThenCallback;
            }
            else if (methodParameterCount == 1 && [methodSelectorString hasSuffix: @"Then:"]) {
                // methodNameThen:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 4];
                *outInvocationAttributes = C3PInvocationHasThenCallback;
            }
            else if (methodParameterCount == 2 && [methodSelectorString hasSuffix: @"AndThen:catch:"]) {
                // methodNameAndThen:catch:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 7];
                *outInvocationAttributes = C3PInvocationHasThenCallback | C3PInvocationHasCatchCallback;
            }
            else if (methodParameterCount == 2 && [methodSelectorString hasSuffix: @"Then:catch:"]) {
                // methodNameThen:catch:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 4];
                *outInvocationAttributes = C3PInvocationHasThenCallback | C3PInvocationHasCatchCallback;
            }
            else if (methodParameterCount >= 2 && ([methodSelectorString hasSuffix: @":andThen:"] ||
                                                   [methodSelectorString hasSuffix: @":then:"])) {
                // methodName:then:, methodName:parameter:then:, ...
                *outInvocationAttributes = C3PInvocationHasThenCallback;
            }
            else if (methodParameterCount >= 3 && ([methodSelectorString hasSuffix: @":andThen:catch:"] ||
                                                   [methodSelectorString hasSuffix: @":then:catch:"])) {
                // methodName:then:catch:, methodName:parameter:andThen:catch:, ...
                *outInvocationAttributes = C3PInvocationHasThenCallback | C3PInvocationHasCatchCallback;
            }
            else if (methodParameterCount == 1 && [methodSelectorString hasSuffix: @"WithResult:"]) {
                // methodNameWithResult:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 10];
                *outInvocationAttributes = C3PInvocationHasResultCallback;
            }
            else if (methodParameterCount == 1 && [methodSelectorString hasSuffix: @"Result:"]) {
                // methodNameResult:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 6];
                *outInvocationAttributes = C3PInvocationHasResultCallback;
            }
            else if (methodParameterCount == 2 && [methodSelectorString hasSuffix: @"WithResult:catch:"]) {
                // methodNameWithResult:catch:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 10];
                *outInvocationAttributes = C3PInvocationHasResultCallback | C3PInvocationHasCatchCallback;
            }
            else if (methodParameterCount == 2 && [methodSelectorString hasSuffix: @"Result:catch:"]) {
                // methodNameResult:catch:
                matchMethodName = [matchMethodName substringToIndex: matchMethodName.length - 6];
                *outInvocationAttributes = C3PInvocationHasResultCallback | C3PInvocationHasCatchCallback;
            }
            else if (methodParameterCount >= 2 && ([methodSelectorString hasSuffix: @":withResult:"] ||
                                                   [methodSelectorString hasSuffix: @":result:"])) {
                // methodName:result:, methodName:parameter:withResult:, ...
                *outInvocationAttributes = C3PInvocationHasResultCallback;
            }
            else if (methodParameterCount >= 3 && ([methodSelectorString hasSuffix: @":withResult:catch:"] ||
                                                   [methodSelectorString hasSuffix: @":result:catch:"])) {
                // methodName:result:catch:, methodName:parameter:withResult:catch:, ...
                *outInvocationAttributes = C3PInvocationHasResultCallback | C3PInvocationHasCatchCallback;
            }
            else {
                *outInvocationAttributes = C3PInvocationNormal;
            }

            methodParameterCount -= C3PInvocationSpecialParameterCount(*outInvocationAttributes);
        }

        if (methodParameterCount == arguments.count &&
            ((methodName != nil && [matchMethodName caseInsensitiveCompare: methodName] == NSOrderedSame) ||
             (methodName == nil && [matchMethodName hasPrefix: @"init"]))) {

            // TODO: Incorporate parameter types in overload resolution using method_getArgumentType.
            // Currently this resolution is only based on the method name and parameter count.

            selector = methodSelector;
            break;
        }
    }
    free(methodList);

    return selector;
}

+ (NSObject*) convertReturnValueFromInvocation: (NSInvocation*) invocation
                                    marshaller: (C3PJavaScriptMarshaller*) marshaller
                                         error: (NSError**) outError {
    const char* encodedType = [invocation.methodSignature methodReturnType];
    char encodedTypeFirstChar = encodedType[0];

    switch (encodedTypeFirstChar) {
        case _C_VOID: {
            return nil;
        }
        case _C_ID: {
            __unsafe_unretained id value;
            [invocation getReturnValue: &value];
            return [marshaller marshalToJavaScript: value];
        }
        case _C_BOOL: {
            BOOL value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithBool: value];
        }
        case _C_CHR: {
            char value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithChar: value];
        }
        case _C_SHT: {
            short value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithShort: value];
        }
        case _C_INT: {
            int value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithInt: value];
        }
        case _C_LNG: {
            long value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithLong: value];
        }
        case _C_LNG_LNG: {
            long long value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithLongLong: value];
        }
        case _C_UCHR: {
            unsigned char value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithUnsignedChar: value];
        }
        case _C_USHT: {
            ushort value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithUnsignedShort: value];
        }
        case _C_UINT: {
            uint value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithUnsignedInt: value];
        }
        case _C_ULNG: {
            unsigned long value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithUnsignedLong: value];
        }
        case _C_ULNG_LNG: {
            unsigned long long value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithUnsignedLongLong: value];
        }
        case _C_FLT: {
            float value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithFloat: value];
        }
        case _C_DBL: {
            double value;
            [invocation getReturnValue: &value];
            return [NSNumber numberWithDouble: value];
        }
        default: {
            [C3PJavaScriptBridge setError: outError
                        toCode: ERROR_INVALID_ARGUMENT
                   withMessage: @"Failed to convert return value of type: %s", encodedType];
            return nil;
        }
    }
}

@end
