// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PJavaScriptEventBridge.h"
#import "C3PInvocationHelper.h"
#import "C3PJavaScriptMarshaller.h"


@interface C3PJavaScriptEventBridge ()

@property (readwrite, copy) C3PJavaScriptEventListener listener;
@property (readwrite) Class sourceClass;
@property (readwrite) NSObject* sourceInstance;
@property (readwrite) NSString* eventName;

@end


@implementation C3PJavaScriptEventBridge {
    NSInvocation* _addInvocation;
    NSInvocation* _removeInvocation;
}

- (C3PJavaScriptEventBridge*) initWithListener: (C3PJavaScriptEventListener) listener
                                  forEventName: (NSString*) eventName
                                 onSourceClass: (Class) sourceClass
                              onSourceInstance: (NSObject*) sourceInstance
                                    marshaller: (C3PJavaScriptMarshaller*) marshaller
                                         error: (NSError**) outError {
    self = [super init];
    if (self != nil) {
        self.listener = listener;
        self.sourceClass = sourceClass;
        self.sourceInstance = sourceInstance;
        self.eventName = eventName;

        void (^listenerConverter)(NSObject*, NSObject*) = ^void (NSObject* source, NSObject* eventObject)
        {
            // Note the source is discarded here. It will be filled in at a higher layer
            // based on the callback context.
            NSDictionary* jsEvent = (NSDictionary*)[marshaller marshalToJavaScript: eventObject];
            listener(jsEvent);
        };
        NSArray* arguments = [[NSArray alloc] initWithObjects: listenerConverter, nil];

        NSString* addMethodName = [NSString stringWithFormat: @"add%@Listener", eventName];
        _addInvocation = [C3PInvocationHelper getInvocationForMethod: addMethodName
                                                               class: sourceClass
                                                            instance: sourceInstance
                                                           arguments: arguments
                                                          marshaller: marshaller
                                                               error: outError];
        if (_addInvocation == nil) {
            return nil;
        }

        NSString* removeMethodName = [NSString stringWithFormat: @"remove%@Listener", eventName];
        _removeInvocation = [C3PInvocationHelper getInvocationForMethod: removeMethodName
                                                                  class: sourceClass
                                                               instance: sourceInstance
                                                              arguments: arguments
                                                             marshaller: marshaller
                                                                  error: outError];
        if (_removeInvocation == nil) {
            return nil;
        }
    }
    return self;
}

- (void) addListener {
    [_addInvocation invoke];
}

- (void) removeListener {
    [_removeInvocation invoke];
}


@end
