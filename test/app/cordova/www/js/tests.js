// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

var runTests = function () {
    var currentTest = null;
    var passCount;
    var failCount;
    var plugin;
    var log;

    function run(p, l) {
        passCount = 0;
        failCount = 0;
        plugin = p;
        log = l;

        if (!plugin) {
            log("Test plugin not found!");
            return;
        }

        log("Test plugin found.");

        return testMethods().then(function () {
        return testProperties().then(function () {
        return testNestedTypes().then(function () {
        return testAsyncMethods().then(function () {
        return testEvents().then(function () {
        return testContext().then(function () {
        return testDispose().then(function () {

        });});});});});});})
        .then(
            function () {
                log("");
                logImportant("RESULTS: " + passCount + " passed, " + failCount + " failed.");
                log("");
            },
            function (error) {
                log("");
                logError("ERROR: " + error.message);
                log("");
            });
    }

    function testMethods() {
        log("");
        log("Testing methods...");

        currentTest = "TestMethods.staticLog('test', fail: false)";
        return plugin.TestMethods.staticLogAsync('test', false)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestMethods.staticEcho('test', fail: false)";
        return plugin.TestMethods.staticEchoAsync('test', false)
        .then(expectResult("test"), handleError).then(function () {

        currentTest = "TestMethods.staticLog('test', fail: true)";
        return plugin.TestMethods.staticLogAsync('test', true)
        .then(expectNoResult(), handleExpectedError).then(function () {

        currentTest = "TestMethods.staticEcho('test', fail: true)";
        return plugin.TestMethods.staticEchoAsync('test', true)
        .then(expectNoResult(), handleExpectedError).then(function () {

        var testMethodsInstance = new plugin.TestMethods();

        currentTest = "testMethodsInstance.log('test', fail: false)";
        return testMethodsInstance.logAsync('test', false)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testMethodsInstance.echo('test', fail: false)";
        return testMethodsInstance.echoAsync('test', false)
        .then(expectResult("test"), handleError).then(function () {

        currentTest = "testMethodsInstance.log('test', fail: true)";
        return testMethodsInstance.logAsync('test', true)
        .then(expectNoResult(), handleExpectedError).then(function () {

        currentTest = "testMethodsInstance.echo('test', fail: true)";
        return testMethodsInstance.echoAsync('test', true)
        .then(expectNoResult(), handleExpectedError).then(function () {

        var testData = new plugin.TestStruct();
        testData.value = new Date(2016, 1, 1);

        currentTest = "testMethodsInstance.echoData({}, fail: false)";
        return testMethodsInstance.echoDataAsync(testData, false)
        .then(expectResult2(testData), handleError).then(function () {

        var testDataArray = [new plugin.TestStruct(), new plugin.TestStruct()];
        testDataArray[0].value = new Date(2016, 1, 1);
        testDataArray[1].value = new Date(2016, 1, 2);

        currentTest = "testMethodsInstance.echoDataList([{},{}], fail: false)";
        return testMethodsInstance.echoDataListAsync(testDataArray, false)
        .then(expectResult2(testDataArray), handleError).then(function () {

        currentTest = "testMethodsInstance.echoNullableInt(12)";
        return testMethodsInstance.echoNullableIntAsync(12)
        .then(expectResult2(12), handleError).then(function () {

        currentTest = "testMethodsInstance.echoUuid('2ADC0D21-525E-4A5D-A106-149E1535E43E')";
        return testMethodsInstance.echoUuidAsync('2ADC0D21-525E-4A5D-A106-149E1535E43E', false)
        .then(expectResult2('2ADC0D21-525E-4A5D-A106-149E1535E43E'), handleError).then(function () {

        });});});});
        });});});});
        });});});});
    }

    function testAsyncMethods() {
        log("");
        log("Testing async methods...");

        currentTest = "TestAsync.staticLogAsync('test', fail: false)";
        return plugin.TestAsync.staticLogAsync('test', false)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestAsync.staticEchoAsync('test', fail: false)";
        return plugin.TestAsync.staticEchoAsync('test', false)
        .then(expectResult("test"), handleError).then(function () {

        currentTest = "TestAsync.staticLogAsync('test', fail: true)";
        return plugin.TestAsync.staticLogAsync('test', true)
        .then(expectNoResult(), handleExpectedError).then(function () {

        currentTest = "TestAsync.staticEchoAsync('test', fail: true)";
        return plugin.TestAsync.staticEchoAsync('test', true)
        .then(expectNoResult(), handleExpectedError).then(function () {

        var testAsyncInstance = new plugin.TestAsync();

        currentTest = "testAsyncInstance.logAsync('test', fail: false)";
        return testAsyncInstance.logAsync('test', false)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testAsyncInstance.echoAsync('test', fail: false)";
        return testAsyncInstance.echoAsync('test', false)
        .then(expectResult("test"), handleError).then(function () {

        currentTest = "testAsyncInstance.logAsync('test', fail: true)";
        return testAsyncInstance.logAsync('test', true)
        .then(expectNoResult(), handleExpectedError).then(function () {

        currentTest = "testAsyncInstance.echoAsync('test', fail: true)";
        return testAsyncInstance.echoAsync('test', true)
        .then(expectNoResult(), handleExpectedError).then(function () {

        var testData = new plugin.TestStruct();
        testData.value = new Date(2016, 1, 1);

        currentTest = "testAsyncInstance.echoDataAsync({}, fail: false)";
        return testAsyncInstance.echoDataAsync(testData, false)
        .then(expectResult2(testData), handleError).then(function () {

        var testDataArray = [new plugin.TestStruct(), new plugin.TestStruct()];
        testDataArray[0].value = new Date(2016, 1, 1);
        testDataArray[1].value = new Date(2016, 1, 2);

        currentTest = "testAsyncInstance.echoDataListAsync([{},{}], fail: false)";
        return testAsyncInstance.echoDataListAsync(testDataArray, false)
        .then(expectResult2(testDataArray), handleError).then(function () {

        currentTest = "testAsyncInstance.echoNullableIntAsync(12)";
        return testAsyncInstance.echoNullableIntAsync(12)
        .then(expectResult2(12), handleError).then(function () {

        currentTest = "testAsyncInstance.echoUuidAsync('2ADC0D21-525E-4A5D-A106-149E1535E43E')";
        return testAsyncInstance.echoUuidAsync('2ADC0D21-525E-4A5D-A106-149E1535E43E', false)
        .then(expectResult2('2ADC0D21-525E-4A5D-A106-149E1535E43E'), handleError).then(function () {

        });});});});
        });});});});
        });});});});
    }

    function testProperties() {
        log("");
        log("Testing properties...");

        currentTest = "TestProperties.setStaticBoolProperty(true)";
        return plugin.TestProperties.setStaticBoolPropertyAsync(true)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestProperties.isStaticBoolProperty()";
        return plugin.TestProperties.isStaticBoolPropertyAsync()
        .then(expectResult(true), handleError).then(function () {

        currentTest = "TestProperties.setStaticDoubleProperty(3.14)";
        return plugin.TestProperties.setStaticDoublePropertyAsync(3.14)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestProperties.getStaticDoubleProperty()";
        return plugin.TestProperties.getStaticDoublePropertyAsync()
        .then(expectResult(3.14), handleError).then(function () {

        currentTest = "TestProperties.setStaticEnumProperty(TestEnum.two)";
        return plugin.TestProperties.setStaticEnumPropertyAsync(plugin.TestEnum.two)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestProperties.getStaticEnumProperty()";
        return plugin.TestProperties.getStaticEnumPropertyAsync()
        .then(expectResult(plugin.TestEnum.two), handleError).then(function () {

        currentTest = "TestProperties.setStaticListProperty(['a', 'b', 'c'])";
        return plugin.TestProperties.setStaticListPropertyAsync(['a', 'b', 'c'])
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestProperties.getStaticListProperty()";
        return plugin.TestProperties.getStaticListPropertyAsync()
        .then(expectResult2(['a', 'b', 'c']), handleError).then(function () {

        var testPropertiesInstance = new plugin.TestProperties();

        currentTest = "testPropertiesInstance.setBoolProperty(true)";
        return testPropertiesInstance.setBoolPropertyAsync(true)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testPropertiesInstance.isBoolProperty()";
        return testPropertiesInstance.isBoolPropertyAsync()
        .then(expectResult(true), handleError).then(function () {

        currentTest = "testPropertiesInstance.setDoubleProperty(3.14)";
        return testPropertiesInstance.setDoublePropertyAsync(3.14)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testPropertiesInstance.getDoubleProperty()";
        return testPropertiesInstance.getDoublePropertyAsync()
        .then(expectResult(3.14), handleError).then(function () {

        currentTest = "testPropertiesInstance.setEnumProperty(TestEnum.three)";
        return testPropertiesInstance.setEnumPropertyAsync(plugin.TestEnum.three)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testPropertiesInstance.getEnumProperty()";
        return testPropertiesInstance.getEnumPropertyAsync()
        .then(expectResult(plugin.TestEnum.three), handleError).then(function () {

        currentTest = "testPropertiesInstance.setListProperty(['a', 'b', 'c'])";
        return testPropertiesInstance.setListPropertyAsync(['a', 'b', 'c'])
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testPropertiesInstance.getListProperty()";
        return testPropertiesInstance.getListPropertyAsync()
        .then(expectResult2(['a', 'b', 'c']), handleError).then(function () {

        currentTest = "testPropertiesInstance.setNullableIntProperty(14)";
        return testPropertiesInstance.setNullableIntPropertyAsync(14)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testPropertiesInstance.getNullableIntProperty()";
        return testPropertiesInstance.getNullableIntPropertyAsync()
        .then(expectResult2(14), handleError).then(function () {

        currentTest = "testPropertiesInstance.setNullableDoubleProperty(1.44)";
        return testPropertiesInstance.setNullableDoublePropertyAsync(1.44)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testPropertiesInstance.getNullableDoubleProperty()";
        return testPropertiesInstance.getNullableDoublePropertyAsync()
        .then(expectResult2(1.44), handleError).then(function () {

        currentTest = "testPropertiesInstance.setUuidProperty('2ADC0D21-525E-4A5D-A106-149E1535E43E')";
        return testPropertiesInstance.setUuidPropertyAsync('2ADC0D21-525E-4A5D-A106-149E1535E43E')
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testPropertiesInstance.getUuidProperty()";
        return testPropertiesInstance.getUuidPropertyAsync()
        .then(expectResult('2ADC0D21-525E-4A5D-A106-149E1535E43E'), handleError).then(function () {

        currentTest = "testPropertiesInstance.setUriProperty('http://www.example.com/')";
        return testPropertiesInstance.setUriPropertyAsync('http://www.example.com/')
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testPropertiesInstance.getUriProperty()";
        return testPropertiesInstance.getUriPropertyAsync()
        .then(expectResult('http://www.example.com/'), handleError).then(function () {

        });});});});});});});});
        });});});});});});});});
        });});});});});});});});
    }

    function testNestedTypes() {
        return Promise.resolve();
    }

    function testEvents() {
        log("");
        log("Testing events...");

        var receivedEvent;
        var testEventHandler = function(e) {
            receivedEvent = e;
        };

        currentTest = "TestEvents.addStaticEventListener";
        return plugin.TestEvents.addStaticEventListenerAsync(testEventHandler)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "raise TestEvents.StaticEvent";
        receivedEvent = null;
        return plugin.TestEvents.raiseStaticEventAsync()
        .then(expectResult(undefined), handleError).then(function () {

        assert(receivedEvent !== null, "received an event");
        assert(receivedEvent !== null && receivedEvent.constructor === plugin.TestEvent,
            "event was a TestEvent instance");
        assert(receivedEvent !== null && receivedEvent.counter > 0,
            "event has data");

        currentTest = "TestEvents.removeStaticEventListener";
        return plugin.TestEvents.removeStaticEventListenerAsync(testEventHandler)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "raise TestEvents.StaticEvent";
        receivedEvent = null;
        return plugin.TestEvents.raiseStaticEventAsync()
        .then(expectResult(undefined), handleError).then(function () {

        assert(receivedEvent === null, "did not receive an event");

        var testEventsInstance = new plugin.TestEvents();

        currentTest = "testEventsInstance.addInstanceEventListener";
        return testEventsInstance.addInstanceEventListenerAsync(testEventHandler)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "raise testEventsInstance.InstanceEvent";
        receivedEvent = null;
        return testEventsInstance.raiseInstanceEventAsync()
        .then(expectResult(undefined), handleError).then(function () {

        assert(receivedEvent !== null, "received an event");
        assert(receivedEvent !== null && receivedEvent.constructor === plugin.TestEvent,
            "event was a TestEvent instance");
        assert(receivedEvent !== null && receivedEvent.counter > 0,
            "event has data");
        currentTest = "testEventsInstance.removeInstanceEventListener";
        return testEventsInstance.removeInstanceEventListenerAsync(testEventHandler)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "raise testEventsInstance.InstanceEvent";
        receivedEvent = null;
        return testEventsInstance.raiseInstanceEventAsync()
        .then(expectResult(undefined), handleError).then(function () {

        assert(receivedEvent === null, "did not receive an event");

        });});});});
        });});});});
    }

    function testContext() {
        log("");
        log("Testing context...");

        currentTest = "TestContext.testStaticMethodAppContext";
        return plugin.TestContext.testStaticMethodAppContextAsync()
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestContext.testStaticMethodAppContext2";
        return plugin.TestContext.testStaticMethodAppContext2Async(0)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestContext.testStaticMethodWindowContext";
        return plugin.TestContext.testStaticMethodWindowContextAsync()
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "TestContext.testStaticMethodWindowContext2";
        return plugin.TestContext.testStaticMethodWindowContext2Async(0)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "new TestContext(fail: true)";
        var failingTestContextInstance = new plugin.TestContext(true);
        return failingTestContextInstance.testConstructorAppContextAsync()
        .then(expectNoResult(), handleExpectedError).then(function () {

        var testContextInstance = new plugin.TestContext(false);

        currentTest = "testContextInstance.testConstructorAppContext";
        return testContextInstance.testConstructorAppContextAsync()
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testMethodAppContext";
        return testContextInstance.testMethodAppContextAsync()
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testMethodAppContext2";
        return testContextInstance.testMethodAppContext2Async(0)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testMethodWindowContext";
        return testContextInstance.testMethodWindowContextAsync()
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testMethodWindowContext2";
        return testContextInstance.testMethodWindowContext2Async(0)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testMethodAppContext3Async";
        return testContextInstance.testMethodAppContext3Async()
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testMethodAppContext4Async";
        return testContextInstance.testMethodAppContext4Async(0)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testAsyncMethodWindowContext3Async";
        return testContextInstance.testMethodWindowContext3Async()
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testAsyncMethodWindowContext4Async";
        return testContextInstance.testMethodWindowContext4Async(0)
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testContextInstance.testAndroidActivityAsync";
        return testContextInstance.testAndroidActivityAsync()
        .then(expectResult(undefined), handleError).then(function () {

        });});});});});
        });});});});});
        });});});});});
    }

    function testDispose() {
        log("");
        log("Testing dispose...");

        var testInstance = new plugin.TestOuter();
        currentTest = "testInstance.dispose";
        return testInstance.dispose()
        .then(expectResult(undefined), handleError).then(function () {

        currentTest = "testInstance.dispose 2";
        return testInstance.dispose()
        .then(expectNoResult(), handleExpectedError).then(function () {

        });});
    }

    function expectNoResult() {
        return expect(function (result) { return false; }, "(exception)");
    }

    function expectResult(expected) {
        return expect(function (result) { return result === expected; }, stringify(expected));
    }

    function expectResult2(expected) {
        return expect(function (result) { return stringify(result) === stringify(expected); }, stringify(expected));
    }

    function expect(expectation, expectationString) {
        return function(result) {
            if (expectation(result)) {
                logSuccess("PASSED: " + currentTest + " - " + expectationString);
                passCount++;
            } else {
                logError("FAILED: " + currentTest +
                    " - actual: " + stringify(result) + ", expected: " + expectationString);
                failCount++;
            }
        };
    }

    function assert(assertion, assertionString) {
        if (assertion) {
            logSuccess("PASSED: " + currentTest + " - " + assertionString);
            passCount++;
        } else {
            logError("FAILED: " + currentTest + " - " + assertionString);
            failCount++;
        }
    }

    function handleError(error) {
        logError("FAILED: " + currentTest + " - exception: " +
            (error.message || stringify(error)));
        failCount++;
    }

    function handleExpectedError(error) {
        logSuccess("PASSED: " + currentTest + " - expected exception: " +
            (error.message || stringify(error)));
        passCount++;
    }

    function logSuccess(message) {
        log(message, "color: green");
    }

    function logError(message) {
        log(message, "color: red");
    }

    function logImportant(message) {
        log(message, "font-weight: bold; font-size: 120%");
    }

    function stringify(obj) {
        try {
            return JSON.stringify(obj);
        } catch (e) {
            return e.message || JSON.stringify(e);
        }
    }

    return run;
}();
