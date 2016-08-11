// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>

@interface C3PCConfigParser : NSObject <NSXMLParserDelegate>

@property (readonly) NSMutableDictionary* prefixMappings;
@property (readonly) NSMutableSet* marshalByValueClasses;

@end
