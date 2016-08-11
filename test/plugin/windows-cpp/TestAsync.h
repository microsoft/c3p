// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#pragma once

namespace Microsoft { namespace VisualStudio { namespace C3P { namespace Test
{
    ref class TestStruct;

    public ref class TestAsync sealed
    {
    public:
        TestAsync();

        static Windows::Foundation::IAsyncAction^ StaticLogAsync(Platform::String^ text, bool fail);

        static Windows::Foundation::IAsyncOperation<Platform::String^>^ StaticEchoAsync(
            Platform::String^ text, bool fail);

        static Windows::Foundation::IAsyncOperation<TestStruct^>^ StaticEchoDataAsync(TestStruct^ data, bool fail);

        Windows::Foundation::IAsyncAction^ LogAsync(Platform::String^ text, bool fail);

        Windows::Foundation::IAsyncOperation<Platform::String^>^ EchoAsync(Platform::String^ text, bool fail);

        Windows::Foundation::IAsyncOperation<TestStruct^>^ EchoDataAsync(TestStruct^ data, bool fail);

        Windows::Foundation::IAsyncOperation<Windows::Foundation::Collections::IVector<TestStruct^>^>^
        EchoDataListAsync(
            Windows::Foundation::Collections::IVector<TestStruct^>^ dataList, bool fail);

        Windows::Foundation::IAsyncOperation<Platform::IBox<int>^>^ EchoNullableIntAsync(
            Platform::IBox<int>^ intValue);

        Windows::Foundation::IAsyncOperation<Platform::IBox<Platform::Guid>^>^ EchoUuidAsync(
            Platform::IBox<Platform::Guid>^ uuidValue);

        Windows::Foundation::IAsyncOperation<Platform::IBox<bool>^>^ EchoNullableBoolAsync(
            Platform::IBox<bool>^ boolValue);
    };

}}}}