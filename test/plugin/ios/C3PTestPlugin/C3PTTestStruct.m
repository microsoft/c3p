// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PTTestStruct.h"

@implementation C3PTTestStruct {
    NSDate* _value;
}

- (C3PTTestStruct*) init {
    self = [super init];
    return self;
}

- (C3PTTestStruct*) init: (NSDate*) value {
    self = [super init];
    if (self) {
        _value = value;
    }
    return self;
}

- (NSDate*) value {
    return _value;
}

- (void) setValue: (NSDate*) value {
    _value = value;
}

- (void) updateValue: (NSDate*) newValue {
    self.value = newValue;
}

- (NSString*) toXmlError: (NSError**) outError {
    return [[@"<value>" stringByAppendingString: [self.value description]] stringByAppendingString: @"</value>"];
}

@end