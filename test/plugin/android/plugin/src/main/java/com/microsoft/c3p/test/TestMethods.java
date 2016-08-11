// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import android.util.Log;

import java.util.List;
import java.util.UUID;

/**
 * Tests methods with various parameters and return types, async, and exceptions.
 */
public class TestMethods {
    private static final String TAG = "C3PTest";

    /**
     * Logs the provided string. Throws an exception if the fail parameter is
     * set to true.
     * @param value the string to log
     * @param fail whether a failure should be simulated by throwing a runtime exception
     */
    public static void staticLog(final String value, boolean fail) {
        if (fail) {
            Log.i(TAG, "Failed to log: " + value);
            throw new RuntimeException("Failed to log: " + value);
        }

        Log.i(TAG, value);
    }

    /**
     * Returns the provided string. Throws an exception if the fail parameter is
     * set to true.
     * @param value the string to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return the provided string
     */
    public static String staticEcho(final String value, boolean fail) {
        if (fail) {
            Log.i(TAG, "Failed to echo: " + value);
            throw new RuntimeException("Failed to echo: " + value);
        }

        Log.i(TAG, value);
        return value;
    }

    /**
     * Returns the provided data object. Throws an exception if the fail parameter is
     * set to true.
     * @param data the object to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return the provided object
     */
    public static TestStruct staticEchoData(final TestStruct data, final boolean fail) {
        if (fail) {
            Log.i(TAG, "Failed to echo data");
            throw new RuntimeException("Failed to echo data");
        }

        Log.i(TAG, "(data)");
        return data;
    }

    /**
     * Logs the provided string. Throws an exception if the fail parameter is
     * set to true.
     * @param value the string to log
     * @param fail whether a failure should be simulated by throwing a runtime exception
     */
    public void log(final String value, boolean fail) {
        if (fail) {
            Log.i(TAG, "Failed to log: " + value);
            throw new RuntimeException("Failed to log: " + value);
        }

        Log.i(TAG, value);
    }

    /**
     * Returns the provided string. Throws an exception if the fail parameter is
     * set to true.
     * @param value the string to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return the provided string
     */
    public String echo(final String value, boolean fail) {
        if (fail) {
            Log.i(TAG, "Failed to echo: " + value);
            throw new RuntimeException("Failed to echo: " + value);
        }

        Log.i(TAG, value);
        return value;
    }

    /**
     * Returns the provided data object. Throws an exception if the fail parameter is
     * set to true.
     * @param data the object to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return the provided object
     */
    public TestStruct echoData(final TestStruct data, final boolean fail) {
        if (fail) {
            Log.i(TAG, "Failed to echo data");
            throw new RuntimeException("Failed to echo data");
        }

        Log.i(TAG, "(data)");
        return data;
    }

    /**
     * Returns the provided list of data objects. Throws an exception if the fail parameter
     * is set to true.
     * @param dataList the list of objects to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return the provided object
     */
    public List<TestStruct> echoDataList(final List<TestStruct> dataList, final boolean fail) {
        if (fail) {
            Log.i(TAG, "Failed to echo data list");
            throw new RuntimeException("Failed to echo data list");
        }

        Log.i(TAG, "(data list)");
        return dataList;
    }

    public Integer echoNullableInt(Integer value) {
        return value;
    }

    public Boolean echoNullableBool(Boolean value) {
        return value;
    }

    public UUID echoUuid(UUID value) {
        return value;
    }
}

