// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#pragma once

namespace Microsoft { namespace VisualStudio { namespace C3P { namespace Test
{
    ref class TestStruct;
    ref class TestOneWayStruct;
    enum class TestEnum;

    public ref class TestProperties sealed
    {
    public:
        TestProperties();

        static property TestStruct^ StaticStructProperty
        {
            TestStruct^ get();
            void set(TestStruct^);
        }

        static property Windows::Foundation::Collections::IVector<Platform::String^>^ StaticListProperty
        {
            Windows::Foundation::Collections::IVector<Platform::String^>^ get();
            void set(Windows::Foundation::Collections::IVector<Platform::String^>^);
        }

        static property double StaticDoubleProperty
        {
            double get();
            void set(double);
        }

        static property TestEnum StaticEnumProperty
        {
            TestEnum get();
            void set(TestEnum);
        }

        static property bool StaticBoolProperty
        {
            bool get();
            void set(bool);
        }

        property TestStruct^ StructProperty
        {
            TestStruct^ get();
            void set(TestStruct^);
        }

        property Windows::Foundation::Collections::IVector<Platform::String^>^ ListProperty
        {
            Windows::Foundation::Collections::IVector<Platform::String^>^ get();
            void set(Windows::Foundation::Collections::IVector<Platform::String^>^);
        }

        property Windows::Foundation::Collections::IVector<Platform::String^>^ ReadonlyListProperty
        {
            Windows::Foundation::Collections::IVector<Platform::String^>^ get();
        }

        property double DoubleProperty
        {
            double get();
            void set(double);
        }

        property int ReadonlyIntProperty
        {
            int get();
        }

        property TestEnum EnumProperty
        {
            TestEnum get();
            void set(TestEnum);
        }

        property bool BoolProperty
        {
            bool get();
            void set(bool);
        }

        property Platform::IBox<int>^ NullableIntProperty
        {
            Platform::IBox<int>^ get();
            void set(Platform::IBox<int>^);
        }

        property Platform::IBox<double>^ NullableDoubleProperty
        {
            Platform::IBox<double>^ get();
            void set(Platform::IBox<double>^);
        }

        property Platform::IBox<Platform::Guid>^ UuidProperty
        {
            Platform::IBox<Platform::Guid>^ get();
            void set(Platform::IBox<Platform::Guid>^);
        }

        property Windows::Foundation::Uri^ UriProperty
        {
            Windows::Foundation::Uri^ get();
            void set(Windows::Foundation::Uri^);
        }

        property TestOneWayStruct^ OneWayStructProperty
        {
            TestOneWayStruct^ get();
        }

    private:
        static TestStruct^ staticStructValue;
        static Windows::Foundation::Collections::IVector<Platform::String^>^ staticListValue;
        static double staticDoubleValue;
        static TestEnum staticEnumValue;
        static bool staticBoolValue;
        Windows::Foundation::Collections::IVector<Platform::String^>^ readonlyListValue;
        TestStruct^ structValue;
        Windows::Foundation::Collections::IVector<Platform::String^>^ listValue;
        double doubleValue;
        TestEnum enumValue;
        bool boolValue;
        Platform::IBox<int>^ nullableIntValue;
        Platform::IBox<double>^ nullableDoubleValue;
        Platform::IBox<Platform::Guid>^ nullableUuidValue;
        Windows::Foundation::Uri^ uriValue;
    };

}}}}