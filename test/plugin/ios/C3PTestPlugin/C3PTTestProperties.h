// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>
#import "C3PTTestStruct.h"
#import "C3PTTestOneWayStruct.h"

typedef NS_ENUM(NSInteger, C3PTTestEnum) {
    C3PTTestEnumZero,
    C3PTTestEnumOne,
    C3PTTestEnumTwo,
    C3PTTestEnumThree,
};

@interface C3PTTestProperties : NSObject

- (C3PTTestProperties*) init;

+ (C3PTTestStruct*) staticStructProperty;
+ (void) setStaticStructProperty: (C3PTTestStruct*) value;
+ (NSArray<NSString*>*) staticListProperty;
+ (void) setStaticListProperty: (NSArray<NSString*>*) value;
+ (double) staticDoubleProperty;
+ (void) setStaticDoubleProperty: (double) value;
+ (C3PTTestEnum) staticEnumProperty;
+ (void) setStaticEnumProperty: (C3PTTestEnum) value;
+ (BOOL) staticBoolProperty;
+ (void) setStaticBoolProperty: (BOOL) value;

@property C3PTTestStruct* structProperty;
@property NSArray<NSString*>* listProperty;
@property (readonly) NSArray<NSString*>* readonlyListProperty;
@property double doubleProperty;
@property (readonly) int readonlyIntProperty;
@property C3PTTestEnum enumProperty;
@property BOOL boolProperty;
@property NSNumber* nullableIntProperty;
@property NSNumber* nullableDoubleProperty;
@property NSUUID* uuidProperty;
@property NSURL* uriProperty;
@property (readonly) C3PTTestOneWayStruct* oneWayStructProperty;

// Currently broken: does not get bound as a property on IOS.
//+ (int) staticReadonlyIntProperty;

@end

