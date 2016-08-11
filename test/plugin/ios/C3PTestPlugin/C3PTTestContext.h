// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <UIKit/UIKit.h>

@interface C3PTTestContext : NSObject

- (C3PTTestContext* _Nullable) init NS_UNAVAILABLE;

- (C3PTTestContext* _Nullable) initWithApplication: (UIApplication* _Nonnull) application
                                       testFailure: (BOOL) fail
                                             error: (NSError* _Nonnull * _Nullable) outError;

- (void) testConstructorAppContextError: (NSError* _Nonnull * _Nullable) outError;

+ (void) testStaticMethodAppContext: (UIApplication* _Nonnull) appContext
                              error: (NSError* _Nonnull * _Nullable) outError;

+ (void) testStaticMethodAppContext2: (UIApplication* _Nonnull) appContext
                                 and: (int) someOtherParam
                               error: (NSError* _Nonnull * _Nullable) outError;

+ (void) testStaticMethodWindowContext: (UIWindow* _Nullable) windowContext
                               error: (NSError* _Nonnull * _Nullable) outError;

+ (void) testStaticMethodWindowContext2: (UIWindow* _Nullable) windowContext
                                  and: (int) someOtherParam
                                error: (NSError* _Nonnull * _Nullable) outError;

- (void) testMethodAppContext: (UIApplication* _Nonnull) appContext
                        error: (NSError* _Nonnull * _Nullable) outError;

- (void) testMethodAppContext2: (UIApplication* _Nonnull) appContext
                           and: (int) someOtherParam
                         error: (NSError* _Nonnull * _Nullable) outError;

- (void) testMethodWindowContext: (UIWindow* _Nullable) windowContext
                         error: (NSError* _Nonnull * _Nullable) outError;

- (void) testMethodWindowContext2: (UIWindow* _Nullable) windowContext
                            and: (int) someOtherParam
                          error: (NSError* _Nonnull * _Nullable) outError;

- (void) testMethodAppContext3Async: (UIApplication* _Nonnull) appContext
                               then: (void(^ _Nonnull)()) success
                              catch: (void(^ _Nonnull)(NSError* _Nonnull)) failure;

- (void) testMethodAppContext4Async: (UIApplication* _Nonnull) appContext
                                and: (int) someOtherParam
                               then: (void(^ _Nonnull)()) success
                              catch: (void(^ _Nonnull)(NSError* _Nonnull)) failure;

- (void) testMethodWindowContext3Async: (UIWindow* _Nullable) windowContext
                                  then: (void(^ _Nonnull)()) success
                                 catch: (void(^ _Nonnull)(NSError* _Nonnull)) failure;

- (void) testMethodWindowContext4Async: (UIWindow* _Nullable) windowContext
                                   and: (int) someOtherParam
                                  then: (void(^ _Nonnull)()) success
                                 catch: (void(^ _Nonnull)(NSError* _Nonnull)) failure;

- (void) testAndroidActivityAsync: (UIWindow* _Nullable) windowContext
                             then: (void(^ _Nonnull)()) success
                            catch: (void(^ _Nonnull)(NSError* _Nonnull)) failure;
@end
