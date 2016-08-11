// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

module.exports = function(context) {
    var title = "C3P Cordova Tests";
    var stringsResourcePath = "platforms/android/res/values/strings.xml"

    var fs = context.requireCordovaModule('fs');
    var path = context.requireCordovaModule('path');

    var rootdir = context.opts.projectRoot;

    function replaceTextInFile(filename, findText, replaceText) {
        var contents = fs.readFileSync(filename, 'utf8');
        var updatedContents = contents.replace(findText, replaceText);
        fs.writeFileSync(filename, updatedContents, 'utf8');
    }

    replaceTextInFile(path.join(rootdir, stringsResourcePath), "C3PTestApp", title);
};
