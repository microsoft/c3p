// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PTTestProperties.h"

@implementation C3PTTestProperties {
}

static C3PTTestStruct* _staticStruct = nil;
static NSArray<NSString*>* _staticArray = nil;
static double _staticDouble = 0;
static C3PTTestEnum _staticEnum = C3PTTestEnumZero;
static BOOL _staticBool = NO;

- (C3PTTestProperties*) init {
    self = [super init];
    return self;
}

+ (C3PTTestStruct*) staticStructProperty {
    return _staticStruct;
}

+ (void) setStaticStructProperty: (C3PTTestStruct*) value {
    _staticStruct = value;
}

+ (NSArray<NSString*>*) staticListProperty {
    return _staticArray;
}

+ (void) setStaticListProperty: (NSArray<NSString*>*) value {
    _staticArray = value;
}

+ (double) staticDoubleProperty {
    return _staticDouble;
}

+ (void) setStaticDoubleProperty: (double) value {
    _staticDouble = value;
}

+ (int) staticReadonlyIntProperty {
    return 10;
}

+ (C3PTTestEnum) staticEnumProperty {
    return _staticEnum;
}

+ (void) setStaticEnumProperty: (C3PTTestEnum) value {
    _staticEnum = value;
}

+ (BOOL) staticBoolProperty {
    return _staticBool;
}

+ (void) setStaticBoolProperty: (BOOL) value {
    _staticBool = value;
}


@synthesize structProperty;
@synthesize listProperty;
@synthesize doubleProperty;

- (NSArray<NSString*>*) readonlyListProperty {
    NSArray<NSString*>* list = @[@"One", @"Two", @"Three"];
    return list;
}

- (int) readonlyIntProperty {
    return 20;
}

@synthesize enumProperty;
@synthesize boolProperty;
@synthesize nullableIntProperty;
@synthesize nullableDoubleProperty;
@synthesize uuidProperty;
@synthesize uriProperty;

- (C3PTTestOneWayStruct*) oneWayStructProperty {
    return [[C3PTTestOneWayStruct alloc] initWithValue: @"test"];
}

@end
