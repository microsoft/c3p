// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import android.app.Activity;
import android.app.Application;
import android.content.Intent;

import java.util.concurrent.Callable;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

public class TestContext {
    private Application _appContext;
    private final ExecutorService _executor;
    private Promise<Void> _activityPromise;

    public TestContext(Application appContext, boolean fail) {
        if (fail) {
            throw new RuntimeException("Requested failure.");
        }

        _appContext = appContext;
        _executor = Executors.newSingleThreadExecutor();
    }

    public void testConstructorAppContext() {
        if (_appContext == null) {
            throw new IllegalStateException("Constructor app context is null!");
        }
    }

    public static void testStaticMethodAppContext(Application appContext) {
        if (appContext == null) {
            throw new IllegalStateException("Static method app context is null!");
        }
    }

    public static void testStaticMethodAppContext2(Application appContext, int someOtherParam) {
        if (appContext == null) {
            throw new IllegalStateException("Static method app context is null!");
        }
    }

    public static void testStaticMethodWindowContext(Activity windowContext) {
        if (windowContext == null) {
            throw new IllegalStateException("Static method window context is null!");
        }
    }

    public static void testStaticMethodWindowContext2(Activity windowContext, int someOtherParam) {
        if (windowContext == null) {
            throw new IllegalStateException("Static method window context is null!");
        }
    }

    public void testMethodAppContext(Application appContext) {
        if (appContext == null) {
            throw new IllegalStateException("Method app context is null!");
        }
    }

    public void testMethodAppContext2(Application appContext, int someOtherParam) {
        if (appContext == null) {
            throw new IllegalStateException("Method app context is null!");
        }
    }

    public void testMethodWindowContext(Activity windowContext) {
        if (windowContext == null) {
            throw new IllegalStateException("Method window context is null!");
        }
    }

    public void testMethodWindowContext2(Activity windowContext, int someOtherParam) {
        if (windowContext == null) {
            throw new IllegalStateException("Method window context is null!");
        }
    }

    public Future<Void> testMethodAppContext3Async(final Application appContext) {
        final TestContext self = this;
        return _executor.submit(new Callable<Void>() {
            public Void call() {
                self.testMethodAppContext(appContext);
                return null;
            }});
    }

    public Future<Void> testMethodAppContext4Async(
            final Application appContext, final int someOtherParam) {
        final TestContext self = this;
        return _executor.submit(new Callable<Void>() {
            public Void call() {
                self.testMethodAppContext2(appContext, someOtherParam);
                return null;
            }});
    }

    public Future<Void> testMethodWindowContext3Async(final Activity windowContext) {
        final TestContext self = this;
        return _executor.submit(new Callable<Void>() {
            public Void call() {
                self.testMethodWindowContext(windowContext);
                return null;
            }});
    }

    public Future<Void> testMethodWindowContext4Async(
            final Activity windowContext, final int someOtherParam) {
        final TestContext self = this;
        return _executor.submit(new Callable<Void>() {
            public Void call() {
                self.testMethodWindowContext2(windowContext, someOtherParam);
                return null;
            }});
    }

    public Future<Void> testAndroidActivityAsync(final Activity windowContext) {
        if (_activityPromise != null) {
            throw new IllegalStateException("An activity test is currently in progress.");
        }

        _activityPromise = new Promise<Void>();
        Intent testIntent = new Intent();
        testIntent.setClassName(windowContext.getPackageName(), TestActivity.class.getName());
        windowContext.startActivityForResult(testIntent, 111);
        return _activityPromise;
    }

    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == 111 &&_activityPromise != null) {
            _activityPromise.resolve(null);
            _activityPromise = null;
        }
    }

    public static class TestActivity extends Activity {
        @Override
        public void onResume() {
            super.onResume();

            this.setResult(Activity.RESULT_OK, null);
            this.finish();
        }
    }
}
