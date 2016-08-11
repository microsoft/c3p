// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#include "pch.h"
#include "TestEvents.h"

using namespace Microsoft::VisualStudio::C3P::Test;

TestEvent::TestEvent(int counter)
{
    this->counter = counter;
}

int TestEvent::Counter::get()
{
    return this->counter;
}

int TestEvents::staticCounter = 0;

TestEvents::TestEvents()
{
}

void TestEvents::RaiseStaticEvent()
{
    TestEvent^ e = ref new TestEvent(++TestEvents::staticCounter);
    TestEvents::StaticEvent(nullptr, e);
}

void TestEvents::RaiseInstanceEvent()
{
    TestEvent^ e = ref new TestEvent(++this->instanceCounter);
    this->InstanceEvent(this, e);
}
