// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.js;

import java.util.Map;

/**
 * Interface for reading the type and value(s) of an item that was serialized from JavaScript.
 */
public interface JavaScriptValue {
    JavaScriptType getType();
    boolean getBoolean();
    int getInteger();
    long getLong();
    double getDouble();
    String getString();
    Iterable<String> getObjectKeys();
    JavaScriptValue getObjectValue(String key);
    Iterable<Map.Entry<String, JavaScriptValue>> getObjectEntries();
    int getArrayLength();
    JavaScriptValue getArrayItem(int index);
    Iterable<JavaScriptValue> getArrayItems();
}
