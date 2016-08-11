// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import android.util.Log;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.Callable;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

public class TestAsync {
    private static final String TAG = "C3PTest";

    private static final ExecutorService _staticExecutor =
            Executors.newSingleThreadExecutor();
    private final ExecutorService _executor;

    public TestAsync() {
        _executor = Executors.newSingleThreadExecutor();
    }

    /**
     * Logs the provided string asynchronously. Throws an exception asynchronously
     * if the fail parameter is set to true.
     * @param value the string to log
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return a future for the requested operation
     */
    public static Future<Void> staticLogAsync(final String value, final boolean fail) {
        return _staticExecutor.submit(new Callable<Void>() {
            public Void call() {
                if (fail) {
                    Log.i(TAG, "Failed to log: " + value);
                    throw new RuntimeException("Failed to log: " + value);
                }

                Log.i(TAG, value);
                return null;
            }});
    }

    /**
     * Returns the provided string asynchronously. Throws an exception asynchronously
     * if the fail parameter is set to true.
     * @param value the string to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return a future for the provided string
     */
    public static Future<String> staticEchoAsync(final String value, final boolean fail) {
        return _staticExecutor.submit(new Callable<String>() {
            public String call() {
                if (fail) {
                    Log.i(TAG, "Failed to echo: " + value);
                    throw new RuntimeException("Failed to echo: " + value);
                }

                Log.i(TAG, value);
                return value;
            }});
    }

    /**
     * Returns the provided data object asynchronously. Throws an exception
     * asynchronously if the fail parameter is set to true.
     * @param data the object to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return a future for the provided object
     */
    public static Future<TestStruct> staticEchoDataAsync(
            final TestStruct data, final boolean fail) {
        return _staticExecutor.submit(new Callable<TestStruct>() {
            public TestStruct call() {
                if (fail) {
                    Log.i(TAG, "Failed to echo data");
                    throw new RuntimeException("Failed to echo data");
                }

                Log.i(TAG, "(data)");
                return data;
            }});
    }

    /**
     * Logs the provided string asynchronously. Throws an exception asynchronously
     * if the fail parameter is set to true.
     * @param value the string to log
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return a future for the requested operation
     */
    public Future<Void> logAsync(final String value, final boolean fail) {
        final TestAsync self = this;
        return _executor.submit(new Callable<Void>() {
            public Void call() {
                if (fail) {
                    Log.i(TAG, "Failed to log: " + value);
                    throw new RuntimeException("Failed to log: " + value);
                }

                Log.i(TAG, value);
                return null;
            }});
    }

    /**
     * Returns the provided string asynchronously. Throws an exception asynchronously
     * if the fail parameter is set to true.
     * @param value the string to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return a future for the provided string
     */
    public Future<String> echoAsync(final String value, final boolean fail) {
        final TestAsync self = this;
        return _executor.submit(new Callable<String>() {
            public String call() {
                if (fail) {
                    Log.i(TAG, "Failed to echo: " + value);
                    throw new RuntimeException("Failed to echo: " + value);
                }

                Log.i(TAG, value);
                return value;
            }});
    }

    /**
     * Returns the provided data object asynchronously. Throws an exception
     * asynchronously if the fail parameter is set to true.
     * @param data the object to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return a future for the provided object
     */
    public Future<TestStruct> echoDataAsync(final TestStruct data, final boolean fail) {
        final TestAsync self = this;
        return _executor.submit(new Callable<TestStruct>() {
            public TestStruct call() {
                if (fail) {
                    Log.i(TAG, "Failed to echo data");
                    throw new RuntimeException("Failed to echo data");
                }

                Log.i(TAG, "(data)");
                return data;
            }});
    }

    /**
     * Returns the provided list of data objects asynchronously. Throws an exception
     * asynchronously if the fail parameter is set to true.
     * @param dataList the list of objects to return
     * @param fail whether a failure should be simulated by throwing a runtime exception
     * @return a future for the provided object
     */
    public Future<List<TestStruct>> echoDataListAsync(
            final List<TestStruct> dataList, final boolean fail) {
        final TestAsync self = this;
        return _executor.submit(new Callable<List<TestStruct>>() {
            public List<TestStruct> call() {
                if (fail) {
                    Log.i(TAG, "Failed to echo data list");
                    throw new RuntimeException("Failed to echo data list");
                }

                Log.i(TAG, "(data list)");
                return dataList;
            }});
    }

    public Future<Integer> echoNullableIntAsync(final Integer value) {
        final TestAsync self = this;
        return _executor.submit(new Callable<Integer>() {
            public Integer call() {
                return value;
            }});
    }

    public Future<Boolean> echoNullableBoolAsync(final Boolean value) {
        final TestAsync self = this;
        return _executor.submit(new Callable<Boolean>() {
            public Boolean call() {
                return value;
            }});
    }

    public Future<UUID> echoUuidAsync(final UUID value) {
        final TestAsync self = this;
        return _executor.submit(new Callable<UUID>() {
            public UUID call() {
                return value;
            }});
    }
}
