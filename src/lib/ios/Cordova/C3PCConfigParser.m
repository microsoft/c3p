// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PCConfigParser.h"

@interface C3PCConfigParser ()

@property (readwrite) NSMutableDictionary<NSString*,NSString*>* prefixMappings;
@property (readwrite) NSMutableSet<NSString*>* marshalByValueClasses;

@end

@implementation C3PCConfigParser {
    NSString* featureName;
}

- (C3PCConfigParser*) init {
    self = [super init];
    if (self != nil) {
        self.prefixMappings = [[NSMutableDictionary<NSString*,NSString*> alloc] initWithCapacity: 5];
        self.marshalByValueClasses = [[NSMutableSet<NSString*> alloc] initWithCapacity: 5];
        featureName = nil;
    }
    return self;
}

- (void)     parser: (NSXMLParser*) parser
    didStartElement: (nonnull NSString*) elementName
       namespaceURI: (nullable NSString*) namespaceURI
      qualifiedName: (nullable NSString*) qName
         attributes: (nonnull NSDictionary<NSString*, NSString*>*) attributeDict {
    if ([elementName isEqualToString: @"feature"]) {
        featureName = [attributeDict[@"name"] lowercaseString];
    }
    else if ([featureName isEqualToString: @"c3p"] && [elementName isEqualToString: @"param"]) {
        NSString* paramName = attributeDict[@"name"];
        NSString* paramValue = attributeDict[@"value"];
        if (paramName != nil && [paramName hasPrefix: @"plugin-namespace:"]) {
            NSString* nsPrefix = [paramName substringFromIndex: @"plugin-namespace:".length];
            if (nsPrefix.length > 0 && paramValue != nil && paramValue.length > 0) {
                [self.prefixMappings setObject: paramValue forKey: nsPrefix];
            }
        }
        else if (paramName != nil && [paramName hasPrefix: @"plugin-class:"]) {
            NSString* className = [paramName substringFromIndex: @"plugin-class:".length];
            if (className.length > 0 && paramValue != nil && [paramValue isEqualToString: @"marshal-by-value"]) {
                [self.marshalByValueClasses addObject: className];
            }
        }
    }
}

- (void)     parser: (NSXMLParser*) parser
      didEndElement: (nonnull NSString*) elementName
       namespaceURI: (nullable NSString*) namespaceURI
      qualifiedName: (nullable NSString*) qName {

}

- (void)     parser: (NSXMLParser*) parser
 parseErrorOccurred: (NSError*) parseError {
    NSAssert(NO, @"config.xml parser error line %ld col %ld", (long)parser.lineNumber, (long)parser.columnNumber);
}

@end
