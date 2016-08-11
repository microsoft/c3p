// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PTTestOuter.h"

@implementation C3PTTestOuter_InnerClass

- (C3PTTestOuter_InnerClass*) init {
    self = [super init];
    return self;
}

@synthesize value;

@end

@implementation C3PTTestOuter_InnerStruct

- (C3PTTestOuter_InnerStruct*) init {
    self = [super init];
    return self;
}

@synthesize value;

@end


@implementation C3PTTestOuter

- (C3PTTestOuter*) init {
    self = [super init];
    return self;
}

@synthesize innerClassProperty;
@synthesize innerStructProperty;
@synthesize innerEnumProperty;


@end