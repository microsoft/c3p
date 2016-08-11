// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PTTestContext.h"

@interface C3PTTestContext()

@property UIApplication* app;

@end

@implementation C3PTTestContext

- (C3PTTestContext*) initWithApplication: (UIApplication*) application
                             testFailure: (BOOL) fail
                                   error: (NSError* _Nonnull * _Nullable) outError {
    self = [super init];

    if (!fail) {
        self.app = application;
    }
    else if (self) {
        // Note when a constructor fails with an NSError, it must still return the (partially-initialized) instance.
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Requested failure."};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 1 userInfo: userInfo];
    }

    return self;
}

- (void) testConstructorAppContextError: (NSError**) outError {
    if (self.app == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Constructor app context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}

+ (void) testStaticMethodAppContext: (UIApplication*) appContext error: (NSError**) outError {
    if (appContext == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Static method app context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}

+ (void) testStaticMethodAppContext2: (UIApplication*) appContext and: (int) someOtherParam error: (NSError**) outError {
    if (appContext == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Static method app context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}


+ (void) testStaticMethodWindowContext: (UIWindow*) windowContext error: (NSError**) outError {
    if (windowContext == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Static method window context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}


+ (void) testStaticMethodWindowContext2: (UIWindow*) windowContext and: (int) someOtherParam error: (NSError**) outError {
    if (windowContext == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Static method window context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}

- (void) testMethodAppContext: (UIApplication*) appContext error: (NSError**) outError {
    if (appContext == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Method app context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}


- (void) testMethodAppContext2: (UIApplication*) appContext and: (int) someOtherParam error: (NSError**) outError {
    if (appContext == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Method app context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}


- (void) testMethodWindowContext: (UIWindow*) windowContext error: (NSError**) outError {
    if (windowContext == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Method window context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}


- (void) testMethodWindowContext2: (UIWindow*) windowContext and: (int) someOtherParam error: (NSError**) outError {
    if (windowContext == nil) {
        NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Method window context is null!"};
        *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 99 userInfo: userInfo];
    }
}


- (void) testMethodAppContext3Async: (UIApplication*) appContext
                               then: (void(^)()) success
                              catch: (void(^)(NSError*)) failure {
    NSError* error;
    [self testMethodAppContext: appContext error: &error];
    if (error != nil) failure(error);
    else success();
}

- (void) testMethodAppContext4Async: (UIApplication*) appContext
                                and: (int) someOtherParam
                               then: (void(^)()) success
                              catch: (void(^)(NSError*)) failure {
    NSError* error;
    [self testMethodAppContext2: appContext and: someOtherParam error: &error];
    if (error != nil) failure(error);
    else success();
}

- (void) testMethodWindowContext3Async: (UIWindow*) windowContext
                                  then: (void(^)()) success
                                 catch: (void(^)(NSError*)) failure {
    NSError* error;
    [self testMethodWindowContext: windowContext error: &error];
    if (error != nil) failure(error);
    else success();
}

- (void) testMethodWindowContext4Async: (UIWindow*) windowContext
                                   and: (int) someOtherParam
                                  then: (void(^)()) success
                                 catch: (void(^)(NSError*)) failure {
    NSError* error;
    [self testMethodWindowContext2: windowContext and: someOtherParam error: &error];
    if (error != nil) failure(error);
    else success();
}

- (void) testAndroidActivityAsync: (UIWindow*) windowContext
                             then: (void(^)()) success
                            catch: (void(^)(NSError*)) failure {
    // This is an Android-only test, so the implementation here is a no-op.
    success();
}

@end
