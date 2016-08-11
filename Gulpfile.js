// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

"use strict";

var gulp          = require("gulp");
var gutil         = require("gulp-util");
var newer         = require('gulp-newer');
var fs            = require("fs");
var https         = require("https");
var path          = require("path");
var os            = require("os");
var process       = require("process");
var child_process = require("child_process");
var through       = require('through2');

var srcPath = "src";
var srcLibPath = srcPath + "/lib";
var testPath = "test";
var testPluginPath = testPath + "/plugin";
var testAppPath = testPath + "/app";

var sourceGlobs = {
    cli: [
         srcPath + "/cli/**/*.cs",
         srcPath + "/cli/**/*.csproj",
         srcPath + "/cli/**/*.ts",
         srcPath + "/cli/**/*.config",
         srcPath + "/cli/**/*.json",
     ],
     libandroid: [
         srcLibPath + "/android/**/*.java",
         srcLibPath + "/android/**/*.xml",
         srcLibPath + "/android/**/*.gradle",
     ],
     libios: [
         srcLibPath + "/ios/**/*.h",
         srcLibPath + "/ios/**/*.m",
         srcLibPath + "/ios/**/*.pbxproj",
     ],
     libts: [
         srcLibPath + "/ts/**/*.ts",
         srcLibPath + "/ts/**/*.json",
         srcLibPath + "/ts/**/*.xml",
         srcLibPath + "/ts/**/es6-promise.js",
     ],
     testandroid: [
         testPluginPath + "/plugin.xml",
         testPluginPath + "/android/**/*.java",
         testPluginPath + "/android/**/*.xml",
         testPluginPath + "android/**/*.gradle",
     ],
     testios: [
         testPluginPath + "/plugin.xml",
         testPluginPath + "/ios/**/*.h",
         testPluginPath + "/ios/**/*.m",
         testPluginPath + "/ios/**/*.pbxproj",
     ],
     testwindowscs: [
         testPluginPath + "/plugin.xml",
         testPluginPath + "/windows-cs/**/*.cs",
         testPluginPath + "/windows-cs/**/*.csproj",
         testPluginPath + "/windows-cs/**/*.config",
         testPluginPath + "/windows-cs/**/project.json",
     ],
     testwindowscpp: [
         testPluginPath + "/plugin.xml",
         testPluginPath + "/windows-cpp/**/*.h",
         testPluginPath + "/windows-cpp/**/*.cpp",
         testPluginPath + "/windows-cpp/**/*.vcxproj",
     ],
};

var release = process.argv.indexOf("--release") >= 0;
var configurationOption = (release ? "--release" : "--debug");
var configurationName = (release ? "Release" : "Debug");

// TASKS

gulp.task("default", ["help"]);

gulp.task("help", function () {
    gutil.log("");
    gutil.log("Tasks:");
    gutil.log("  build [configuration]         Build sources and tests");
    gutil.log("  test-<framework>-<platform>   Run automated tests");
    gutil.log("");
    gutil.log("        configurations:  --debug --release");
    gutil.log("        frameworks:      cordova reactnative xamarin");
    gutil.log("        platforms:       android ios windows");
    gutil.log("");
});

gulp.task("build", ["build-lib", "build-test"], function () {
});

// Build the CLI.
gulp.task("build-cli", function () {
    var cliOutputPath = srcPath + "/cli/bin/" + configurationName + "/c3p.exe";

    return gulp.src(sourceGlobs.cli, { read: false })
        .pipe(newer(cliOutputPath))
        .pipe(ifAny(function () {
            msBuild(srcPath + "/cli", release);
        }));
});

// Build the lib plugin for Cordova.
gulp.task("build-lib-cordova", ["build-cli"], function () {
    return gulp.src(
        [].concat(
            sourceGlobs.libandroid,
            sourceGlobs.libios,
            sourceGlobs.libts), { read: false })
        .pipe(newer(srcLibPath + "/build/cordova/package.json"))
        .pipe(ifAny(function () {
            c3pCli(
                ["pack", "cordova", configurationOption],
                srcLibPath);
        }));
});

// Build the lib plugin for React Native.
gulp.task("build-lib-reactnative", ["build-cli"], function () {
    return gulp.src(
        [].concat(
            sourceGlobs.libandroid,
            sourceGlobs.libios,
            sourceGlobs.libts), { read: false })
        .pipe(newer(srcLibPath + "/build/reactnative/package.json"))
        .pipe(ifAny(function () {
            c3pCli(
                ["pack", "reactnative", configurationOption],
                srcLibPath);
        }));
});

