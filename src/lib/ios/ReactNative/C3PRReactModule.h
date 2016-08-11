// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <Foundation/Foundation.h>
#import <RCTBridgeModule.h>
#import "C3PApplicationContext.h"


/// A React Native module that enables other React Native modules to easily bridge between
/// JavaScript and Obj-C code.
@interface C3PRReactModule : NSObject <RCTBridgeModule, C3PApplicationContext>

@end
