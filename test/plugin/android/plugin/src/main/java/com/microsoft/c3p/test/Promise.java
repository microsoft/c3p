// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import android.os.ConditionVariable;

import java.util.concurrent.CancellationException;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

/**
 * Basic promise object that can be resolved with a result or rejected with an exception.
 * @param <V> The type of the promised result, or java.lang.Void for a void promise.
 */
class Promise<V> implements Future<V> {
    private boolean cancelled;
    private boolean done;
    private ConditionVariable condition;
    protected V result;
    protected Exception exception;

    /**
     * Creates a new promise that can be resolved or rejected later.
     */
    public Promise() {
        this.condition = new ConditionVariable(false);
    }

    /**
     * Creates a new promise that is already resolved with a result.
     */
    public Promise(V result) {
        this.result = result;
        this.done = true;
        this.condition = new ConditionVariable(true);
    }

    /**
     * Creates a new promise that is already rejected with an exception.
     */
    public Promise(Exception exception) {
        this.exception = exception;
        this.done = true;
        this.condition = new ConditionVariable(true);
    }

    /**
     * Checks if the promise is cancelled.
     */
    @Override
    public boolean isCancelled() {
        return this.cancelled;
    }

    /**
     * Checks if the promise is done (either resolved or rejected).
     */
    @Override
    public boolean isDone() {
        return this.done;
    }

    /**
     * Cancels a promise. After a promise is successfully cancelled, attempting to get the
     * result will throw a CancellationException.
     * @param mayInterruptIfRunning Not supported, must be false.
     * @return False if the promise is already resolved or rejected and could not be cancelled,
     *         or true if the promise was successfully cancelled before it was resolved or rejected.
     *         (The task providing the promise may not have actually been interrupted, though any
     *         eventual resolution or rejection will be ignored.)
     */
    @Override
    public boolean cancel(boolean mayInterruptIfRunning) {
        if (mayInterruptIfRunning) {
            throw new UnsupportedOperationException("Interruption is not supported.");
        }

        synchronized (this) {
            if (this.done)
            {
                return false;
            }
            else
            {
                this.cancelled = true;
                this.condition.open();
            }
        }

        return true;
    }

    /**
     * Synchronously waits for the promise to be resolved (or rejected or cancelled).
     * @return The resolved result, or null if this is a void promise.
     * @throws InterruptedException If the waiting thread was interrupted.
     * @throws CancellationException If the promise was cancelled before it was resolved.
     * @throws ExecutionException If the promise was rejected. The rejection exception is the
     *         ExecutionException's cause.
     */
    @Override
    public V get() throws InterruptedException, ExecutionException {
        this.condition.block();

        synchronized (this) {
            if (this.cancelled) {
                throw new CancellationException();
            } else if (this.exception != null) {
                throw new ExecutionException(exception);
            } else {
                return this.result;
            }
        }
    }

    /**
     * Synchronously waits for the promise to be resolved (or rejected or cancelled), up to
     * a specified timeout.
     * @param timeout The maximum length of time to wait.
     * @param unit The unit of the timeout value.
     * @return The resolved result, or null if this is a void promise.
     * @throws InterruptedException If the waiting thread was interrupted.
     * @throws TimeoutException If the specified timeout expired before the promise was resolved
     *         or rejected.
     * @throws CancellationException If the promise was cancelled before it was resolved.
     * @throws ExecutionException If the promise was rejected. The rejection exception is the
     *         ExecutionException's cause.
     */
    @Override
    public V get(long timeout, TimeUnit unit)
            throws InterruptedException, ExecutionException, TimeoutException {
        this.condition.block(unit.toMillis(timeout));

        synchronized (this) {
            if (this.cancelled) {
                throw new CancellationException();
            } else if (this.exception != null) {
                throw new ExecutionException(exception);
            } else {
                return this.result;
            }
        }
    }

    /**
     * Resolves the promise.
     * @param result The promise result, or null if this is a void promise.
     */
    public void resolve(V result) {
        synchronized (this) {
            if (this.done) {
                throw new IllegalStateException("Cannot resolve a promise that is already done.");
            }

            this.result = result;
            this.done = true;
            this.condition.open();
        }
    }

    /**
     * Rejects the promise.
     * @param exception The exception that caused the rejection (required).
     */
    public void reject(Exception exception) {
        if (exception == null) {
            throw new IllegalArgumentException(
                    "An exception is required when rejecting a promise.");
        }

        synchronized (this) {
            if (this.done) {
                throw new IllegalStateException("Cannot reject a promise that is already done.");
            }

            this.exception = exception;
            this.done = true;
            this.condition.open();
        }
    }
}