gulp.task(
    "build-lib",
    ["build-lib-cordova", "build-lib-reactnative"],
    function () {});

gulp.task("build-test-android", ["build-cli"], function () {
    var dllIntPath = "test/plugin/build/android/bin/" +
        configurationName + "/C3PTestPlugin.dll";
    return gulp.src(sourceGlobs.testandroid, { read: false })
        .pipe(newer(dllIntPath))
        .pipe(ifAny(function () {
            c3pCli(
                ["compile", "android", configurationOption],
                testPluginPath);
        }));
});

gulp.task("build-test-ios", ["build-cli"], function () {
    var dllIntPath = "test/plugin/build/ios/bin/" +
        configurationName + "/C3PTestPlugin.dll";
    return gulp.src(sourceGlobs.testios, { read: false })
        .pipe(newer(dllIntPath))
        .pipe(ifAny(function () {
            c3pCli(
                ["compile", "ios", configurationOption],
                testPluginPath);
        }));
});

gulp.task("build-test-windows-cs", ["build-cli"], function () {
    var dllIntPath = "test/plugin/build/windows/bin/x86/" +
        configurationName + "/C3PTestPlugin.dll";
    return gulp.src(sourceGlobs.testwindowscs, { read: false })
        .pipe(newer(dllIntPath))
        .pipe(ifAny(function () {
            nugetRestore("test/plugin/windows-cs/C3PTestPluginCS.sln");
            c3pCli(
                ["compile", "windows-cs", configurationOption],
                testPluginPath);
        }));
});

gulp.task("build-test-windows-cpp", ["build-cli"], function () {
    var dllIntPath = "test/plugin/build/windows/bin/x86/" +
        configurationName + "/C3PTestPlugin.dll";
    return gulp.src(sourceGlobs.testwindowscpp, { read: false })
        .pipe(newer(dllIntPath))
        .pipe(ifAny(function () {
            nugetRestore("test/plugin/windows-cpp/C3PTestPluginPP.sln");
            c3pCli(
                ["compile", "windows-cpp", configurationOption],
                testPluginPath);
        }));
});

var testCompileTasks = ["build-test-android"];
if (isMac()) testCompileTasks = testCompileTasks.concat("build-test-ios");
if (isWindows()) testCompileTasks = testCompileTasks.concat("build-test-windows-cs");

gulp.task("build-test-cordova", testCompileTasks, function () {
    var dllIntGlob = "test/plugin/build/*/bin/{,@(x86|x64|ARM)/}" +
        configurationName + "/C3PTestPlugin.dll";
    return gulp.src(dllIntGlob, { read: false })
        .pipe(newer("test/plugin/build/cordova/package.json"))
        .pipe(ifAny(function () {
            c3pCli(
                ["link", "cordova", configurationOption],
                testPluginPath);
        }));
});

gulp.task("build-test-reactnative", testCompileTasks, function () {
    var dllIntGlob = "test/plugin/build/*/bin/{,@(x86|x64|ARM)/}" +
        configurationName + "/C3PTestPlugin.dll";
    return gulp.src(dllIntGlob, { read: false })
        .pipe(newer("test/plugin/build/reactnative/package.json"))
        .pipe(ifAny(function () {
            c3pCli(
                ["link", "reactnative", configurationOption],
                testPluginPath);
        }));
});

gulp.task("build-test-xamarin", testCompileTasks, function () {
    var dllIntGlob = "test/plugin/build/*/bin/{,@(x86|x64|ARM)/}" +
        configurationName + "/C3PTestPlugin.dll";
    return gulp.src(dllIntGlob, { read: false })
        .pipe(newer("test/plugin/build/xamarin/C3PTestPlugin.nuspec"))
        .pipe(ifAny(function () {
            c3pCli(
                ["link", "xamarin", configurationOption],
                testPluginPath);
        }));
});

gulp.task(
    "build-test",
    ["build-test-cordova", "build-test-reactnative", "build-test-xamarin"],
    function () {});

gulp.task("test-cordova-android", function (cb) {
    var appPath = testAppPath + "/cordova";
    addCordovaPlatformIfMissing(appPath, "android");
    addCordovaPlugin(appPath, "c3p-cordova", "src/lib/build/cordova");
    addCordovaPlugin(appPath, "c3p-test-cordova", "test/plugin/build/cordova");
    addCordovaPlugin(appPath, "cordova-plugin-console");
    startAndroidEmulatorIfNotRunning();
    execCmdSync("cordova",
        [
            "build", "android",
            configurationOption,
            "--emulator",
        ],
        { cwd: appPath });
    runAndroidTest(
        appPath,
        "platforms/android/build/outputs/apk/android-debug.apk",
        "com.microsoft.c3p.test.cordova",
        cb);
});

