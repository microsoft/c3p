// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#include "pch.h"
#include "TestStruct.h"

using namespace Microsoft::VisualStudio::C3P::Test;
using namespace Platform;
using namespace Windows::Globalization::DateTimeFormatting;

TestStruct::TestStruct()
{
}

TestStruct::TestStruct(Platform::IBox<Windows::Foundation::DateTime>^ initialValue)
{
    this->value = initialValue;
}

Platform::IBox<Windows::Foundation::DateTime>^ TestStruct::Value::get()
{
    return this->value;
}

void TestStruct::Value::set(Platform::IBox<Windows::Foundation::DateTime>^ value)
{
    this->value = value;
}

void TestStruct::UpdateValue(Platform::IBox<Windows::Foundation::DateTime>^ value)
{
    this->value = value;
}

Platform::String^ TestStruct::ToXml()
{
    DateTimeFormatter^ dtf = DateTimeFormatter::LongDate::get();
    String^ stringValue = (this->value != nullptr ? dtf->Format(this->value->Value) : StringReference(L""));
    return String::Concat(String::Concat(StringReference(L"<value>"), stringValue), StringReference(L"</value>"));
}
