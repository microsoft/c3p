// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PTTestOneWayStruct.h"

@implementation C3PTTestOneWayStruct {
    NSString* _value;
}

- (C3PTTestOneWayStruct*) initWithValue: (NSString*) value {
    self = [super init];
    if (self) {
        _value = value;
    }
    return self;
}

- (NSString*) value {
    return _value;
}

@end