gulp.task("test-cordova-ios", function (cb) {
    var appPath = testAppPath + "/cordova";
    addCordovaPlatformIfMissing(appPath, "ios");
    addCordovaPlugin(appPath, "c3p-cordova", "src/lib/build/cordova");
    addCordovaPlugin(appPath, "c3p-test-cordova", "test/plugin/build/cordova");
    addCordovaPlugin(appPath, "cordova-plugin-console");
    execCmdSync("cordova",
        [
            "build", "ios",
            configurationOption,
            "--emulator",
        ],
        { cwd: appPath });
    runIOSTest(appPath, "platforms/ios/build/emulator/C3PTestApp.app", cb);
});

gulp.task("test-cordova-windows", function () {
    var appPath = testAppPath + "/cordova";
    addCordovaPlatformIfMissing(appPath, "windows");
    addCordovaPlugin(appPath, "c3p-cordova", "src/lib/build/cordova");
    addCordovaPlugin(appPath, "c3p-test-cordova", "test/plugin/build/cordova");
    addCordovaPlugin(appPath, "cordova-plugin-console");
    execCmdSync("cordova",
        [
            "run", "windows",
            "--archs=x64",
            configurationOption,
        ],
        { cwd: appPath });
});

gulp.task("test-reactnative-android", function () {
    var appPath = testAppPath + "/reactnative";
    removeNpmPackageIfPresent(appPath, "c3p-test-reactnative");
    removeNpmPackageIfPresent(appPath, "c3p-reactnative");
    addNpmPackage(appPath, "c3p-reactnative");
    addNpmPackage(appPath, "c3p-test-reactnative");
    execCmdSync(
        "react-native", [ "run-android" ],
        { cwd: appPath });
});

gulp.task("test-reactnative-ios", function () {
    var appPath = testAppPath + "/reactnative";
    removeNpmPackageIfPresent(appPath, "c3p-test-reactnative");
    removeNpmPackageIfPresent(appPath, "c3p-reactnative");
    addNpmPackage(appPath, "c3p-reactnative");
    addNpmPackage(appPath, "c3p-test-reactnative");
    execCmdSync(
        "react-native", [ "run-ios" ],
        { cwd: appPath });
});

gulp.task("test-xamarin-android", function () {
    gutil.log(gutil.colors.red.bold("Launching the Xamarin test app is not implemented."));
});

gulp.task("test-xamarin-ios", function () {
    gutil.log(gutil.colors.red.bold("Launching the Xamarin test app is not implemented."));
});

gulp.task("test-xamarin-windows", function () {
    gutil.log(gutil.colors.red.bold("Launching the Xamarin test app is not implemented."));
});

// PRIVATE HELPER FUNCTIONS

gutil.env.nonZeroFatal = true;

function isWindows() {
    return os.platform() === "win32";
}

function isMac() {
    return os.platform() === "darwin";
}

function getSupportedTargetPlatforms() {
    var platforms = ["android"];
    if (isMac()) {
        platforms.push("ios");
    }
    if (isWindows()) {
        platforms.push("windows");
    }

    return platforms;
}

function execSync(command, args, options, checkExit) {
    gutil.log(gutil.colors.green([command].concat(args).join(" ")));

    if (typeof(options) === "undefined") {
        options = {};
    }
    if (typeof(options.stdio) === "undefined") {
        options.stdio = "inherit";
    }

    var returnState = child_process.spawnSync(command, args, options);
    var exitCode = returnState.status;
    var signal = returnState.signal;

    if (exitCode === null) {
        gutil.log(gutil.colors.red("Command not found: " + command));
        process.exit(1);
    }

    if (checkExit ? !checkExit(exitCode) : (exitCode !== 0)) {
        var exitMessage = "Command " + command + " exited";
        if (exitCode !== null) {
            exitMessage += " with code " + exitCode + ".";
        }
        if (signal !== null) {
            exitMessage += " because it got killed by a " + signal + " signal.";
        }
        gutil.log(gutil.colors.red(exitMessage));

        if (gutil.env.nonZeroFatal === true) {
            process.exit(1);
        }
    }

    return returnState;
}

