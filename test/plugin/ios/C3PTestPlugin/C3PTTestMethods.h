// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>
#import "C3PTTestStruct.h"


@interface C3PTTestMethods : NSObject

- (C3PTTestMethods*) init;

+ (void) staticLog: (NSString*) value
              fail: (BOOL) fail
              error: (NSError**) outError;


+ (NSString*) staticEcho: (NSString*) value
                    fail: (BOOL) fail
                   error: (NSError**) outError;

+ (C3PTTestStruct*) staticEchoData: (C3PTTestStruct*) data
                              fail: (BOOL) fail
                             error: (NSError**) outError;

- (void) log: (NSString*) value
        fail: (BOOL) fail
        error: (NSError**) outError;

- (NSString*) echo: (NSString*) value
              fail: (BOOL) fail
             error: (NSError**) outError;

- (C3PTTestStruct*) echoData: (C3PTTestStruct*) data
                        fail: (BOOL) fail
                       error: (NSError**) outError;

- (NSArray<C3PTTestStruct*>*) echoDataList: (NSArray<C3PTTestStruct*>*) dataList
                                      fail: (BOOL) fail
                                     error: (NSError**) outError;

- (NSNumber*) echoNullableInt: (NSNumber*) value;

- (NSNumber*) echoNullableBool: (NSNumber*) value;

- (NSUUID*) echoUuid: (NSUUID*) value;

@end

