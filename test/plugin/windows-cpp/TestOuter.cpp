// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#include "pch.h"
#include "TestOuter.h"

using namespace Microsoft::VisualStudio::C3P::Test;
using namespace Platform;

TestOuter::TestOuter()
{
    this->classValue = nullptr;
    this->structValue = nullptr;
    this->enumValue = TestOuter_InnerEnum::Zero;
}

TestOuter_InnerClass^ TestOuter::InnerClassProperty::get()
{
    return this->classValue;
}

void TestOuter::InnerClassProperty::set(TestOuter_InnerClass^ value)
{
    this->classValue = value;
}

TestOuter_InnerStruct^ TestOuter::InnerStructProperty::get()
{
    return this->structValue;
}

void TestOuter::InnerStructProperty::set(TestOuter_InnerStruct^ value)
{
    this->structValue = value;
}

TestOuter_InnerEnum TestOuter::InnerEnumProperty::get()
{
    return this->enumValue;
}

void TestOuter::InnerEnumProperty::set(TestOuter_InnerEnum value)
{
    this->enumValue = value;
}

TestOuter_InnerClass::TestOuter_InnerClass()
{
    this->value = 0;
}

int TestOuter_InnerClass::Value::get()
{
    return this->value;
}

void TestOuter_InnerClass::Value::set(int value)
{
    this->value = value;
}

TestOuter_InnerStruct::TestOuter_InnerStruct()
{
    this->value = 0;
}

int TestOuter_InnerStruct::Value::get()
{
    return this->value;
}

void TestOuter_InnerStruct::Value::set(int value)
{
    this->value = value;
}
