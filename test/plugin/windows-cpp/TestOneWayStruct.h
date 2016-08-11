// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#pragma once

namespace Microsoft { namespace VisualStudio { namespace C3P { namespace Test
{

    public ref class TestOneWayStruct sealed
    {
    internal:
        TestOneWayStruct(Platform::String^ initialValue);

    public:
        property Platform::String^ Value
        {
            Platform::String^ get();
        }

    private:
        Platform::String^ value;
    };

}}}}