// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#include "pch.h"
#include "TestEnum.h"
#include "TestStruct.h"
#include "TestOneWayStruct.h"
#include "TestProperties.h"

using namespace Microsoft::VisualStudio::C3P::Test;

TestStruct^ TestProperties::staticStructValue = nullptr;
Windows::Foundation::Collections::IVector<Platform::String^>^ TestProperties::staticListValue = nullptr;
double TestProperties::staticDoubleValue = 0.0;
TestEnum TestProperties::staticEnumValue = TestEnum::Zero;
bool TestProperties::staticBoolValue = false;

TestProperties::TestProperties()
{
    this->structValue = nullptr;
    this->listValue = nullptr;
    this->doubleValue = 0.0;
    this->enumValue = TestEnum::Zero;
    this->boolValue = false;
    this->nullableDoubleValue = nullptr;
    this->nullableIntValue = nullptr;
    this->nullableUuidValue = nullptr;

    this->readonlyListValue = ref new Platform::Collections::Vector<Platform::String^>();
    this->readonlyListValue->Append(ref new Platform::String(L"One"));
    this->readonlyListValue->Append(ref new Platform::String(L"Two"));
    this->readonlyListValue->Append(ref new Platform::String(L"Three"));
}

TestStruct^ TestProperties::StaticStructProperty::get()
{
    return TestProperties::staticStructValue;
}

void TestProperties::StaticStructProperty::set(TestStruct^ value)
{
    TestProperties::staticStructValue = value;
}

Windows::Foundation::Collections::IVector<Platform::String^>^ TestProperties::StaticListProperty::get()
{
    return TestProperties::staticListValue;
}

void TestProperties::StaticListProperty::set(Windows::Foundation::Collections::IVector<Platform::String^>^ value)
{
    TestProperties::staticListValue = value;
}

double TestProperties::StaticDoubleProperty::get()
{
    return TestProperties::staticDoubleValue;
}

void TestProperties::StaticDoubleProperty::set(double value)
{
    TestProperties::staticDoubleValue = value;
}

TestEnum TestProperties::StaticEnumProperty::get()
{
    return TestProperties::staticEnumValue;
}

void TestProperties::StaticEnumProperty::set(TestEnum value)
{
    TestProperties::staticEnumValue = value;
}

bool TestProperties::StaticBoolProperty::get()
{
    return TestProperties::staticBoolValue;
}

void TestProperties::StaticBoolProperty::set(bool value)
{
    TestProperties::staticBoolValue = value;
}

TestStruct^ TestProperties::StructProperty::get()
{
    return this->structValue;
}

void TestProperties::StructProperty::set(TestStruct^ value)
{
    this->structValue = value;
}

Windows::Foundation::Collections::IVector<Platform::String^>^ TestProperties::ListProperty::get()
{
    return this->listValue;
}

void TestProperties::ListProperty::set(Windows::Foundation::Collections::IVector<Platform::String^>^ value)
{
    this->listValue = value;
}

Windows::Foundation::Collections::IVector<Platform::String^>^ TestProperties::ReadonlyListProperty::get()
{
    return this->readonlyListValue;
}

double TestProperties::DoubleProperty::get()
{
    return this->doubleValue;
}

void TestProperties::DoubleProperty::set(double value)
{
    this->doubleValue = value;
}

int TestProperties::ReadonlyIntProperty::get()
{
    return 20;
}

TestEnum TestProperties::EnumProperty::get()
{
    return this->enumValue;
}

void TestProperties::EnumProperty::set(TestEnum value)
{
    this->enumValue = value;
}

bool TestProperties::BoolProperty::get()
{
    return this->boolValue;
}

void TestProperties::BoolProperty::set(bool value)
{
    this->boolValue = value;
}

Platform::IBox<int>^ TestProperties::NullableIntProperty::get()
{
    return this->nullableIntValue;
}

void TestProperties::NullableIntProperty::set(Platform::IBox<int>^ value)
{
    this->nullableIntValue = value;
}

Platform::IBox<double>^ TestProperties::NullableDoubleProperty::get()
{
    return this->nullableDoubleValue;
}

void TestProperties::NullableDoubleProperty::set(Platform::IBox<double>^ value)
{
    this->nullableDoubleValue = value;
}

Platform::IBox<Platform::Guid>^ TestProperties::UuidProperty::get()
{
    return this->nullableUuidValue;
}

void TestProperties::UuidProperty::set(Platform::IBox<Platform::Guid>^ value)
{
    this->nullableUuidValue = value;
}

Windows::Foundation::Uri^ TestProperties::UriProperty::get()
{
    return this->uriValue;
}

void TestProperties::UriProperty::set(Windows::Foundation::Uri^ value)
{
    this->uriValue = value;
}

TestOneWayStruct^ TestProperties::OneWayStructProperty::get()
{
    return ref new TestOneWayStruct(L"test");
}
