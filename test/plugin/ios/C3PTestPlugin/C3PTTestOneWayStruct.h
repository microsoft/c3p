// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>

@interface C3PTTestOneWayStruct : NSObject

- (C3PTTestOneWayStruct*) init __unavailable;

- (C3PTTestOneWayStruct*) initWithValue: (NSString*) value;

@property (readonly) NSString* value;

@end

