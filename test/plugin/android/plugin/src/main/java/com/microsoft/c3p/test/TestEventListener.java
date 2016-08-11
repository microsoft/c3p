// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import java.util.EventListener;

public interface TestEventListener extends EventListener {
    void testEventOccurred(TestEvent e);
}
