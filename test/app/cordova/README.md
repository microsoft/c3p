This is a test Cordova application that runs test cases in the test plugin.

## Building tests

1. Build the test plugin and link it for the 'cordova' target, using the C3P CLI:

        c3p compile ios -s ../../plugin
        c3p compile android -s ../../plugin
        c3p link cordova -s ../../plugin

2. Add the desired platform(s) to this cordova app project:

        cordova platform add android
        cordova platform add ios

3. Add the C3P lib plugin (c3p-cordova) to this cordova app project:

        cordova plugin add ../../../src/lib/build/cordova

3. Add the test plugin (c3p-test-cordova) to this cordova app project:

        cordova plugin add ../../plugin/build/cordova

4. Build the app for the desired platform(s):

        cordova build android
        cordova build ios

## Running tests

Launch the test app on a simulator or device with one of the following commands:

        cordova run android --simulator
        cordova run android --device
        cordova run ios --simulator
        cordova run ios --device
        cordova run windows --device

## Debugging
1. Ensure the cordova project is prepared (or built) for the target platform:

        cordova prepare <platform>

2. Open the generated project at `platforms/<platform>` in the corresponding IDE (Android Studio, XCode, or Visual Studio).

3. Select the desired target device or emulator.

4. Build, run, and debug the project using the normal IDE functions.

### Updating plugin code

After changing code in the C3P lib, use the following commands to update the app:

        cordova plugin remove c3p-cordova
        c3p pack cordova -s ../../../src/lib
        cordova plugin add ../../../src/lib/build/cordova

After changing code in the test plugin, use the following commands to update the app:

        cordova plugin remove c3p-test-cordova
        c3p compile <platform> -s ../../plugin
        c3p link cordova -s ../../plugin
        cordova plugin add ../../plugin/build/cordova

