// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#include "pch.h"
#include "TestOneWayStruct.h"

using namespace Microsoft::VisualStudio::C3P::Test;
using namespace Platform;

TestOneWayStruct::TestOneWayStruct(Platform::String^ initialValue)
{
    this->value = initialValue;
}

Platform::String^ TestOneWayStruct::Value::get()
{
    return this->value;
}

