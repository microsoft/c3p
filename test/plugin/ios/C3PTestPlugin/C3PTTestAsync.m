// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PTTestAsync.h"


@implementation C3PTTestAsync

- (C3PTTestAsync*) init {
    self = [super init];
    return self;
}

+ (void) staticLogAsync: (NSString*) value
                   fail: (BOOL) fail
                   then: (void(^)()) success
                  catch: (void(^)(NSError*)) failure {
    if (!fail) {
        NSLog(@"C3PTestPlugin: staticLogAsync: %@", value);
        if (success) {
            success();
        }
    }
    else {
        NSLog(@"C3PTestPlugin: staticLogAsync(fail): %@", value);
        NSError* error;
        [C3PTTestAsync setRequestedFailureError: &error];
        if (failure) {
            failure(error);
        }
    }
}

+ (void) staticEchoAsync: (NSString*) value
                    fail: (BOOL) fail
                  result: (void(^)(NSString*)) success
                   catch: (void(^)(NSError*)) failure {
    if (!fail) {
        if (success) {
            success(value);
        }
    }
    else {
        NSError* error;
        [C3PTTestAsync setRequestedFailureError: &error];
        if (failure) {
            failure(error);
        }
    }
}

+ (void) staticEchoDataAsync: (C3PTTestStruct*) data
                        fail: (BOOL) fail
                      result: (void(^)(C3PTTestStruct*)) success
                       catch: (void(^)(NSError*)) failure {
    if (!fail) {
        if (success) {
            success(data);
        }
    }
    else {
        NSError* error;
        [C3PTTestAsync setRequestedFailureError: &error];
        if (failure) {
            failure(error);
        }
    }
}

- (void) logAsync: (NSString*) value
             fail: (BOOL) fail
             then: (void(^)()) success
            catch: (void(^)(NSError*)) failure {
    if (!fail) {
        NSLog(@"C3PTestPlugin: logAsync: %@", value);
        if (success) {
            success();
        }
    }
    else {
        NSLog(@"C3PTestPlugin: logAsync(fail): %@", value);
        NSError* error;
        [C3PTTestAsync setRequestedFailureError: &error];
        if (failure) {
            failure(error);
        }
    }
}

- (void) echoAsync: (NSString*) value
              fail: (BOOL) fail
            result: (void(^)(NSString*)) success
             catch: (void(^)(NSError*)) failure {
    if (!fail) {
        if (success) {
            success(value);
        }
    }
    else {
        NSError* error;
        [C3PTTestAsync setRequestedFailureError: &error];
        if (failure) {
            failure(error);
        }
    }
}

- (void) echoDataAsync: (C3PTTestStruct*) data
                  fail: (BOOL) fail
                result: (void(^)(C3PTTestStruct*)) success
                 catch: (void(^)(NSError*)) failure {
    if (!fail) {
        if (success) {
            success(data);
        }
    }
    else {
        NSError* error;
        [C3PTTestAsync setRequestedFailureError: &error];
        if (failure) {
            failure(error);
        }
    }
}

- (void) echoDataListAsync: (NSArray<C3PTTestStruct*>*) dataList
                      fail: (BOOL) fail
                    result: (void(^)(NSArray<C3PTTestStruct*>*)) success
                     catch: (void(^)(NSError*)) failure {
    if (!fail) {
        if (success) {
            success(dataList);
        }
    }
    else {
        NSError* error;
        [C3PTTestAsync setRequestedFailureError: &error];
        if (failure) {
            failure(error);
        }
    }
}

- (void) echoNullableIntAsync: (NSNumber*) value
                       result: (void(^)(NSNumber*)) success
                        catch: (void(^)(NSError*)) failure {
    if (value == nil) {
        if (success) {
            success(nil);
        }
    }
    else {
        int intValue = [value intValue];
        if (success) {
            success([NSNumber numberWithInt: intValue]);
        }
    }
}

- (void) echoNullableBoolAsync: (NSNumber*) value
                        result: (void(^)(NSNumber*)) success
                         catch: (void(^)(NSError*)) failure {
    if (value == nil) {
        if (success) {
            success(nil);
        }
    }
    else {
        BOOL boolValue = [value boolValue];
        if (success) {
            success([NSNumber numberWithBool: boolValue]);
        }
    }
}

- (void) echoUuidAsync: (NSUUID*) value
                result: (void(^)(NSUUID*)) success
                 catch: (void(^)(NSError*)) failure{
    if (success) {
        success(value);
    }
}

+ (void) setRequestedFailureError: (NSError**) outError {
    NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Requested failure."};
    *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 1 userInfo: userInfo];
}


@end
