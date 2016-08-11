// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#pragma once

namespace Microsoft { namespace VisualStudio { namespace C3P { namespace Test
{

    public ref class TestStruct sealed
    {
    public:
        TestStruct();

        TestStruct(Platform::IBox<Windows::Foundation::DateTime>^ initialValue);

        property Platform::IBox<Windows::Foundation::DateTime>^ Value
        {
            Platform::IBox<Windows::Foundation::DateTime>^ get();
            void set(Platform::IBox<Windows::Foundation::DateTime>^);
        }

        void UpdateValue(Platform::IBox<Windows::Foundation::DateTime>^ value);

        Platform::String^ ToXml();

    private:
        Platform::IBox<Windows::Foundation::DateTime>^ value;
    };

}}}}