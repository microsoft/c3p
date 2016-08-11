// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p;

import android.app.Activity;
import android.app.Application;

/**
 * Provides access to the application context necessary for marshalling calls over the JS bridge.
 */
public interface JavaScriptApplicationContext {
    Application getApplication();
    Activity getCurrentActivity();
    void interceptActivityResults();
}
