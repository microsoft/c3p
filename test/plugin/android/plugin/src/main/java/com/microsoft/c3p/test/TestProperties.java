// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p.test;

import android.net.Uri;

import java.util.ArrayList;
import java.util.Dictionary;
import java.util.List;
import java.util.UUID;

/**
 * Tests getting and setting static and instance properties of various types.
 */
public class TestProperties {
    private static TestStruct _staticStruct;
    private static double _staticDouble;
    private static List<String> _staticList;
    private static int _staticEnum;
    private static boolean _staticBool;

    private TestStruct _struct;
    private double _double;
    private List<String> _list;
    private List<String> _readonlyList;
    private int _enum;
    private boolean _bool;
    private Integer _nullableInt;
    private Double _nullableDouble;
    private UUID _uuid;
    private Uri _uri;

    public TestProperties() {
        _readonlyList = new ArrayList<String>();
        _readonlyList.add("One");
        _readonlyList.add("Two");
        _readonlyList.add("Three");
    }

    public static TestStruct getStaticStructProperty() {
        return _staticStruct;
    }

    public static void setStaticStructProperty(TestStruct value) {
        _staticStruct = value;
    }

    public static List<String> getStaticListProperty() {
        return _staticList;
    }

    public static void setStaticListProperty(List<String> value) {
        _staticList = value;
    }

    public static double getStaticDoubleProperty() {
        return _staticDouble;
    }

    public static void setStaticDoubleProperty(double value) {
        _staticDouble = value;
    }

    // Currently broken: does not get bound as a property on IOS.
    /*
    public static int getStaticReadonlyIntProperty() {
        return 10;
    }
    */

    public static int getStaticEnumProperty() {
        return _staticEnum;
    }

    public static void setStaticEnumProperty(int value) {
        _staticEnum = value;
    }

    public static boolean getStaticBoolProperty() {
        return _staticBool;
    }

    public static void setStaticBoolProperty(boolean value) {
        _staticBool = value;
    }

    public TestStruct getStructProperty() {
        return _struct;
    }

    public void setStructProperty(TestStruct value) {
        _struct = value;
    }

    public List<String> getListProperty() {
        return _list;
    }

    public void setListProperty(List<String> value) {
        _list = value;
    }

    public List<String> getReadonlyListProperty() {
        return _readonlyList;
    }

    public double getDoubleProperty() {
        return _double;
    }

    public void setDoubleProperty(double value) {
        _double = value;
    }

    public int getReadonlyIntProperty() {
        return 20;
    }

    public int getEnumProperty() {
        return _enum;
    }

    public void setEnumProperty(int value) {
        _enum = value;
    }

    public boolean getBoolProperty() {
        return _bool;
    }

    public void setBoolProperty(boolean value) {
        _bool = value;
    }

    public Integer getNullableIntProperty() {
        return _nullableInt;
    }

    public void setNullableIntProperty(Integer value) {
        _nullableInt = value;
    }

    public Double getNullableDoubleProperty() {
        return _nullableDouble;
    }

    public void setNullableDoubleProperty(Double value) {
        _nullableDouble = value;
    }

    public UUID getUuidProperty() {
        return _uuid;
    }

    public void setUuidProperty(UUID value) {
        _uuid = value;
    }

    public Uri getUriProperty() {
        return _uri;
    }

    public void setUriProperty(Uri value) {
        _uri = value;
    }

    public TestOneWayStruct getOneWayStructProperty() {
        return new TestOneWayStruct("test");
    }
}
