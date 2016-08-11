// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#include "pch.h"
#include "TestStruct.h"
#include "TestMethods.h"

using namespace Microsoft::VisualStudio::C3P::Test;

TestMethods::TestMethods()
{
}

void TestMethods::StaticLog(Platform::String^ text, bool fail)
{
    if (fail)
    {
        throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
    }

    OutputDebugString(text->Data());
}

Platform::String^ TestMethods::StaticEcho(Platform::String^ text, bool fail)
{
    if (fail)
    {
        throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
    }

    return text;
}

TestStruct^ TestMethods::StaticEchoData(TestStruct^ data, bool fail)
{
    if (fail)
    {
        throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
    }

    return data;
}

void TestMethods::Log(Platform::String^ text, bool fail)
{
    if (fail)
    {
        throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
    }

    OutputDebugString(text->Data());
}

Platform::String^ TestMethods::Echo(Platform::String^ text, bool fail)
{
    if (fail)
    {
        throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
    }

    return text;
}

TestStruct^ TestMethods::EchoData(TestStruct^ data, bool fail)
{
    if (fail)
    {
        throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
    }

    return data;
}

Windows::Foundation::Collections::IVector<TestStruct^>^ TestMethods::EchoDataList(
    Windows::Foundation::Collections::IVector<TestStruct^>^ dataList, bool fail)
{
    if (fail)
    {
        throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
    }

    Windows::Foundation::Collections::IVector<TestStruct^>^ dataListCopy =
        ref new Platform::Collections::Vector<TestStruct^>();
    for (TestStruct^ value : dataList)
    {
        dataListCopy->Append(value);
    }

    return dataListCopy;
}

Platform::IBox<int>^ TestMethods::EchoNullableInt(Platform::IBox<int>^ intValue)
{
    return intValue;
}

Platform::IBox<Platform::Guid>^ TestMethods::EchoUuid(Platform::IBox<Platform::Guid>^ uuidValue)
{
    return uuidValue;
}

Platform::IBox<bool>^ TestMethods::EchoNullableBool(Platform::IBox<bool>^ boolValue)
{
    return boolValue;
}
