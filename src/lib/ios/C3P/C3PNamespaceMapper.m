// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import "C3PNamespaceMapper.h"
#import <UIKit/UIKit.h>

@implementation C3PNamespaceMapper {
    NSMutableDictionary<NSString*, NSString*>* _prefixesToNamespaces;
    NSMutableDictionary<NSString*, NSString*>* _namespacesToPrefixes;
}

- (C3PNamespaceMapper*) init {
    self = [super init];
    if (self)
    {
        _prefixesToNamespaces = [[NSMutableDictionary<NSString*, NSString*> alloc] init];
        _namespacesToPrefixes = [[NSMutableDictionary<NSString*, NSString*> alloc] init];
    }
    return self;
}

- (void) registerPluginNamespace: (NSString*) pluginNamespace forObjCPrefix: (NSString*) objCPrefix {
    [_prefixesToNamespaces setObject: pluginNamespace forKey: objCPrefix];
    [_namespacesToPrefixes setObject: objCPrefix forKey: pluginNamespace];
    NSLog(@"C3PNamespaceMapper: Registered plugin namespace mapping: %@ <=> %@", pluginNamespace, objCPrefix);
}

- (NSString*) getObjCPrefixForPluginNamespace: (NSString*) pluginNamespace {
    NSString* objCPrefix = [_namespacesToPrefixes objectForKey: pluginNamespace];

    if (objCPrefix == nil) {
        NSLog(@"C3PNamespaceMapper: No Obj-C prefix mapping was found for plugin namespace: %@", pluginNamespace);
    }

    return objCPrefix;
}

- (NSString*) getPluginNamespaceForObjCPrefix: (NSString*) objCPrefix {
    NSString* pluginNamespace = [_prefixesToNamespaces objectForKey: objCPrefix];

    if (pluginNamespace == nil) {
        NSLog(@"C3PNamespaceMapper: No plugin namespace mapping was found for Obj-C prefix: %@", objCPrefix);
    }

    return pluginNamespace;
}

- (NSString*) getObjCClassForPluginClass: (NSString*) pluginClassFullName {
    NSRange lastDotRange = [pluginClassFullName rangeOfString: @"." options: NSBackwardsSearch | NSLiteralSearch];
    if (lastDotRange.location == NSNotFound) {
        if ([pluginClassFullName isEqualToString: @"<application>"]) {
            return NSStringFromClass([UIApplication class]);
        }
        else if ([pluginClassFullName isEqualToString: @"<window>"]) {
            return NSStringFromClass([UIWindow class]);
        }
        else if ([pluginClassFullName isEqualToString: @"<uuid>"]) {
            return NSStringFromClass([NSUUID class]);
        }
        else if ([pluginClassFullName isEqualToString: @"<uri>"]) {
            return NSStringFromClass([NSURL class]);
        }
        else if ([pluginClassFullName isEqualToString: @"<date>"]) {
            return NSStringFromClass([NSDate class]);
        }

        NSLog(@"C3PNamespaceMapper: Plugin class full name does not have a package: %@", pluginClassFullName);
        return nil;
    }

    NSString* pluginNamespace = [pluginClassFullName substringToIndex: lastDotRange.location];
    NSString* className = [pluginClassFullName substringFromIndex: lastDotRange.location + 1];
    NSString* objCPrefix = [self getObjCPrefixForPluginNamespace: pluginNamespace];
    if (objCPrefix == nil) {
        return nil;
    }

    NSString* objCClassName = [objCPrefix stringByAppendingString: className];
    return objCClassName;
}

- (NSString*) getPluginClassForObjCClass: (NSString*) objCClassName {
    NSInteger firstLowercaseIndex = -1;
    for (NSUInteger i = 0; i < [objCClassName length]; i++) {
        unichar c = [objCClassName characterAtIndex: i];
        if (![[NSCharacterSet uppercaseLetterCharacterSet] characterIsMember: c] &&
            ![[NSCharacterSet decimalDigitCharacterSet] characterIsMember: c]) {
            firstLowercaseIndex = i;
            break;
        }
    }

    if (firstLowercaseIndex < 2) {
        if ([objCClassName hasSuffix: @"UUID"]) {
            return @"<uuid>";
        }
        else if ([objCClassName isEqualToString: @"NSURL"]) {
            return @"<uri>";
        }
        else if ([objCClassName hasSuffix: @"Date"]) {
            return @"<date>";
        }
        else {
            NSLog(@"C3PNamespaceMapper: Obj-C class does not have a prefix: %@", objCClassName);
            return nil;
        }
    }

    NSString* objCPrefix = [objCClassName substringToIndex: firstLowercaseIndex - 1];
    NSString* className = [objCClassName substringFromIndex: firstLowercaseIndex - 1];
    NSString* pluginNamespace = [self getPluginNamespaceForObjCPrefix: objCPrefix];
    if (pluginNamespace == nil) {
        if ([objCClassName isEqualToString: NSStringFromClass([UIApplication class])]) {
            return @"<application>";
        }
        else if ([objCClassName isEqualToString: NSStringFromClass([UIWindow class])]) {
            return @"<window>";
        }
        else if ([objCClassName isEqualToString: NSStringFromClass([NSUUID class])]) {
            return @"<uuid>";
        }

        return nil;
    }

    NSString* pluginClassName = [[pluginNamespace stringByAppendingString: @"."] stringByAppendingString: className];
    return pluginClassName;
}

@end
