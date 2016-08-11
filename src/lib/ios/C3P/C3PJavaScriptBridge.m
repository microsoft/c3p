// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PJavaScriptBridge.h"
#import "C3PJavaScriptMarshaller.h"
#import "C3PInvocationHelper.h"
#import "C3PJavaScriptEventBridge.h"
#import "C3PNamespaceMapper.h"


@implementation C3PJavaScriptBridge {
    id<C3PApplicationContext> _context;
    C3PNamespaceMapper* _namespaceMapper;
    C3PJavaScriptMarshaller* _marshaller;
    NSMutableArray<C3PJavaScriptEventBridge*>* _eventBridges;
}

@synthesize namespaceMapper = _namespaceMapper;

- (C3PJavaScriptBridge*) initWithContext: (id<C3PApplicationContext>) context {
    self = [super init];
    if (self)
    {
        _context = context;
        _namespaceMapper = [[C3PNamespaceMapper alloc] init];
        _marshaller = [[C3PJavaScriptMarshaller alloc] initWithContext: context andNamespaceMapper: _namespaceMapper];
        _eventBridges = [[NSMutableArray alloc] initWithCapacity: 10];
    }
    return self;
}


- (void) registerMarshalByValueClass: (NSString*) className {
    [_marshaller registerMarshalByValueClass: className];
}

- (NSObject*) getStaticProperty: (NSString*) property
                         onType: (NSString*) type
                          error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateMemberArgument: property error: outError]) {
        return nil;
    }

    Class class = [self validateTypeArgument: type error: outError];
    if (class == nil) {
        return nil;
    }

    NSArray* arguments = [[NSArray alloc] init];

    NSInvocation* invocation = [C3PInvocationHelper getInvocationForMethod: property
                                                                     class: class
                                                                  instance: nil
                                                                 arguments: arguments
                                                            marshaller: _marshaller
                                                                     error: outError];

    if (invocation == nil) {
        if (outError) {
            *outError = nil;
        }

        invocation = [C3PInvocationHelper getInvocationForMethod: [@"is" stringByAppendingString: property]
                                                           class: class
                                                        instance: nil
                                                       arguments: arguments
                                                  marshaller: _marshaller
                                                           error: outError];
        if (invocation == nil) {
            return nil;
        }
    }

    [invocation invoke];

    NSObject* returnValue = [C3PInvocationHelper convertReturnValueFromInvocation: invocation
                                                                   marshaller: _marshaller
                                                                            error: outError];
    return returnValue;
}

- (void) setStaticProperty: (NSString*) property
                    onType: (NSString*) type
                   toValue: (NSObject*) value
                     error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateMemberArgument: property error: outError]) {
        return;
    }

    Class class = [self validateTypeArgument: type error: outError];
    if (class == nil) {
        return;
    }

    NSArray* arguments = [[NSArray alloc] initWithObjects: (value != nil ? value : [NSNull null]), nil];
    NSString* method = [@"set" stringByAppendingString: property];

    NSInvocation* invocation = [C3PInvocationHelper getInvocationForMethod: method
                                                                     class: class
                                                                  instance: nil
                                                                 arguments: arguments
                                                            marshaller: _marshaller
                                                                     error: outError];

    if (invocation != nil) {
        [invocation invoke];
    }
}

- (void) invokeStaticMethod: (NSString*) method
                     onType: (NSString*) type
              withArguments: (NSArray*) arguments
                     result: (void(^)(NSObject*)) success
                      catch: (void(^)(NSError*)) failure {
    NSError* error;
    if (![C3PJavaScriptBridge validateMemberArgument: method error: &error]) {
        if (failure) {
            failure(error);
        }
    }

    Class class = [self validateTypeArgument: type error: &error];
    if (class == nil) {
        if (failure) {
            failure(error);
        }
        return;
    }

    if (![C3PJavaScriptBridge validateArgumentsArray: arguments error: &error]) {
        if (failure) {
            failure(error);
        }
        return;
    }

    C3PInvocationAttributes invocationAttributes;
    NSInvocation* invocation = [C3PInvocationHelper getInvocationForMethod: method
                                                                     class: class
                                                                  instance: nil
                                                                 arguments: arguments
                                                      invocationAttributes: &invocationAttributes
                                                            marshaller: _marshaller
                                                                     error: &error];
    if (invocation == nil) {
        if (failure) {
            failure(error);
        }
        return;
    }

    [self invoke: invocation
        invocationAttributes: invocationAttributes
                      result: success
                       catch: failure];
}

