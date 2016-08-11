// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.util;

import java.util.concurrent.CancellationException;

/**
 * A promise that supports chaining via "then" methods.
 * @param <V> The type of the promised result, or java.lang.Void for a void promise.
 */
public class ChainablePromise<V> extends Promise<V> {
    private Function<V, ?> resultHandler;
    private Consumer<Exception> exceptionHandler;
    private Promise<?> continuation;

    /**
     * Creates a new chainable promise that can be resolved or rejected later.
     */
    public ChainablePromise() {
        super();
    }

    /**
     * Creates a new chainable promise that is already resolved with a result.
     */
    public ChainablePromise(V result) {
        super(result);
    }

    /**
     * Creates a new chainable promise that is already rejected with an exception.
     */
    public ChainablePromise(Exception exception) {
        super(exception);
    }

    /**
     * Cancels a promise. After a promise is successfully cancelled, attempting to get the
     * result will throw a CancellationException. If there is a chained result handler, it will
     * not be invoked; if there is a chained exception handler, it will be invoked with a
     * CancellationException.
     * @param mayInterruptIfRunning Not supported, must be false.
     * @return False if the promise is already resolved or rejected and could not be cancelled,
     *         or true if the promise was successfully cancelled before it was resolved or rejected.
     *         (The task providing the promise may not have actually been interrupted, though any
     *         eventual resolution or rejection will be ignored.)
     */
    @Override
    public boolean cancel(boolean mayInterruptIfRunning) {
        if (!super.cancel(mayInterruptIfRunning)) {
            return false;
        }

        this.invokeContinuation();
        return true;
    }

    /**
     * Resolves the promise.
     * @param result The promise result, or null if this is a void promise.
     */
    @Override
    public void resolve(V result) {
        super.resolve(result);

        this.invokeContinuation();
    }

    /**
     * Rejects the promise.
     * @param exception The exception that caused the rejection (required).
     */
    @Override
    public void reject(Exception exception) {
        super.reject(exception);

        this.invokeContinuation();
    }

    /**
     * Creates a chained promise by specifying a result handler to be invoked when the promise
     * is resolved.
     * @param resultHandler Consumer to be invoked when the promise is resolved.
     * @return A new promise that will be resolved after the result handler has completed.
     */
    public ChainablePromise<Void> then(Consumer<V> resultHandler) {
        return this.then(resultHandler, (Consumer<Exception>)null);
    }

    /**
     * Creates a chained promise by specifying a result handler and exception handler to be invoked
     * when the promise is done.
     * @param resultHandler Consumer to be invoked when the promise is resolved.
     * @param exceptionHandler Consumer to be invoked when the promise is rejected or cancelled. The
     *        parameter to the action is either the rejection exception or a CancellationException.
     * @return A new promise that will be resolved after the result handler has completed.
     */
    public ChainablePromise<Void> then(
            final Consumer<V> resultHandler, Consumer<Exception> exceptionHandler) {
        return this.then(
                resultHandler == null ? null : new Function<V, Void>() {
                    @Override
                    public Void apply(V result) {
                        resultHandler.accept(result);
                        return null;
                    }
                },
                exceptionHandler);
    }

    /**
     * Creates a chained promise by specifying a result handler to be invoked when the promise
     * is resolved.
     * @param <T> The type returned by the result handler, which becomes the result type of the
     *        chained promise.
     * @param resultHandler Function to be invoked when the promise is resolved. The return value
     *        of this function becomes the result of the chained promise.
     * @return A new promise that will be resolved with the value returned by the result handler.
     */
    public <T> ChainablePromise<T> then(Function<V, T> resultHandler) {
        return this.then(resultHandler, (Consumer<Exception>)null);
    }

    /**
     * Creates a chained promise by specifying a result handler and exception handler to be invoked
     * when the promise is done.
     * @param <T> The type returned by the result handler, which becomes the result type of the
     *        chained promise.
     * @param resultHandler Function to be invoked when the promise is resolved. The return value
     *        of this function becomes the result of the chained promise.
     * @param exceptionHandler Consumer to be invoked when the promise is rejected or cancelled. The
     *        parameter to the action is either the rejection exception or a CancellationException.
     * @return A new promise that will be resolved with the value returned by the result handler.
     */
    public <T> ChainablePromise<T> then(
            Function<V, T> resultHandler, Consumer<Exception> exceptionHandler) {
        ChainablePromise<T> chainedPromise;

        synchronized (this) {
            if (this.continuation != null) {
                throw new IllegalStateException("Another promise is already chained to this one.");
            }

            chainedPromise = new ChainablePromise<T>();
            this.continuation = chainedPromise;
            this.resultHandler = resultHandler;
            this.exceptionHandler = exceptionHandler;
        }

        this.invokeContinuation();

        return chainedPromise;
    }

    /**
     * Creates a chained promise by specifying an exception handler to be invoked when the promise
     * is rejected or cancelled.
     * @param exceptionHandler Consumer to be invoked when the promise is rejected or cancelled. The
     *        parameter to the action is either the rejection exception or a CancellationException.
     * @return A new promise that will never be resolved, but may be rejected with an exception
     * that occurred somewhere in the chain.
     */
    public ChainablePromise<Void> thenCatch(Consumer<Exception> exceptionHandler) {
        return this.then((Function<V, Void>)null, exceptionHandler);
    }

    /**
     * Invokes any continuation (chained result handler or exception handler) if the promise is done
     * and a continuation has not already been invoked.
     */
    private void invokeContinuation() {
        Function<V, ?> continueWithResultHandler;
        Consumer<Exception> continueWithExceptionHandler;

        synchronized (this) {
            if (!(this.isDone() || this.isCancelled()) || this.continuation == null) {
                // There's no continuation to invoke (right now).
                return;
            } else if (this.resultHandler == null && this.exceptionHandler == null) {
                // The continuation was already invoked.
                return;
            }

            continueWithResultHandler = this.resultHandler;
            continueWithExceptionHandler = this.exceptionHandler;

            // Set these to null to avoid invoking the continuation more than once.
            this.resultHandler = null;
            this.exceptionHandler = null;
        }

        if (this.isCancelled()) {
            if (continueWithExceptionHandler != null) {
                continueWithExceptionHandler.accept(new CancellationException());
            } else {
                this.continuation.cancel(false);
            }
        } else if (this.exception != null) {
            if (continueWithExceptionHandler != null) {
                continueWithExceptionHandler.accept(this.exception);
            } else {
                this.continuation.reject(this.exception);
            }
        } else if (continueWithResultHandler != null) {
            Object nextResult = continueWithResultHandler.apply(this.result);
            ((Promise<Object>)this.continuation).resolve(nextResult);
        }
    }
}
