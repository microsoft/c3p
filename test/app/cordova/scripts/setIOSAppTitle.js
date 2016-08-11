// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

module.exports = function(context) {
    var title = "C3P Cordova Tests";
    var plistPath = "platforms/ios/C3PTestApp/C3PTestApp-Info.plist";

    var fs = context.requireCordovaModule('fs');
    var path = context.requireCordovaModule('path');

    var rootdir = context.opts.projectRoot;

    function replaceTextInFile(filename, findText, replaceText) {
        var contents = fs.readFileSync(filename, 'utf8');
        var updatedContents = contents.replace(findText, replaceText);
        fs.writeFileSync(filename, updatedContents, 'utf8');
    }

    replaceTextInFile(path.join(rootdir, plistPath), "${PRODUCT_NAME}", title);
};
