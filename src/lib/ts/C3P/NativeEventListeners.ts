// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

import { NativeReference } from "./NativeObject";

/**
 * One item in an EventListenersCollection.
 */
export class EventListenerRecord {
    constructor(
        public sender: (string | NativeReference),
        public event: string,
        public listener: (e: any) => void,
        public token: string,
        public subscription: any) { }

    matches(
        sender: (string | NativeReference),
        event: string,
        listener: (e: any) => void): boolean {
        return this.sender === sender &&
            this.event === event &&
            this.listener === listener;
    }
}

/**
 * Utility class for managing a collection of event listeners and tracking their subscriptions and tokens.
 */
export class EventListenersCollection {
    private listeners = new Array<EventListenerRecord>();

    add(
        sender: (string | NativeReference),
        event: string,
        listener: (e: any) => void,
        token: string,
        subscription: any): void {
        this.listeners.push(new EventListenerRecord(sender, event, listener, token, subscription));
    }

    remove(
        sender: (string | NativeReference),
        event: string,
        listener: (e: any) => void,
        token: string): boolean {
        for (var i = 0; i < this.listeners.length; i++) {
            if (this.listeners[i].matches(sender, event, listener) &&
                this.listeners[i].token == token) {
                this.listeners.splice(i, 1);
                return true;
            }
        }
        return false;
    }

    find(
        sender: (string | NativeReference),
        event: string,
        listener: (e: any) => void): (EventListenerRecord | null) {
        for (var i = 0; i < this.listeners.length; i++) {
            if (this.listeners[i].matches(sender, event, listener)) {
                return this.listeners[i];
            }
        }
        return null;
    }
}
