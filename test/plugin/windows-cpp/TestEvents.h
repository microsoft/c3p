// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#pragma once

namespace Microsoft { namespace VisualStudio { namespace C3P { namespace Test
{

    public ref class TestEvent sealed
    {
    internal:
        TestEvent(int counter);

    public:
        property int Counter
        {
            int get();
        }

    private:
        int counter;
    };

    public ref class TestEvents sealed
    {
    public:
        TestEvents();

        static event Windows::Foundation::EventHandler<TestEvent^>^ StaticEvent;

        static void RaiseStaticEvent();

        event Windows::Foundation::EventHandler<TestEvent^>^ InstanceEvent;

        void RaiseInstanceEvent();

    private:
        static int staticCounter;
        int instanceCounter;
    };

}}}}
