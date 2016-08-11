// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#pragma once

namespace Microsoft { namespace VisualStudio { namespace C3P { namespace Test
{
    // WinRT doesn't allow nested classes, but C3P can infer them based on underscore naming.

    public ref class TestOuter_InnerClass sealed
    {
    public:
        TestOuter_InnerClass();

        property int Value
        {
            int get();
            void set(int);
        }

    private:
        int value;
    };

    public ref class TestOuter_InnerStruct sealed
    {
    public:
        TestOuter_InnerStruct();

        property int Value
        {
            int get();
            void set(int);
        }

    private:
        int value;
    };

    public enum class TestOuter_InnerEnum
    {
        Zero = 0,
        One,
        Two,
        Three,
    };

    public ref class TestOuter sealed
    {
    public:
        TestOuter();

        property TestOuter_InnerClass^ InnerClassProperty
        {
            TestOuter_InnerClass^ get();
            void set(TestOuter_InnerClass^);
        }

        property TestOuter_InnerStruct^ InnerStructProperty
        {
            TestOuter_InnerStruct^ get();
            void set(TestOuter_InnerStruct^);
        }

        property TestOuter_InnerEnum InnerEnumProperty
        {
            TestOuter_InnerEnum get();
            void set(TestOuter_InnerEnum);
        }

    private:
        TestOuter_InnerClass^ classValue;
        TestOuter_InnerStruct^ structValue;
        TestOuter_InnerEnum enumValue;
    };

}}}}