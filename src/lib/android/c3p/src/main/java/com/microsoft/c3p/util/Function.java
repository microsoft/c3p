// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.util;

/**
 * Equivalent to java.util.function.Function from Java 8.
 * @param <V> parameter (value) type
 * @param <T> return type
 */
public interface Function<V, T> {
    T apply(V value);
}
