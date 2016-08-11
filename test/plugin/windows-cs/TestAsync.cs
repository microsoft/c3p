// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.C3P.Test
{
    public sealed class TestAsync
    {
        public TestAsync()
        {
        }

        public static IAsyncAction StaticLogAsync(string text, bool fail)
        {
            return TestAsync.StaticLogTaskAsync(text, fail).AsAsyncAction();
        }

        static async Task StaticLogTaskAsync(string text, bool fail)
        {
            await Task.Yield();

            if (fail)
            {
                Debug.WriteLine("TestAsync: Failed to log: " + text);
                throw new Exception("Failed to log: " + text);
            }

            Debug.WriteLine("TestAsync: " + text);
        }

        public static IAsyncOperation<string> StaticEchoAsync(string text, bool fail)
        {
            return TestAsync.StaticEchoTaskAsync(text, fail).AsAsyncOperation();
        }

        static async Task<string> StaticEchoTaskAsync(string text, bool fail)
        {
            await Task.Yield();

            if (fail)
            {
                Debug.WriteLine("TestMethods: Failed to echo: " + text);
                throw new Exception("Failed to echo: " + text);
            }

            return text;
        }

        public static IAsyncOperation<TestStruct> StaticEchoDataAsync(TestStruct data, bool fail)
        {
            return TestAsync.StaticEchoDataTaskAsync(data, fail).AsAsyncOperation();
        }

        static async Task<TestStruct> StaticEchoDataTaskAsync(TestStruct data, bool fail)
        {
            await Task.Yield();

            if (fail)
            {
                Debug.WriteLine("TestMethods: Failed to echo data.");
                throw new Exception("Failed to echo data.");
            }

            return data;
        }

        public IAsyncAction LogAsync(string text, bool fail)
        {
            return this.LogTaskAsync(text, fail).AsAsyncAction();
        }

        async Task LogTaskAsync(string text, bool fail)
        {
            await Task.Yield();

            if (fail)
            {
                Debug.WriteLine("TestAsync: Failed to log: " + text);
                throw new Exception("Failed to log: " + text);
            }

            Debug.WriteLine("TestAsync: ");
        }

        public IAsyncOperation<string> EchoAsync(string text, bool fail)
        {
            return this.EchoTaskAsync(text, fail).AsAsyncOperation();
        }

        async Task<string> EchoTaskAsync(string text, bool fail)
        {
            await Task.Yield();

            if (fail)
            {
                Debug.WriteLine("TestMethods: Failed to echo: " + text);
                throw new Exception("Failed to echo: " + text);
            }

            return text;
        }

        public IAsyncOperation<TestStruct> EchoDataAsync(TestStruct data, bool fail)
        {
            return this.EchoDataTaskAsync(data, fail).AsAsyncOperation();
        }

        async Task<TestStruct> EchoDataTaskAsync(TestStruct data, bool fail)
        {
            await Task.Yield();

            if (fail)
            {
                Debug.WriteLine("TestMethods: Failed to echo data.");
                throw new Exception("Failed to echo data.");
            }

            return data;
        }

        public IAsyncOperation<IList<TestStruct>> EchoDataListAsync(IList<TestStruct> dataList, bool fail)
        {
            return this.EchoDataListTaskAsync(dataList, fail).AsAsyncOperation();
        }

        async Task<IList<TestStruct>> EchoDataListTaskAsync(IList<TestStruct> dataList, bool fail)
        {
            await Task.Yield();

            if (fail)
            {
                Debug.WriteLine("TestMethods: Failed to echo data list.");
                throw new Exception("Failed to echo data list.");
            }

            return dataList;
        }

        public IAsyncOperation<int?> EchoNullableIntAsync(int? intValue)
        {
            return this.EchoNullableIntTaskAsync(intValue).AsAsyncOperation();
        }

        async Task<int?> EchoNullableIntTaskAsync(int? intValue)
        {
            await Task.Yield();

            return intValue;
        }

        public IAsyncOperation<Guid?> EchoUuidAsync(Guid? uuidValue)
        {
            return this.EchoUuidTaskAsync(uuidValue).AsAsyncOperation();
        }

        async Task<Guid?> EchoUuidTaskAsync(Guid? uuidValue)
        {
            await Task.Yield();

            return uuidValue;
        }

        public IAsyncOperation<bool?> EchoNullableBoolAsync(bool? boolValue)
        {
            return this.EchoNullableBoolTaskAsync(boolValue).AsAsyncOperation();
        }

        async Task<bool?> EchoNullableBoolTaskAsync(bool? boolValue)
        {
            await Task.Yield();

            return boolValue;
        }
    }
}