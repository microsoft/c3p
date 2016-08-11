// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>

@interface C3PTTestStruct : NSObject

- (C3PTTestStruct*) init;

- (C3PTTestStruct*) init: (NSDate*) value;

@property NSDate* value;

- (void) updateValue: (NSDate*) newValue;

- (NSString*) toXmlError: (NSError**) outError;

@end

