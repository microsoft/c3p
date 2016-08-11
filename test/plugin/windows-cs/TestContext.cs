// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.C3P.Test
{
    public sealed class TestContext
    {
        // The Windows implementation of all these methods is empty because the C3P Windows platform
        // does not use implicit context parameters. Also, note that the Windows.UI.Xaml.Application.Current
        // and Windows.UI.Xaml.Window.Current properties are not available in Cordova apps, because Cordova
        // apps are WinJS apps. (They don't use the XAML application model.) Generally this shouldn't be a
        // problem for plugins because most WinRT APIs don't require any context.

        public TestContext(bool fail)
        {
            if (fail)
            {
                throw new Exception("Requested failure.");
            }
        }

        public void TestConstructorAppContext()
        {
        }

        public static void TestStaticMethodAppContext()
        {
        }

        public static void TestStaticMethodAppContext2(int someOtherParam)
        {
        }

        public static void TestStaticMethodWindowContext()
        {
        }

        public static void TestStaticMethodWindowContext2(int someOtherParam)
        {
        }

        public void TestMethodAppContext()
        {
        }

        public void TestMethodAppContext2(int someOtherParam)
        {
        }

        public void TestMethodWindowContext()
        {
        }

        public void TestMethodWindowContext2(int someOtherParam)
        {
        }

        public IAsyncAction TestMethodAppContext3Async()
        {
            return this.TestMethodAppContext3TaskAsync().AsAsyncAction();
        }

        async Task TestMethodAppContext3TaskAsync()
        {
            await Task.Yield();
        }

        public IAsyncAction TestMethodAppContext4Async(int someOtherParam)
        {
            return this.TestMethodAppContext4TaskAsync(someOtherParam).AsAsyncAction();
        }

        async Task TestMethodAppContext4TaskAsync(int someOtherParam)
        {
            await Task.Yield();
        }

        public IAsyncAction TestMethodWindowContext3Async()
        {
            return this.TestMethodWindowContext3TaskAsync().AsAsyncAction();
        }

        async Task TestMethodWindowContext3TaskAsync()
        {
            await Task.Yield();
        }

        public IAsyncAction TestMethodWindowContext4Async(int someOtherParam)
        {
            return this.TestMethodWindowContext4TaskAsync(someOtherParam).AsAsyncAction();
        }

        async Task TestMethodWindowContext4TaskAsync(int someOtherParam)
        {
            await Task.Yield();
        }

        public IAsyncAction TestAndroidActivityAsync() {
            return this.TestAndroidActivityTaskAsync().AsAsyncAction();
        }

        async Task TestAndroidActivityTaskAsync() {
            // This test case is Android-only, so the implementation here is a no-op.
            await Task.Yield();
        }
    }
}