- (void) addListenerForStaticEvent: (NSString*) event
                            onType: (NSString*) type
                          listener: (C3PJavaScriptEventListener) listener
                             error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateMemberArgument: event error: outError]) {
        return;
    }

    Class class = [self validateTypeArgument: type error: outError];
    if (class == nil) {
        return;
    }

    C3PJavaScriptEventBridge* eventBridge =
        [[C3PJavaScriptEventBridge alloc] initWithListener: listener
                                           forEventName: event
                                          onSourceClass: class
                                       onSourceInstance: nil
                                             marshaller: _marshaller
                                                  error: outError];
    if (eventBridge == nil) {
        return;
    }

    [eventBridge addListener];

    @synchronized(_eventBridges) {
        [_eventBridges addObject: eventBridge];
    }
}

- (void) removeListenerForStaticEvent: (NSString*) event
                               onType: (NSString*) type
                             listener: (C3PJavaScriptEventListener) listener
                                error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateMemberArgument: event error: outError]) {
        return;
    }

    Class class = [self validateTypeArgument: type error: outError];
    if (class == nil) {
        return;
    }

    C3PJavaScriptEventBridge* eventBridge = nil;
    @synchronized(_eventBridges) {
        for (C3PJavaScriptEventBridge* p in _eventBridges) {
            if (p.sourceClass == class && p.sourceInstance == nil &&
                [p.eventName isEqualToString: event] && p.listener == listener) {
                eventBridge = p;
                break;
            }
        }
    }

    if (eventBridge != nil) {
        [eventBridge removeListener];

        @synchronized(_eventBridges) {
            [_eventBridges removeObject: eventBridge];
        }
    }
    else {
        NSLog(@"C3PJavaScriptBridge: Event listener not found to remove: %@.%@", NSStringFromClass(class), event);
    }
}

- (NSDictionary*) createInstanceOfType: (NSString*) type
                         withArguments: (NSArray*) arguments
                                 error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateArgumentsArray: arguments error: outError]) {
        return nil;
    }

    Class class = [self validateTypeArgument: type error: outError];
    if (class == nil) {
        return nil;
    }

    NSObject* instanceObject = [class alloc];
    C3PInvocationAttributes invocationAttributes;
    NSInvocation* invocation = [C3PInvocationHelper getInvocationForClassInit: class
                                                                     instance: instanceObject
                                                                    arguments: arguments
                                                         invocationAttributes: &invocationAttributes
                                                               marshaller: _marshaller
                                                                        error: outError];
    if (invocation == nil) {
        return nil;
    }

    __block NSDictionary* instance = nil;
    [self invoke: invocation invocationAttributes: invocationAttributes
          result: ^void (NSObject* result) {
              instance = (id)result;
          }
          catch: ^void (NSError* error) {
              *outError = error;
          }];
    return instance;
}

- (void) releaseInstance: (NSDictionary*) instance
                   error: (NSError**) outError {
    if (instance == nil) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_INVALID_ARGUMENT
               withMessage: @"Missing required instance parameter."];
        return;
    }

    NSString* pluginClassFullName = [instance objectForKey: @"type"];
    if (pluginClassFullName == nil || pluginClassFullName.length == 0) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_INVALID_ARGUMENT
               withMessage: @"Instance parameter is missing required type field."];
        return;
    }

    Class class = [self validateTypeArgument: pluginClassFullName error: outError];
    if (class == nil) {
        return;
    }

    [_marshaller releaseMarshalledObject: instance class: class];
}

- (NSObject*) getProperty: (NSString*) property
               onInstance: (NSDictionary*) instance
                    error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateMemberArgument: property error: outError]) {
        return nil;
    }

    NSObject* instanceObject = [self validateInstanceArgument: instance error: outError];
    if (instanceObject == nil) {
        return nil;
    }

    NSArray* arguments = [[NSArray alloc] init];

    NSInvocation* invocation = [C3PInvocationHelper getInvocationForMethod: property
                                                                     class: [instanceObject class]
                                                                  instance: instanceObject
                                                                 arguments: arguments
                                                            marshaller: _marshaller
                                                                     error: outError];

    if (invocation == nil) {
        if (outError) {
            *outError = nil;
        }

        invocation = [C3PInvocationHelper getInvocationForMethod: [@"is" stringByAppendingString: property]
                                                           class: [instanceObject class]
                                                        instance: nil
                                                       arguments: arguments
                                                  marshaller: _marshaller
                                                           error: outError];
        if (invocation == nil) {
            return nil;
        }
    }

    [invocation invoke];

    NSObject* returnValue = [C3PInvocationHelper convertReturnValueFromInvocation: invocation
                                                                   marshaller: _marshaller
                                                                            error: outError];
    return returnValue;
}

