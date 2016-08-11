// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#include "pch.h"
#include "TestEnum.h"
#include "TestStruct.h"
#include "TestAsync.h"

using namespace Microsoft::VisualStudio::C3P::Test;

TestAsync::TestAsync()
{
}

Windows::Foundation::IAsyncAction^ TestAsync::StaticLogAsync(Platform::String^ text, bool fail)
{
    return concurrency::create_async([text, fail]
    {
        if (fail)
        {
            throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
        }

        OutputDebugString(text->Data());
    });
}

Windows::Foundation::IAsyncOperation<Platform::String^>^ TestAsync::StaticEchoAsync(
    Platform::String^ text, bool fail)
{
    return concurrency::create_async([text, fail]
    {
        if (fail)
        {
            throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
        }

        return text;
    });
}

Windows::Foundation::IAsyncOperation<TestStruct^>^ TestAsync::StaticEchoDataAsync(TestStruct^ data, bool fail)
{
    return concurrency::create_async([data, fail]
    {
        if (fail)
        {
            throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
        }

        return data;
    });
}

Windows::Foundation::IAsyncAction^ TestAsync::LogAsync(Platform::String^ text, bool fail)
{
    return concurrency::create_async([text, fail]
    {
        if (fail)
        {
            throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
        }

        OutputDebugString(text->Data());
    });
}

Windows::Foundation::IAsyncOperation<Platform::String^>^ TestAsync::EchoAsync(Platform::String^ text, bool fail)
{
    return concurrency::create_async([text, fail]
    {
        if (fail)
        {
            throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
        }

        return text;
    });
}

Windows::Foundation::IAsyncOperation<TestStruct^>^ TestAsync::EchoDataAsync(TestStruct^ data, bool fail)
{
    return concurrency::create_async([data, fail]
    {
        if (fail)
        {
            throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
        }

        return data;
    });
}

Windows::Foundation::IAsyncOperation<Windows::Foundation::Collections::IVector<TestStruct^>^>^
TestAsync::EchoDataListAsync(
    Windows::Foundation::Collections::IVector<TestStruct^>^ dataList, bool fail)
{
    // Note collection objects passed in from script callers cannot be accessed on a different thread.
    Windows::Foundation::Collections::IVector<TestStruct^>^ dataListCopy =
        ref new Platform::Collections::Vector<TestStruct^>();
    for (TestStruct^ value : dataList)
    {
        dataListCopy->Append(value);
    }

    return concurrency::create_async([dataListCopy, fail]
    {
        if (fail)
        {
            throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
        }

        return dataListCopy;
    });
}

Windows::Foundation::IAsyncOperation<Platform::IBox<int>^>^ TestAsync::EchoNullableIntAsync(
    Platform::IBox<int>^ intValue)
{
    return concurrency::create_async([intValue]
    {
        return intValue;
    });
}

Windows::Foundation::IAsyncOperation<Platform::IBox<Platform::Guid>^>^ TestAsync::EchoUuidAsync(
    Platform::IBox<Platform::Guid>^ uuidValue)
{
    return concurrency::create_async([uuidValue]
    {
        return uuidValue;
    });
}

Windows::Foundation::IAsyncOperation<Platform::IBox<bool>^>^ TestAsync::EchoNullableBoolAsync(
    Platform::IBox<bool>^ boolValue)
{
    return concurrency::create_async([boolValue]
    {
        return boolValue;
    });
}
