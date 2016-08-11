// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>


@interface C3PTTestOuter_InnerClass : NSObject

- (C3PTTestOuter_InnerClass*) init;

@property int value;

@end


@interface C3PTTestOuter_InnerStruct : NSObject

- (C3PTTestOuter_InnerStruct*) init;

@property int value;

@end


typedef NS_ENUM(NSInteger, C3PTTestOuter_InnerEnum) {
    InnerEnumZero,
    InnerEnumOne,
    InnerEnumTwo,
    InnerEnumThree,
};


@interface C3PTTestOuter : NSObject

- (C3PTTestOuter*) init;

@property C3PTTestOuter_InnerClass* innerClassProperty;
@property C3PTTestOuter_InnerStruct* innerStructProperty;
@property C3PTTestOuter_InnerEnum innerEnumProperty;

@end
