// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>

@class C3PNamespaceMapper;
@protocol C3PApplicationContext;


typedef void (^C3PJavaScriptEventListener)(NSDictionary* event);


/// Bridge for JavaScript calls into Obj-C code. Instantiates and invokes arbitrary
/// classes and members using reflection, and converts parameters and results from/to JSON.
@interface C3PJavaScriptBridge : NSObject

@property (readonly) C3PNamespaceMapper* namespaceMapper;

- (C3PJavaScriptBridge*) initWithContext: (id<C3PApplicationContext>) context;

- (void) registerMarshalByValueClass: (NSString*) className;

- (NSObject*) getStaticProperty: (NSString*) property
                         onType: (NSString*) type
                          error: (NSError**) outError;

- (void) setStaticProperty: (NSString*) property
                    onType: (NSString*) type
                   toValue: (NSObject*) value
                     error: (NSError**) outError;

- (void) invokeStaticMethod: (NSString*) method
                     onType: (NSString*) type
              withArguments: (NSArray*) arguments
                     result: (void(^)(NSObject*)) success
                      catch: (void(^)(NSError*)) failure;

- (void) addListenerForStaticEvent: (NSString*) event
                            onType: (NSString*) type
                          listener: (C3PJavaScriptEventListener) listener
                             error: (NSError**) outError;

- (void) removeListenerForStaticEvent: (NSString*) event
                               onType: (NSString*) type
                             listener: (C3PJavaScriptEventListener) listener
                                error: (NSError**) outError;

- (NSDictionary*) createInstanceOfType: (NSString*) type
                         withArguments: (NSArray*) arguments
                                 error: (NSError**) outError;

- (void) releaseInstance: (NSDictionary*) instance
                   error: (NSError**) outError;

- (NSObject*) getProperty: (NSString*) property
               onInstance: (NSDictionary*) instance
                    error: (NSError**) outError;

- (void) setProperty: (NSString*) property
          onInstance: (NSDictionary*) instance
             toValue: (NSObject*) value
               error: (NSError**) outError;

- (void) invokeMethod: (NSString*) method
           onInstance: (NSDictionary*) instance
        withArguments: (NSArray*) arguments
               result: (void(^)(NSObject*)) success
                catch: (void(^)(NSError*)) failure;

- (void) addListenerForEvent: (NSString*) event
                  onInstance: (NSDictionary*) instance
                    listener: (C3PJavaScriptEventListener) listener
                       error: (NSError**) outError;

- (void) removeListenerForEvent: (NSString*) event
                     onInstance: (NSDictionary*) instance
                       listener: (C3PJavaScriptEventListener) listener
                          error: (NSError**) outError;

+ (void) setError: (NSError**) error
           toCode: (NSInteger) code
      withMessage: (NSString*) message, ...;

- (void) onPause;

- (void) onResume;

@end

#define ERROR_INVALID_ARGUMENT EINVAL
#define ERROR_CLASS_NOT_FOUND ENOENT
#define ERROR_MEMBER_NOT_FOUND ENOENT
#define ERROR_NOT_IMPLEMENTED ENOSYS

