# Cross-Platform Plugin Packager

## Overview
The Cross-Platform Plugin Packager (**C3P**) tool compiles native code for Android, iOS, and Windows into plugins for mobile application frameworks Cordova, React Native, and Xamarin.

## Windows Prerequisites
On Windows you can build plugins for Windows and Android devices.

 * Visual Studio 2015 (VS Community edition should be fine)
 * Latest version of Xamarin Android tools installed with VS.
 * Latest version of Android Studio, with **SDK Platforms 19 and 23** and **SDK Build-tools 23.0.3** installed via the SDK manager.

## Mac OS X Prerequisites
On Mac OS X you can build plugins for iOS and Android devices.

 * Latest version of XCode.
 * Latest version of Android Studio, with **SDK Platforms 19 and 23** and **SDK Build-tools 23.0.1 and 23.0.3** installed via the SDK manager.
 * Latest version of Xamarin Studio (6.0), with Xamarin.Android and Xamarin.iOS.
 * Xamarin's [Objective Sharpie](https://developer.xamarin.com/guides/cross-platform/macios/binding/objective-sharpie/getting-started/) tool

## How to Build a Universal Plugin
Here is a brief overview of the process. For more details, **read the [Plugin Development Guide](doc/PluginDevGuide.md)**.

1. Create a plugin layout as follows, with native-code projects for each platform. Each platform is optional,
   but you must have at least one.

        MyPlugin/
            plugin.xml
            android/
                (Android Studio project files and Java code)
            ios
                (XCode project files and Obj-C code)
            windows/
                (VS project files and C# or C++/CX code)

2. Ensure the *public* APIs for each platform are equivalent to other platforms and fit within the limitations for
   universal plugin APIs.
3. Create a plugin.xml file at the root with metada about the plugin.
4. Build and run the c3p tool (src/C3P.sln).

The command usage is:

    c3p compile <platform> [-s <source path>] [-i <int. path>] [options]
    c3p link <target> [-s <source path>] [-i <int. path> ...] [-o <output path>] [options]

For more help:

    c3p --help

For example, to run it on the included test plugin:

(On a Mac OS X host)

    cd test/plugin
    c3p compile ios

(On a Windows host)

    cd test/plugin
    c3p compile android
    c3p compile windows

(On the same Windows host, accessing the Mac host via a network path)

    cd test/plugin
    c3p link cordova     -i \\mymac\home\c3p\test\plugin\build
    c3p link reactnative -i \\mymac\home\c3p\test\plugin\build
    c3p link xamarin     -i \\mymac\home\c3p\test\plugin\build

If all goes well, it should produce plugins under `test/plugin/build/cordova`, `test/plugin/build/reactnative`, and
`test/plugin/build/xamarin`. Cordova and React Native plugins are packed as npm packages. Xamarin plugins are packed
as NuGet packages. They are all ready to [install into Cordova, React Native, or Xamarin app projects](doc/Installing.md).

## How it works
The process is divided into two phases, which are roughly analogous to typical "compile" and "link" phases.

The "compile" phase, repeated for each platform:

1. Compile the native code project for Android, iOS, or Windows.
2. (Android & iOS platforms) Use Xamarin tools to generate C# bindings for the native code.
3. Analyze the generated bindings (or the built assembly for Windows), and then add additional binding metadata and some
   C# wrapper code to adapt the results into APIs that can be consistent across platforms.
4. Compile the C# bindings and adapter code into a platform-specific assembly with a portable API (to be used for Xamarin
   bait-and-switch).
5. Reflect over that built assembly and export API metadata to an api.xml file (to be used in the link phase).

The "link" phase, repeated for each target application framework:

6. Load the API metadata for each of the platforms from the previously-generated api.xml files.
7. Verify the APIs are consistent across platforms. (Per-platform omissions are allowed, but overlaps must be consistent.)
8. (Xamarin target) Generate a platform-independent portable assembly with all the API metadata (but no implementations).
9. (JS targets) Generate TypeScript bindings for all the portable APIs.
10. (JS targets) Compile the TypeScript bindings to JavaScript + type definitions.
11. Create plugin packages for the targeted cross-platform framework, with package metadata transformed from plugin.xml.

If the plugin targets both Windows and iOS then the compile phase needs to be performed separately on Windows and Mac OS.
After that the results can be linked together on either OS.

