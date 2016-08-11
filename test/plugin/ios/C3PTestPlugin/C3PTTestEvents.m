// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PTTestEvents.h"

@implementation C3PTTestEvent

- (C3PTTestEvent*) init {
    self = [super init];
    return self;
}

@synthesize counter;

@end

@implementation C3PTTestEvents {
    NSHashTable<C3PTEventListener>* _instanceListeners;
    int _instanceEventCounter;
}

static NSHashTable<C3PTEventListener>* _staticListeners = nil;
static int _staticEventCounter = 0;

- (C3PTTestEvents*) init {
    self = [super init];
    if (self) {
        _instanceListeners = [[NSHashTable<C3PTEventListener> alloc] init];
        _instanceEventCounter = 0;
    }
    return self;
}

+ (void) addStaticEventListener: (C3PTEventListener) listener {
    if (_staticListeners == nil) {
        _staticListeners = [[NSHashTable<C3PTEventListener> alloc] init];
    }

    [_staticListeners addObject:listener];
    NSLog(@"C3PTTestEvents: Added static event listener (%@).", listener);
}

+ (void) removeStaticEventListener: (C3PTEventListener) listener {
    if (_staticListeners == nil || ![_staticListeners containsObject: listener]) {
        NSLog(@"C3PTTestEvents: Static event listener (%@) to remove was not found.", listener);
    }
    else {
        [_staticListeners removeObject:listener];
        NSLog(@"C3PTTestEvents: Removed static event listener (%@).", listener);
    }
}

+ (void) raiseStaticEvent {
    if (_staticListeners != nil) {
        C3PTTestEvent* e = [[C3PTTestEvent alloc] init];
        e.counter = ++_staticEventCounter;
        for (C3PTEventListener listener in _staticListeners) {
            NSLog(@"C3PTTestEvents: Sending static event (%d) to listener (%@).", e.counter, listener);
            listener(nil, e);
        }
    }
}

- (void) addInstanceEventListener: (C3PTEventListener) listener {
    [_instanceListeners addObject:listener];
    NSLog(@"C3PTTestEvents: Added event listener (%@).", listener);
}

- (void) removeInstanceEventListener: (C3PTEventListener) listener {
    if (![_instanceListeners containsObject: listener]) {
        NSLog(@"C3PTTestEvents: Event listener (%@) to remove was not found.", listener);
    }
    else {
        [_instanceListeners removeObject:listener];
        NSLog(@"C3PTTestEvents: Removed event listener (%@).", listener);
    }
}

- (void) raiseInstanceEvent {
    C3PTTestEvent* e = [[C3PTTestEvent alloc] init];
    e.counter = ++_instanceEventCounter;
    for (C3PTEventListener listener in _instanceListeners) {
        NSLog(@"C3PTTestEvents: Sending event (%d) to listener (%@).", e.counter, listener);
        listener(self, e);
    }
}

@end
