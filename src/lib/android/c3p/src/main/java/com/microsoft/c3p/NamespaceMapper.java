// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

package com.microsoft.c3p;

import android.app.Activity;
import android.app.Application;
import android.net.Uri;
import android.util.Log;

import java.util.Date;
import java.util.HashMap;
import java.util.UUID;

/**
 * Handles mapping mappings between JavaScript namespaces and Java packages, and classes within
 * them. While the JavaScript language technically doesn't have namespaces, the JavaScript bridge
 * here enforces namespace semantics to avoid naming collisions among multiple libraries.
 */
public final class NamespaceMapper {
    public static final String appClassPlaceholder = "<application>";
    public static final String windowClassPlaceholder = "<window>";
    public static final String uuidClassPlaceholder = "<uuid>";
    public static final String uriClassPlaceholder = "<uri>";
    public static final String dateClassPlaceholder = "<date>";

    private static final String TAG = "JavaScriptBridge";

    private HashMap<String, String> packagesToNamespaces;
    private HashMap<String, String> namespacesToPackages;

    public NamespaceMapper() {
        this.packagesToNamespaces = new HashMap<String, String>();
        this.namespacesToPackages = new HashMap<String, String>();
    }

    public void register(String javaScriptNamespace, String javaPackage) {
        this.namespacesToPackages.put(javaScriptNamespace, javaPackage);
        this.packagesToNamespaces.put(javaPackage, javaScriptNamespace);
        Log.d(TAG, "Registered namespace mapping: " +
                javaScriptNamespace + " <=> " + javaPackage);
    }

    public String getJavaPackageForJavaScriptNamespace(String javaScriptNamespace) {
        String javaPackageName = this.namespacesToPackages.get(javaScriptNamespace);

        if (javaPackageName == null) {
            throw new IllegalArgumentException(
                    "No Java package mapping was found for JavaScript namespace: " +
                            javaScriptNamespace);
        }

        return javaPackageName;
    }

    public String getJavaScriptNamespaceForJavaPackage(String javaPackageName) {
        String javaScriptNamespaceName = this.packagesToNamespaces.get(javaPackageName);

        if (javaScriptNamespaceName == null) {
            throw new IllegalArgumentException(
                    "No JavaScript namespace mapping was found for Java package: " +
                            javaPackageName);
        }

        return javaScriptNamespaceName;
    }

    public String getJavaClassForJavaScriptClass(String javaScriptClassFullName) {
        int lastDot = javaScriptClassFullName.lastIndexOf('.');
        if (lastDot < 0) {
            if (appClassPlaceholder.equals(javaScriptClassFullName)) {
                return Application.class.getName();
            } else if (windowClassPlaceholder.equals(javaScriptClassFullName)) {
                return Activity.class.getName();
            } else if (uuidClassPlaceholder.equals(javaScriptClassFullName)) {
                return UUID.class.getName();
            } else if (uriClassPlaceholder.equals(javaScriptClassFullName)) {
                return Uri.class.getName();
            } else if (dateClassPlaceholder.equals(javaScriptClassFullName)) {
                return Date.class.getName();
            } else {
                return javaScriptClassFullName;
            }
        }

        String javaScriptNamespace = javaScriptClassFullName.substring(0, lastDot);
        String javaPackage = this.getJavaPackageForJavaScriptNamespace(javaScriptNamespace);
        return javaPackage + javaScriptClassFullName.substring(lastDot);
    }

    public String getJavaScriptClassForJavaClass(String javaClassFullName) {
        int lastDot = javaClassFullName.lastIndexOf('.');
        if (lastDot < 0) {
            return javaClassFullName;
        } else if (Application.class.getName().equals(javaClassFullName)) {
            return appClassPlaceholder;
        } else if (Activity.class.getName().equals(javaClassFullName)) {
            return windowClassPlaceholder;
        } else if (UUID.class.getName().equals(javaClassFullName)) {
            return uuidClassPlaceholder;
        } else if (javaClassFullName.startsWith(Uri.class.getName())) {
            return uriClassPlaceholder;
        } else if (Date.class.getName().equals(javaClassFullName)) {
            return dateClassPlaceholder;
        }

        String javaPackage = javaClassFullName.substring(0, lastDot);
        String javaScriptNamespace = this.getJavaScriptNamespaceForJavaPackage(javaPackage);
        return javaScriptNamespace + javaClassFullName.substring(lastDot);
    }

    public String getJavaMemberForJavaScriptMember(String javaScriptMemberName) {
        return Character.toLowerCase(javaScriptMemberName.charAt(0)) + javaScriptMemberName.substring(1);
    }

    public String getJavaScriptMemberForJavaMember(String javaMemberName) {
        return Character.toUpperCase(javaMemberName.charAt(0)) + javaMemberName.substring(1);
    }
}
