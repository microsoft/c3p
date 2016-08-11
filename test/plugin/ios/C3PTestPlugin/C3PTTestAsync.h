// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>
#import "C3PTTestStruct.h"


@interface C3PTTestAsync : NSObject

- (C3PTTestAsync*) init;

+ (void) staticLogAsync: (NSString*) value
                   fail: (BOOL) fail
                   then: (void(^)()) success
                  catch: (void(^)(NSError*)) failure;

+ (void) staticEchoAsync: (NSString*) value
                    fail: (BOOL) fail
                  result: (void(^)(NSString*)) success
                   catch: (void(^)(NSError*)) failure;

+ (void) staticEchoDataAsync: (C3PTTestStruct*) data
                        fail: (BOOL) fail
                      result: (void(^)(C3PTTestStruct*)) success
                       catch: (void(^)(NSError*)) failure;

- (void) logAsync: (NSString*) value
             fail: (BOOL) fail
             then: (void(^)()) success
            catch: (void(^)(NSError*)) failure;

- (void) echoAsync: (NSString*) value
              fail: (BOOL) fail
            result: (void(^)(NSString*)) success
             catch: (void(^)(NSError*)) failure;

- (void) echoDataAsync: (C3PTTestStruct*) data
                  fail: (BOOL) fail
                result: (void(^)(C3PTTestStruct*)) success
                 catch: (void(^)(NSError*)) failure;

- (void) echoDataListAsync: (NSArray<C3PTTestStruct*>*) dataList
                      fail: (BOOL) fail
                    result: (void(^)(NSArray<C3PTTestStruct*>*)) success
                     catch: (void(^)(NSError*)) failure;

- (void) echoNullableIntAsync: (NSNumber*) value
                       result: (void(^)(NSNumber*)) success
                        catch: (void(^)(NSError*)) failure;

- (void) echoNullableBoolAsync: (NSNumber*) value
                        result: (void(^)(NSNumber*)) success
                         catch: (void(^)(NSError*)) failure;

- (void) echoUuidAsync: (NSUUID*) value
                result: (void(^)(NSUUID*)) success
                 catch: (void(^)(NSError*)) failure;

@end

