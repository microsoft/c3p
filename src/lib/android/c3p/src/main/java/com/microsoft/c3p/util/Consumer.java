// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.util;

/**
 * Equivalent to java.util.function.Consumer from Java 8.
 * @param <V> value type
 */
public interface Consumer<V> {
    void accept(V value);
}
