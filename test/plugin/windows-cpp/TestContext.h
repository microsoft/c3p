// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#pragma once

namespace Microsoft { namespace VisualStudio { namespace C3P { namespace Test
{

    public ref class TestContext sealed
    {
        // The Windows implementation of all these methods is empty because the C3P Windows platform
        // does not use implicit context parameters. Also, note that the Windows.UI.Xaml.Application.Current
        // and Windows.UI.Xaml.Window.Current properties are not available in Cordova apps, because Cordova
        // apps are WinJS apps. (They don't use the XAML application model.) Generally this shouldn't be a
        // problem for plugins because most WinRT APIs don't require any context.

    public:
        TestContext(bool fail);

        void TestConstructorAppContext();

        static void TestStaticMethodAppContext();

        static void TestStaticMethodAppContext2(int someOtherParam);

        static void TestStaticMethodWindowContext();

        static void TestStaticMethodWindowContext2(int someOtherParam);

        void TestMethodAppContext();

        void TestMethodAppContext2(int someOtherParam);

        void TestMethodWindowContext();

        void TestMethodWindowContext2(int someOtherParam);

        Windows::Foundation::IAsyncAction^ TestMethodAppContext3Async();

        Windows::Foundation::IAsyncAction^ TestMethodAppContext4Async(int someOtherParam);

        Windows::Foundation::IAsyncAction^ TestMethodWindowContext3Async();

        Windows::Foundation::IAsyncAction^ TestMethodWindowContext4Async(int someOtherParam);

        Windows::Foundation::IAsyncAction^ TestAndroidActivityAsync();
    };

}}}}