function ifAny(op) {
    var foundAny = false;
    return through.obj(
        function (file, enc, cb) {
            foundAny = true;
            cb(null, file);
        },
        function (cb) {
            if (foundAny) {
                op();
            }
            cb();
        });
}

function execCmdSync(cmd, args, options) {
    if (!options) options = {};
    if (isWindows()) {
        cmd += ".cmd";
        options.shell = true;
    }
    return execSync(cmd, args, options);
}

function c3pCli(args, workingDirectory) {
    var exePath = process.cwd() + "\\src\\cli\\bin\\" + configurationName + "\\c3p.exe";
    if (isWindows()) {
        execSync(
            exePath,
            args,
            { cwd: workingDirectory });
    } else {
        execSync(
            "mono",
            ["--debug", exePath].concat(args),
            { cwd: workingDirectory });
    }
}

function nugetRestore(solutionFile) {
    var nugetToolPath = null;
    if (isMac()) {
        nugetToolPath = "/usr/local/bin/nuget";
    } else if (isWindows()) {
        var externalBuildDir = process.cwd() + "\\external\\build";
        var nugetToolPath = externalBuildDir + "\\nuget.exe";
        if (!fs.existsSync(nugetToolPath)) {
            if (!fs.exists(externalBuildDir)) {
                fs.mkdirSync(externalBuildDir);
            }

            var nugetToolUri = "https://dist.nuget.org/win-x86-commandline/v3.3.0/nuget.exe";
            execSync("powershell.exe",
            	["-NoProfile", "-Command", "& {Invoke-WebRequest -Uri '" + nugetToolUri + "' -OutFile nuget.exe}"],
                { cwd: externalBuildDir });
        }
    }

    if (nugetToolPath) {
        execSync(
            nugetToolPath,
            ["restore", path.basename(solutionFile)],
            { cwd: path.dirname(solutionFile) });
    }
}

function msBuild(projectDirectory, release) {
    if (isMac()) {
        execSync(
            "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool",
            ["build", "-c:" + configurationName],
            { cwd: projectDirectory });
    } else if (isWindows()) {
        execSync(
            process.env['ProgramFiles(x86)'] + "\\MSBuild\\14.0\\bin\\MSBuild.exe",
            ["/t:Build", "/p:Configuration=" + configurationName],
            { cwd: projectDirectory });
    }
}

function xcodeBuild(projectDirectory, release, sdk) {
    execSync(
        "/usr/bin/xcodebuild",
        [
            "-configuration", configurationName,
            "-sdk", sdk,
            "build"
        ],
        { cwd: projectDirectory });
}

function gradleBuild(projectDirectory, release) {
    var cmd = (isWindows() ? "./gradlew.bat" : "./gradlew");
    execSync(
        cmd,
        ["build", (release ? "assembleRelease" : "assembleDebug")],
        { cwd: projectDirectory });
}

function cordovaAppHasPlatform(appDirectory, platform) {
    var listResult = execCmdSync("cordova",
        ["platform", "list"], { cwd: appDirectory, stdio: null });

    // Strip off the listed available platforms
    var installedPlatforms = listResult.stdout.toString().replace(/\nAvailable (.|\n)+/, "");

    var hasPlatform = new RegExp("^ *" + platform + " .*$", "m").test(installedPlatforms);
    return hasPlatform;
}

function cordovaAppHasPlugin(appDirectory, plugin) {
    var listResult = execCmdSync("cordova",
        ["plugin", "list"], { cwd: appDirectory, stdio: null });
    var hasPlugin = new RegExp("^" + plugin + " .*$", "m").test(listResult.stdout);
    return hasPlugin;
}

function addCordovaPlatform(appDirectory, platform) {
    execCmdSync("cordova", ["platform", "add", platform], { cwd: appDirectory });
}

function addCordovaPlugin(appDirectory, plugin, sourcePath) {
    var isPluginInstalled;
    var pluginPath = appDirectory + "/plugins/" + plugin;
    try {
        isPluginInstalled = fs.statSync(pluginPath).isDirectory();
    } catch (e) {
        isPluginInstalled = false;
    }

    if (isPluginInstalled && sourcePath) {
        var pluginInstallTime = fs.statSync(pluginPath + "/plugin.xml").mtime.getTime();
        var sourceTime = fs.statSync(sourcePath + "/plugin.xml").mtime.getTime();
        if (pluginInstallTime < sourceTime) {
            execCmdSync("cordova", ["plugin", "remove", plugin], { cwd: appDirectory });
            isPluginInstalled = false;
        }
    }

    if (!isPluginInstalled) {
        execCmdSync("cordova", ["plugin", "add", plugin], { cwd: appDirectory });
    }
}

