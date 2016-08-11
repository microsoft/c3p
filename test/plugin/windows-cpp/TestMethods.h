// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#pragma once

namespace Microsoft { namespace VisualStudio { namespace C3P { namespace Test
{
    ref class TestStruct;

    public ref class TestMethods sealed
    {
    public:
        TestMethods();

        static void StaticLog(Platform::String^ text, bool fail);

        static Platform::String^ StaticEcho(Platform::String^ text, bool fail);

        static TestStruct^ StaticEchoData(TestStruct^ data, bool fail);

        void Log(Platform::String^ text, bool fail);

        Platform::String^ Echo(Platform::String^ text, bool fail);

        TestStruct^ EchoData(TestStruct^ data, bool fail);

        Windows::Foundation::Collections::IVector<TestStruct^>^ EchoDataList(
            Windows::Foundation::Collections::IVector<TestStruct^>^ dataList, bool fail);

        Platform::IBox<int>^ EchoNullableInt(Platform::IBox<int>^ intValue);

        Platform::IBox<Platform::Guid>^ EchoUuid(Platform::IBox<Platform::Guid>^ uuidValue);

        Platform::IBox<bool>^ EchoNullableBool(Platform::IBox<bool>^ boolValue);
    };

}}}}