- (void) setProperty: (NSString*) property
          onInstance: (NSDictionary*) instance
             toValue: (NSObject*) value
               error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateMemberArgument: property error: outError]) {
        return;
    }

    NSObject* instanceObject = [self validateInstanceArgument: instance error: outError];
    if (instanceObject == nil) {
        return;
    }

    NSArray* arguments = [[NSArray alloc] initWithObjects: (value != nil ? value : [NSNull null]), nil];
    NSString* method = [@"set" stringByAppendingString: property];

    NSInvocation* invocation = [C3PInvocationHelper getInvocationForMethod: method
                                                                     class: [instanceObject class]
                                                                  instance: instanceObject
                                                                 arguments: arguments
                                                            marshaller: _marshaller
                                                                     error: outError];

    if (invocation != nil) {
        [invocation invoke];
    }
}

- (void) invokeMethod: (NSString*) method
           onInstance: (NSDictionary*) instance
        withArguments: (NSArray*) arguments
               result: (void(^)(NSObject*)) success
                catch: (void(^)(NSError*)) failure {
    NSError* __autoreleasing error;
    if (![C3PJavaScriptBridge validateMemberArgument: method error: &error]) {
        if (failure) {
            failure(error);
        }
    }

    NSObject* instanceObject = [self validateInstanceArgument: instance error: &error];
    if (instanceObject == nil) {
        return;
    }

    if (![C3PJavaScriptBridge validateArgumentsArray: arguments error: &error]) {
        if (failure) {
            failure(error);
        }
        return;
    }

    C3PInvocationAttributes invocationAttributes;
    NSInvocation* invocation = [C3PInvocationHelper getInvocationForMethod: method
                                                                     class: [instanceObject class]
                                                                  instance: instanceObject
                                                                 arguments: arguments
                                                      invocationAttributes: &invocationAttributes
                                                            marshaller: _marshaller
                                                                     error: &error];
    if (invocation == nil) {
        if (failure) {
            failure(error);
        }
        return;
    }

    [self invoke: invocation
        invocationAttributes: invocationAttributes
                      result: success
                       catch: failure];
}

- (void) addListenerForEvent: (NSString*) event
                  onInstance: (NSDictionary*) instance
                    listener: (C3PJavaScriptEventListener) listener
                       error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateMemberArgument: event error: outError]) {
        return;
    }

    NSObject* instanceObject = [self validateInstanceArgument: instance error: outError];
    if (instanceObject == nil) {
        return;
    }

    C3PJavaScriptEventBridge* eventBridge =
        [[C3PJavaScriptEventBridge alloc] initWithListener: listener
                                              forEventName: event
                                             onSourceClass: [instanceObject class]
                                          onSourceInstance: instanceObject
                                                marshaller: _marshaller
                                                     error: outError];
    if (eventBridge == nil) {
        return;
    }

    [eventBridge addListener];

    @synchronized(_eventBridges) {
        [_eventBridges addObject: eventBridge];
    }
}

- (void) removeListenerForEvent: (NSString*) event
                     onInstance: (NSDictionary*) instance
                       listener: (C3PJavaScriptEventListener) listener
                          error: (NSError**) outError {
    if (![C3PJavaScriptBridge validateMemberArgument: event error: outError]) {
        return;
    }

    NSObject* instanceObject = [self validateInstanceArgument: instance error: outError];
    if (instanceObject == nil) {
        return;
    }

    Class class = [instanceObject class];
    C3PJavaScriptEventBridge* eventBridge = nil;
    @synchronized(_eventBridges) {
        for (C3PJavaScriptEventBridge* p in _eventBridges) {
            if (p.sourceClass == class && p.sourceInstance == instanceObject &&
                [p.eventName isEqualToString: event] && p.listener == listener) {
                eventBridge = p;
                break;
            }
        }
    }

    if (eventBridge != nil) {
        [eventBridge removeListener];

        @synchronized(_eventBridges) {
            [_eventBridges removeObject: eventBridge];
        }
    }
    else {
        NSLog(@"C3PJavaScriptBridge: Event listener not found to remove: %@.%@", NSStringFromClass(class), event);
    }
}