function addCordovaPlatformIfMissing(appDirectory, platform) {
    if (!cordovaAppHasPlatform(appDirectory, platform)) {
        addCordovaPlatform(appDirectory, platform);
    }
}

function appHasNpmPackage(appDirectory, packageId) {
    try {
        var modulePath = appDirectory + "/node_modules/" + packageId;
        return fs.statSync(modulePath).isDirectory();
    } catch (e) {
        return false;
    }
}

function addNpmPackage(appDirectory, packageId) {
    execCmdSync("npm", ["install", packageId], { cwd: appDirectory });
}

function removeNpmPackage(appDirectory, packageId) {
    execCmdSync("npm", ["remove", packageId], { cwd: appDirectory });
}

function removeNpmPackageIfPresent(appDirectory, packageId) {
    if (appHasNpmPackage(appDirectory, packageId)) {
        removeNpmPackage(appDirectory, packageId);
    }
}

function startAndroidEmulatorIfNotRunning() {
    // TODO
}

function testLog(logLine, done) {
    if (logLine.startsWith("PASSED: ")) {
        gutil.log(gutil.colors.green(logLine));
    } else if (logLine.startsWith("FAILED: ")) {
        gutil.log(gutil.colors.red.bold(logLine));
    } else if (logLine.startsWith("ERROR: ")) {
        gutil.log(gutil.colors.magenta.bold(logLine));
        done();
    } else if (logLine.startsWith("RESULTS: ")) {
        gutil.log(gutil.colors.cyan.bold(logLine));
        done();
    }
}

function runAndroidTest(appDirectory, apkPath, appPackage, cb) {
    execSync(
        "adb",
        ["install", "-r", apkPath],
        { cwd: appDirectory });
    execSync("adb", ["logcat", "-c"], { stdio: null });
    execSync("adb", ["shell", "am", "start", appPackage + "/.MainActivity"]);

    var proc = child_process.spawn("adb", [ "logcat" ]);

    proc.stdout.on('data', function (data) {
        data = data.toString().trim();
        var dataLines = data.split("\n");
        for (var i = 0; i < dataLines.length; i++) {
            var dataLine = dataLines[i].trim();
            var chromiumIndex = dataLine.indexOf("chromium: [");
            if (chromiumIndex > 0) {
                var bracketIndex = dataLine.indexOf("] \"", chromiumIndex);
                if (bracketIndex > 0) {
                    var sourceIndex = dataLine.indexOf("\", source: ", bracketIndex);
                    if (sourceIndex > 0) {
                        var logLine = dataLine.substr(
                            bracketIndex + 3, (sourceIndex - bracketIndex - 3));
                        testLog(logLine, proc.kill.bind(proc));
                    }
                }
            }
        }
    });
    proc.stderr.on('data', function (data) {
        gutil.log(gutil.colors.red(data));
    });
    proc.on('close', function (exitCode) {
        cb();
    });
    proc.on('error', function (err) {
        gutil.log("Failed to connect to Android simulator logcat: " + err);
        cb();
    });
}

function runIOSTest(appDirectory, appBuildDirectory, cb) {
    var proc = child_process.spawn(
        "ios-sim",
        [
            "launch",
            appBuildDirectory,
            "--devicetypeid", "iPhone-6s",
        ],
        { cwd: appDirectory });

    proc.stdout.on('data', function (data) {
        data = data.toString();
        var dataLines = data.split("\n");
        for (var i = 0; i < dataLines.length; i++) {
            var dataLine = dataLines[i].trim();
            var appLabelIndex = dataLine.indexOf("C3PTestApp[");
            if (appLabelIndex > 0) {
                var bracketIndex = dataLine.indexOf("]: ", appLabelIndex);
                if (bracketIndex > 0) {
                    var logLine = dataLine.substring(bracketIndex + 3);
                    testLog(logLine, proc.kill.bind(proc));
                }
            }
        }
    });
    proc.stderr.on('data', function (data) {
        gutil.log(gutil.colors.red(data));
    });
    proc.on('close', function (exitCode) {
        cb();
    });
    proc.on('error', function (err) {
        gutil.log("Failed to launch test app on iOS simulator: " + err);
        cb();
    });
}
