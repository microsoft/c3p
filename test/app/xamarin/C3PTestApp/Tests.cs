// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Microsoft.C3P.Test
{
    public class Tests
    {
        TestLogView log;
        int passCount = 0;
        int failCount = 0;
        string currentTest;

        public Tests(TestLogView log)
        {
            this.log = log;
        }

        public async void Run()
        {
            log.Clear();

            await Task.Run(async () =>
            {
                try
                {
                    TestMethodAPIs();
                    await Task.Yield();
                    TestPropertyAPIs();
                    await Task.Yield();
                    await TestAsyncMethodAPIs();
                    await Task.Yield();
                    TestEventAPIs();
                    await Task.Yield();
                    await TestContextAPIs();
                    await Task.Yield();
                    TestDispose();

                    Log("");
                    LogImportant("RESULTS: " + passCount + " passed, " + failCount + " failed.");
                }
                catch (Exception ex)
                {
                    LogError("ERROR: " + ex.ToString());
                }
            });
        }

        void TestMethodAPIs()
        {
            Log("");
            Log("Testing methods...");

            currentTest = "TestMethods.StaticLog(\"test\", fail: false)";
            Expect(() => TestMethods.StaticLog("test", fail: false));

            currentTest = "TestMethods.StaticEcho(\"test\", fail: false)";
            ExpectResult("test", () => TestMethods.StaticEcho("test", fail: false));

            currentTest = "TestMethods.StaticLog(\"test\", fail: true)";
            ExpectException(() => TestMethods.StaticLog("test", fail: true));

            currentTest = "TestMethods.StaticEcho(\"test\", fail: true)";
            ExpectException(() => TestMethods.StaticEcho("test", fail: true));

            var testMethodsInstance = new TestMethods();

            currentTest = "testMethodsInstance.Log(\"test\", fail: false)";
            Expect(() => testMethodsInstance.Log("test", fail: false));

            currentTest = "testMethodsInstance.Echo(\"test\", fail: false)";
            ExpectResult("test", () => testMethodsInstance.Echo("test", fail: false));

            currentTest = "testMethodsInstance.Log(\"test\", fail: true)";
            ExpectException(() => testMethodsInstance.Log("test", fail: true));

            currentTest = "testMethodsInstance.Echo(\"test\", fail: true)";
            ExpectException(() => testMethodsInstance.Echo("test", fail: true));

            var testData = new TestStruct();
            testData.Value = new DateTimeOffset(new DateTime(2016, 6, 6));

            currentTest = "testMethodsInstance.EchoData({}, fail: false)";
            ExpectResult(() => testMethodsInstance.EchoData(testData, false),
                result => testData.Value == result?.Value);

            TestStruct[] testDataArray = new[] { new TestStruct(), new TestStruct() };
            testDataArray[0].Value = new DateTimeOffset(new DateTime(2016, 6, 7));
            testDataArray[1].Value = new DateTimeOffset(new DateTime(2016, 6, 8));

            currentTest = "testMethodsInstance.EchoDataList([{},{}], fail: false)";
            ExpectResult(() => testMethodsInstance.EchoDataList(testDataArray, false),
                result => 2 == result?.Count &&
                    testDataArray[0].Value == result[0].Value &&
                    testDataArray[1].Value == result[1].Value);
        }

        void TestPropertyAPIs()
        {
            Log("");
            Log("Testing properties...");

            currentTest = "set TestProperties.StaticBoolProperty = true";
            Expect(() => TestProperties.IsStaticBoolProperty = true);

            currentTest = "get TestProperties.StaticBoolProperty";
            ExpectResult(true, () => TestProperties.IsStaticBoolProperty);

            currentTest = "set TestProperties.StaticDoubleProperty = 3.14";
            Expect(() => TestProperties.StaticDoubleProperty = 3.14);

            currentTest = "get TestProperties.StaticDoubleProperty";
            ExpectResult(3.14, () => TestProperties.StaticDoubleProperty);

            currentTest = "set TestProperties.StaticEnumProperty = TestEnum.Two";
            Expect(() => TestProperties.StaticEnumProperty = TestEnum.Two);

            currentTest = "get TestProperties.StaticEnumProperty";
            ExpectResult(TestEnum.Two, () => TestProperties.StaticEnumProperty);

            currentTest = "set TestProperties.StaticListProperty = [\"a\", \"b\", \"c\"]";
            Expect(() => TestProperties.StaticListProperty = new string[] { "a", "b", "c" });

            currentTest = "get TestProperties.StaticListProperty";
            ExpectResult(new List<string>(new string[] { "a", "b", "c" }), () => TestProperties.StaticListProperty);

            var testPropertiesInstance = new TestProperties();

            currentTest = "set testPropertiesInstance.BoolProperty = true";
            Expect(() => testPropertiesInstance.IsBoolProperty = true);

            currentTest = "get testPropertiesInstance.BoolProperty";
            ExpectResult(true, () => testPropertiesInstance.IsBoolProperty);

            currentTest = "set testPropertiesInstance.DoubleProperty = 3.14";
            Expect(() => testPropertiesInstance.DoubleProperty = 3.14);

            currentTest = "get testPropertiesInstance.DoubleProperty";
            ExpectResult(3.14, () => testPropertiesInstance.DoubleProperty);

            currentTest = "set testPropertiesInstance.EnumProperty = TestEnum.Two";
            Expect(() => testPropertiesInstance.EnumProperty = TestEnum.Two);

            currentTest = "get testPropertiesInstance.EnumProperty";
            ExpectResult(TestEnum.Two, () => testPropertiesInstance.EnumProperty);

            currentTest = "set testPropertiesInstance.ListProperty = [\"a\", \"b\", \"c\"]";
            Expect(() => testPropertiesInstance.ListProperty = new string[] { "a", "b", "c" });

            currentTest = "get testPropertiesInstance.ListProperty";
            ExpectResult(new List<string>(new string[] { "a", "b", "c" }), () => testPropertiesInstance.ListProperty);
        }

        async Task TestAsyncMethodAPIs()
        {
            Log("");
            Log("Testing async methods...");

            currentTest = "TestAsync.StaticLogAsync(\"test\", fail: false)";
            await ExpectAsync(() => TestAsync.StaticLogAsync("test", fail: false));

            currentTest = "TestAsync.StaticEchoAsync(\"test\", fail: false)";
            await ExpectResultAsync("test", () => TestAsync.StaticEchoAsync("test", fail: false));

            currentTest = "TestAsync.StaticLogAsync(\"test\", fail: true)";
            await ExpectExceptionAsync(() => TestAsync.StaticLogAsync("test", fail: true));

            currentTest = "TestAsync.StaticEchoAsync(\"test\", fail: true)";
            await ExpectExceptionAsync(() => TestAsync.StaticEchoAsync("test", fail: true));

            var testAsyncInstance = new TestAsync();

            currentTest = "testAsyncInstance.LogAsync(\"test\", fail: false)";
            await ExpectAsync(() => testAsyncInstance.LogAsync("test", fail: false));

            currentTest = "testAsyncInstance.EchoAsync(\"test\", fail: false)";
            await ExpectResultAsync("test", () => testAsyncInstance.EchoAsync("test", fail: false));

            currentTest = "testAsyncInstance.LogAsync(\"test\", fail: true)";
            await ExpectExceptionAsync(() => testAsyncInstance.LogAsync("test", fail: true));

            currentTest = "testAsyncInstance.EchoAsync(\"test\", fail: true)";
            await ExpectExceptionAsync(() => testAsyncInstance.EchoAsync("test", fail: true));

            var testData = new TestStruct();
            testData.Value = new DateTimeOffset(new DateTime(2016, 6, 6));

            currentTest = "testAsyncInstance.EchoDataAsync({}, fail: false)";
            await ExpectResultAsync(() => testAsyncInstance.EchoDataAsync(testData, false),
                result => testData.Value == result?.Value);

            TestStruct[] testDataArray = new[] { new TestStruct(), new TestStruct() };
            testDataArray[0].Value = new DateTimeOffset(new DateTime(2016, 6, 7));
            testDataArray[1].Value = new DateTimeOffset(new DateTime(2016, 6, 8));

            currentTest = "testAsyncInstance.EchoDataListAsync([{},{}], fail: false)";
            await ExpectResultAsync(() => testAsyncInstance.EchoDataListAsync(testDataArray, false),
                result => 2 == result?.Count &&
                testDataArray[0].Value == result[0].Value &&
                testDataArray[1].Value == result[1].Value);
        }

        void TestEventAPIs()
        {
            Log("");
            Log("Testing events...");

            object receivedEventSender;
            TestEvent receivedEvent;
            EventHandler<TestEvent> testEventHandler = (sender, e) =>
            {
                receivedEventSender = sender;
                receivedEvent = e;
            };

            currentTest = "add TestEvents.StaticEvent";
            Expect(() => TestEvents.StaticEvent += testEventHandler);
            currentTest = "raise TestEvents.StaticEvent";
            receivedEventSender = null;
            receivedEvent = null;
            Expect(() => TestEvents.RaiseStaticEvent());
            Assert(receivedEvent != null, "received an event");
            Assert(receivedEventSender == typeof(TestEvents), "event was from TestEvents class");

            currentTest = "remove TestEvents.StaticEvent";
            Expect(() => TestEvents.StaticEvent -= testEventHandler);
            currentTest = "raise TestEvents.StaticEvent";
            receivedEventSender = null;
            receivedEvent = null;
            Expect(() => TestEvents.RaiseStaticEvent());
            Assert(receivedEvent == null && receivedEventSender == null, "did not receive an event");

            var testEventsInstance = new TestEvents();

            currentTest = "add testEventsInstance.InstanceEvent";
            Expect(() => testEventsInstance.InstanceEvent += testEventHandler);
            currentTest = "raise testEventsInstance.InstanceEvent";
            receivedEventSender = null;
            receivedEvent = null;
            Expect(() => testEventsInstance.RaiseInstanceEvent());
            Assert(receivedEvent != null, "received an event");
            Assert(receivedEventSender == testEventsInstance, "event was from TestEvents instance");

            currentTest = "remove testEventsInstance.InstanceEvent";
            Expect(() => testEventsInstance.InstanceEvent -= testEventHandler);
            currentTest = "raise testEventsInstance.InstanceEvent";
            receivedEventSender = null;
            receivedEvent = null;
            Expect(() => testEventsInstance.RaiseInstanceEvent());
            Assert(receivedEvent == null && receivedEventSender == null, "did not receive an event");
        }

        async Task TestContextAPIs()
        {
            Log("");
            Log("Testing context...");

            currentTest = "TestContext.TestStaticMethodAppContext()";
            Expect(() => TestContext.TestStaticMethodAppContext());

            currentTest = "TestContext.TestStaticMethodAppContext2(0)";
            Expect(() => TestContext.TestStaticMethodAppContext2(0));

            currentTest = "TestContext.TestStaticMethodPageContext()";
            Expect(() => TestContext.TestStaticMethodWindowContext());

            currentTest = "TestContext.TestStaticMethodPageContext2(0)";
            Expect(() => TestContext.TestStaticMethodWindowContext2(0));

            var testContextInstance = new TestContext(false);

            currentTest = "testContextInstance.TestConstructorAppContext()";
            Expect(() => testContextInstance.TestConstructorAppContext());

            currentTest = "testContextInstance.TestMethodAppContext()";
            Expect(() => testContextInstance.TestMethodAppContext());

            currentTest = "testContextInstance.TestMethodAppContext2(0)";
            Expect(() => testContextInstance.TestMethodAppContext2(0));

            currentTest = "testContextInstance.TestMethodPageContext()";
            Expect(() => testContextInstance.TestMethodWindowContext());

            currentTest = "testContextInstance.TestMethodPageContext2(0)";
            Expect(() => testContextInstance.TestMethodWindowContext2(0));

            currentTest = "testContextInstance.TestAsyncMethodAppContext()";
            await ExpectAsync(() => testContextInstance.TestMethodAppContext3Async());

            currentTest = "testContextInstance.TestAsyncMethodAppContext2(0)";
            await ExpectAsync(() => testContextInstance.TestMethodAppContext4Async(0));

            currentTest = "testContextInstance.TestAsyncMethodPageContext()";
            await ExpectAsync(() => testContextInstance.TestMethodWindowContext3Async());

            currentTest = "testContextInstance.TestAsyncMethodPageContext2(0)";
            await ExpectAsync(() => testContextInstance.TestMethodWindowContext4Async(0));
        }

        void TestDispose()
        {
            Log("");
            Log("Testing dispose...");

            currentTest = "(proxy-by-value type)";
            TestStruct testStruct = new TestStruct();
            Assert(!(testStruct is IDisposable), "should not be disposable");

            currentTest = "dispose()";
            TestMethods testMethods = new TestMethods();
            IDisposable disposableInstance = testMethods;
            Expect(() => testMethods.Dispose());

            currentTest = "(calling method on disposed instance)";
            ExpectException(() => testMethods.EchoNullableInt(1));
        }

        void Expect(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert(false, GetExceptionMessage(ex));
                return;
            }

            Assert(true, "(no exception)");
        }

        async Task ExpectAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Assert(false, GetExceptionMessage(ex));
                return;
            }

            Assert(true, "(no exception)");
        }

        void ExpectResult<T>(T expected, Func<T> action)
        {
            T result;
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                Assert(false, GetExceptionMessage(ex));
                return;
            }

            AssertEquals(expected, result);
        }

        void ExpectResult<T>(Func<T> action, Func<T, bool> test)
        {
            T result;
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                Assert(false, GetExceptionMessage(ex));
                return;
            }

            Assert(test(result), typeof(T).Name);
        }

        async Task ExpectResultAsync<T>(T expected, Func<Task<T>> action)
        {
            T result;
            try
            {
                result = await action();
            }
            catch (Exception ex)
            {
                Assert(false, GetExceptionMessage(ex));
                return;
            }

            AssertEquals(expected, result);
        }

        async Task ExpectResultAsync<T>(Func<Task<T>> action, Func<T, bool> test)
        {
            T result;
            try
            {
                result = await action();
            }
            catch (Exception ex)
            {
                Assert(false, GetExceptionMessage(ex));
                return;
            }

            Assert(test(result), typeof(T).Name);
        }

        void ExpectException(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert(true, GetExceptionMessage(ex));
                return;
            }

            Assert(false, "Expected exception but none was thrown.");
        }

        async Task ExpectExceptionAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Assert(true, GetExceptionMessage(ex));
                return;
            }

            Assert(false, "Expected exception but none was thrown.");
        }

        void AssertEquals<T>(T expected, T actual)
        {
            if (expected is IList)
            {
                if (actual is IList)
                {
                    IList expectedList = (IList)(Object)expected;
                    IList actualList = (IList)(Object)actual;
                    bool listsAreEqual = (((actualList == null) == (expectedList == null)) &&
                        (expectedList == null || (actualList.Count == expectedList.Count &&
                        actualList.Cast<Object>().SequenceEqual(expectedList.Cast<Object>()))));
                    if (listsAreEqual)
                    {
                        Assert(true, "[" + String.Join(", ", actualList.Cast<Object>()) + "]");
                    }
                    else
                    {
                        Assert(false, "actual: " + "[" + String.Join(", ", actualList.Cast<Object>()) + "]" + ", " +
                            "expected: " + "[" + String.Join(", ", expectedList.Cast<Object>()) + "]");
                    }
                }
                else
                {
                    Assert(false, "actual: " + (actual == null ? "(null)" : actual.GetType().Name) + ", " +
                        "expected: IList");
                }
            }
            else
            {
                if (Object.Equals(actual, expected))
                {
                    Assert(true, actual.ToString());
                }
                else
                {
                    Assert(false, "actual: " + actual + ", expected: " + expected);
                }
            }
        }

        void Assert(bool assertion, string assertionString)
        {
            if (assertion)
            {
                LogSuccess("PASSED: " + currentTest + " - " + assertionString);
                passCount++;
            }
            else
            {
                LogError("FAILED: " + currentTest + " - " + assertionString);
                failCount++;
            }
        }

        void Log(string message)
        {
            Debug.WriteLine(message);
            this.log.AppendLine(message);
        }

        void LogImportant(string message)
        {
            Debug.WriteLine(message);
            this.log.AppendLine(message, Color.Black, true);
        }

        void LogSuccess(string message)
        {
            Debug.WriteLine(message);
            this.log.AppendLine(message, Color.Green, false);
        }

        void LogError(string message)
        {
            Debug.WriteLine(message);
            this.log.AppendLine(message, Color.Red, false);
        }

        static string GetExceptionMessage(Exception ex)
        {
            if (ex.GetType().Name == "NSErrorException")
            {
                Object nserror = null;
                TypeInfo nserrorExceptionType = ex.GetType().GetTypeInfo();
                PropertyInfo errorProperty = nserrorExceptionType.GetDeclaredProperty("Error");
                if (errorProperty != null)
                {
                    nserror = errorProperty.GetValue(ex);
                }
                else
                {
                    // This seems to be a bug in the Xamarin runtime.
                    // Error is a field instead of a property on iOS.
                    FieldInfo errorField = nserrorExceptionType.GetDeclaredField("error");
                    if (errorField != null)
                    {
                        nserror = errorField.GetValue(ex);
                    }
                }

                return "NSError: " + nserror;
            }
            else
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }
    }
}