- (void) invoke: (NSInvocation*) invocation
    invocationAttributes: (C3PInvocationAttributes) invocationAttributes
                  result: (void(^)(NSObject*)) success
                   catch: (void(^)(NSError*)) failure {

    if (invocationAttributes & (C3PInvocationHasThenCallback | C3PInvocationHasResultCallback)) {
        void (^invocationThenCallback)();
        void (^invocationResultCallback)(NSObject* result);
        void (^invocationCatchCallback)(NSError* error);

        NSUInteger thenArgumentIndex = invocation.methodSignature.numberOfArguments -
            (invocationAttributes & C3PInvocationHasCatchCallback ? 2 : 1);
        if (invocationAttributes & C3PInvocationHasThenCallback)
        {
            invocationThenCallback = ^void() {
                if (success) {
                    success(nil);
                }
            };
            [invocation setArgument: &invocationThenCallback atIndex: thenArgumentIndex];
        }
        else if (invocationAttributes & C3PInvocationHasResultCallback) {
            invocationResultCallback = ^void(NSObject* result) {
                if (success) {
                    result = [_marshaller marshalToJavaScript: result];
                    success(result);
                }
            };
            [invocation setArgument: &invocationResultCallback atIndex: thenArgumentIndex];
        }

        if (invocationAttributes & C3PInvocationHasCatchCallback) {
            invocationCatchCallback = ^void(NSError* error) {
                if (failure) {
                    failure(error);
                }
            };
            [invocation setArgument: &invocationCatchCallback atIndex: invocation.methodSignature.numberOfArguments - 1];
        }

        [invocation invoke];
    }
    else {
        NSError* __autoreleasing error;
        if (invocationAttributes & C3PInvocationHasOutError) {
            NSError* __autoreleasing * outError = &error;
            [invocation setArgument: &outError atIndex: invocation.methodSignature.numberOfArguments - 1];
        }

        [invocation invoke];

        NSObject* returnValue = [C3PInvocationHelper convertReturnValueFromInvocation: invocation
                                                                       marshaller: _marshaller
                                                                                error: &error];
        if (error != nil) {
            if (failure) {
                failure(error);
            }
        }
        else if (success) {
            success(returnValue);
        }
    }
}


+ (BOOL) validateMemberArgument: (NSString*) value
                          error: (NSError**) outError {
    if (value == nil || value.length == 0) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_INVALID_ARGUMENT
               withMessage: @"Missing required member argument."];
        return NO;
    }
    return YES;
}

+ (BOOL) validateArgumentsArray: (NSArray*) arguments
                          error: (NSError**) outError {
    if (arguments == nil) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_INVALID_ARGUMENT
               withMessage: @"Missing required arguments array argument."];
        return NO;
    }
    return YES;
}

- (Class) validateTypeArgument: (NSString*) pluginClassFullName
                         error: (NSError**) outError {
    if (pluginClassFullName == nil) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_INVALID_ARGUMENT
               withMessage: @"Missing required type argument."];
        return nil;
    }

    NSString* objCClassName = [_namespaceMapper getObjCClassForPluginClass: pluginClassFullName];

    if (objCClassName == nil) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_CLASS_NOT_FOUND
               withMessage: @"Class mapping for %@ not found.", pluginClassFullName];
        return nil;
    }

    Class class = NSClassFromString(objCClassName);
    if (class == nil) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_CLASS_NOT_FOUND
               withMessage: @"C3PJavaScriptBridge: Class %@ not found", pluginClassFullName];
        return nil;
    }

    return class;
}

- (NSObject*) validateInstanceArgument: (NSDictionary*) instance
                                 error: (NSError**) outError {
    if (instance == nil) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_INVALID_ARGUMENT
               withMessage: @"Missing required instance parameter."];
        return nil;
    }

    NSString* pluginClassFullName = [instance objectForKey: @"type"];
    if (pluginClassFullName == nil || pluginClassFullName.length == 0) {
        [C3PJavaScriptBridge setError: outError
                    toCode: ERROR_INVALID_ARGUMENT
               withMessage: @"Instance parameter is missing required type field"];
        return nil;
    }

    Class class = [self validateTypeArgument: pluginClassFullName error: outError];
    if (class == nil) {
        return nil;
    }

    NSObject* instanceObject = [_marshaller marshalFromJavaScript: instance class: class];
    return instanceObject;
}

+ (void) setError: (NSError**) error toCode: (NSInteger) code withMessage: (NSString*) message, ... {
    if (error) {
        va_list args;
        va_start(args, message);
        NSString* formattedMessage = [[NSString alloc] initWithFormat: message arguments: args];
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : formattedMessage};
        *error = [[NSError alloc] initWithDomain: NSPOSIXErrorDomain code: code userInfo: userInfo];
        va_end(args);
    }
}

- (void) onPause {
    // TODO: Notify listeners
}

- (void) onResume {
    // TODO: Notify listeners
}

@end
