// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#import <UIKit/UIKit.h>

/// Provides access to the application context necessary for marshalling calls over the JS bridge.
@protocol C3PApplicationContext <NSObject>

- (UIApplication*) getApplication;

- (UIWindow*) getCurrentWindow;

@end
