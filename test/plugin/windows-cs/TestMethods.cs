// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.C3P.Test
{
    public sealed class TestMethods
    {
        public static void StaticLog(string text, bool fail)
        {
            if (fail)
            {
                Debug.WriteLine("TestMethods: Failed to log: " + text);
                throw new Exception("Failed to log: " + text);
            }

            Debug.WriteLine("TestMethods: logging: " + text);
        }

        public static String StaticEcho(string text, bool fail)
        {
            if (fail)
            {
                Debug.WriteLine("TestMethods: Failed to echo: " + text);
                throw new Exception("Failed to echo: " + text);
            }

            Debug.WriteLine("TestMethods: echoing: " + text);
            return text;
        }

        public static TestStruct StaticEchoData(TestStruct data, bool fail)
        {
            if (fail)
            {
                Debug.WriteLine("TestMethods: " + "Failed to echo data");
                throw new Exception("Failed to echo data");
            }

            Debug.WriteLine("TestMethods: " + "(data)");
            return data;
        }

        public void Log(string text, bool fail)
        {
            if (fail)
            {
                Debug.WriteLine("TestMethods: " + "Failed to log: " + text);
                throw new Exception("Failed to log: " + text);
            }

            Debug.WriteLine("TestMethods: " + text);
        }

        public String Echo(string text, bool fail)
        {
            if (fail)
            {
                Debug.WriteLine("TestMethods: " + "Failed to echo: " + text);
                throw new Exception("Failed to echo: " + text);
            }

            Debug.WriteLine("TestMethods: " + text);
            return text;
        }

        public TestStruct EchoData(TestStruct data, bool fail)
        {
            if (fail)
            {
                Debug.WriteLine("TestMethods: Failed to echo data");
                throw new Exception("Failed to echo data");
            }

            Debug.WriteLine("TestMethods: " + "(data)");
            return data;
        }

        public IList<TestStruct> EchoDataList(IList<TestStruct> dataList, bool fail)
        {
            if (fail)
            {
                Debug.WriteLine("TestMethods: " + "Failed to echo data list");
                throw new Exception("Failed to echo data list");
            }

            Debug.WriteLine("TestMethods: " + "(data list)");
            return dataList;
        }

        public int? EchoNullableInt(int? intValue)
        {
            return intValue;
        }

        public Guid? EchoUuid(Guid? uuidValue)
        {
            return uuidValue;
        }

        public bool? EchoNullableBool(bool? boolValue)
        {
            return boolValue;
        }
    }
}
