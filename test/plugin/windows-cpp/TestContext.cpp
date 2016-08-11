// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#include "pch.h"
#include "TestContext.h"

using namespace Microsoft::VisualStudio::C3P::Test;

TestContext::TestContext(bool fail)
{
    if (fail)
    {
        throw ref new Platform::Exception(E_FAIL, Platform::StringReference(L"Requested failure."));
    }
}

void TestContext::TestConstructorAppContext()
{
}

void TestContext::TestStaticMethodAppContext()
{
}

void TestContext::TestStaticMethodAppContext2(int someOtherParam)
{
}

void TestContext::TestStaticMethodWindowContext()
{
}

void TestContext::TestStaticMethodWindowContext2(int someOtherParam)
{

}

void TestContext::TestMethodAppContext()
{
}

void TestContext::TestMethodAppContext2(int someOtherParam)
{
}

void TestContext::TestMethodWindowContext()
{
}

void TestContext::TestMethodWindowContext2(int someOtherParam)
{
}

Windows::Foundation::IAsyncAction^ TestContext::TestMethodAppContext3Async()
{
    return concurrency::create_async([]{});
}

Windows::Foundation::IAsyncAction^ TestContext::TestMethodAppContext4Async(int someOtherParam)
{
    return concurrency::create_async([]{});
}

Windows::Foundation::IAsyncAction^ TestContext::TestMethodWindowContext3Async()
{
    return concurrency::create_async([]{});
}

Windows::Foundation::IAsyncAction^ TestContext::TestMethodWindowContext4Async(int someOtherParam)
{
    return concurrency::create_async([]{});
}

Windows::Foundation::IAsyncAction^ TestContext::TestAndroidActivityAsync()
{
    // This test case is Android-only, so the implementation here is a no-op.
    return concurrency::create_async([]{});
}

