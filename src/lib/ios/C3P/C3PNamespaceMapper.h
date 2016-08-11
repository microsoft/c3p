// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>


/// Handles mapping mappings between JavaScript namespaces and Java packages, and classes within
/// them. While the JavaScript language technically doesn't have namespaces, the JavaScript bridge
/// here enforces namespace semantics to avoid naming collisions among multiple libraries.
@interface C3PNamespaceMapper : NSObject

- (C3PNamespaceMapper*) init;

- (void) registerPluginNamespace: (NSString*) pluginNamespace
                   forObjCPrefix: (NSString*) objCPrefix;

- (NSString*) getObjCPrefixForPluginNamespace: (NSString*) pluginNamespace;

- (NSString*) getPluginNamespaceForObjCPrefix: (NSString*) objCPrefix;

- (NSString*) getObjCClassForPluginClass: (NSString*) pluginClassFullName;

- (NSString*) getPluginClassForObjCClass: (NSString*) objCClassName;

@end
