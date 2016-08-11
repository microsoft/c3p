// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PTTestMethods.h"

@implementation C3PTTestMethods

- (C3PTTestMethods*) init {
    self = [super init];
    return self;
}

+ (void) staticLog: (NSString*) value
              fail: (BOOL) fail
              error: (NSError**) outError {
    if (!fail) {
        NSLog(@"C3PTestPlugin: staticLog: %@", value);
    }
    else {
        NSLog(@"C3PTestPlugin: staticLog(fail): %@", value);
        [C3PTTestMethods setRequestedFailureError: outError];
    }
}

+ (NSString*) staticEcho: (NSString*) value
                    fail: (BOOL) fail
                   error: (NSError**) outError {
    if (!fail) {
        return value;
    }
    else {
        [C3PTTestMethods setRequestedFailureError: outError];
        return nil;
    }
}

+ (C3PTTestStruct*) staticEchoData: (C3PTTestStruct*) data
                              fail: (BOOL) fail
                             error: (NSError**) outError {
    if (!fail) {
        return data;
    }
    else {
        [C3PTTestMethods setRequestedFailureError: outError];
        return nil;
    }
}

- (void) log: (NSString*) value
        fail: (BOOL) fail
       error: (NSError**) outError {
    if (!fail) {
        NSLog(@"C3PTestPlugin: log: %@", value);
    }
    else {
        NSLog(@"C3PTestPlugin: log(fail): %@", value);
        [C3PTTestMethods setRequestedFailureError: outError];
    }
}


- (NSString*) echo: (NSString*) value
              fail: (BOOL) fail
             error: (NSError**) outError {
    if (!fail) {
        return value;
    }
    else {
        [C3PTTestMethods setRequestedFailureError: outError];
        return nil;
    }
}

- (C3PTTestStruct*) echoData: (C3PTTestStruct*) data
                        fail: (BOOL) fail
                       error: (NSError**) outError {
    if (!fail) {
        return data;
    }
    else {
        [C3PTTestMethods setRequestedFailureError: outError];
        return nil;
    }
}

- (NSArray<C3PTTestStruct*>*) echoDataList: (NSArray<C3PTTestStruct*>*) dataList
                                      fail: (BOOL) fail
                                     error: (NSError**) outError {
    if (!fail) {
        return dataList;
    }
    else {
        [C3PTTestMethods setRequestedFailureError: outError];
        return nil;
    }
}

- (NSNumber*) echoNullableInt: (NSNumber*) value {
    if (value == nil) {
        return nil;
    }
    int intValue = [value intValue];
    return [NSNumber numberWithInt: intValue];
}

- (NSNumber*) echoNullableBool: (NSNumber*) value {
    if (value == nil) {
        return nil;
    }
    BOOL boolValue = [value boolValue];
    return [NSNumber numberWithInt: boolValue];
}

- (NSUUID*) echoUuid: (NSUUID*) value{
    return value;
}

+ (void) setRequestedFailureError: (NSError**) outError {
    NSDictionary* userInfo = @{NSLocalizedDescriptionKey : @"Requested failure."};
    *outError = [[NSError alloc] initWithDomain: @"C3PTestPlugin" code: 1 userInfo: userInfo];
}

@end
