// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>

@interface C3PTTestEvent : NSObject

- (C3PTTestEvent*) init;

@property int counter;

@end


typedef void (^C3PTEventListener)(NSObject* sender, C3PTTestEvent* e);


@interface C3PTTestEvents : NSObject

- (C3PTTestEvents*) init;

+ (void) addStaticEventListener: (C3PTEventListener) listener;
+ (void) removeStaticEventListener: (C3PTEventListener) listener;
+ (void) raiseStaticEvent;

- (void) addInstanceEventListener: (C3PTEventListener) listener;
- (void) removeInstanceEventListener: (C3PTEventListener) listener;
- (void) raiseInstanceEvent;

@end
