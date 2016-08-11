// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Foundation;
using UIKit;

namespace Microsoft.C3P
{
    /// <summary>
    /// Provides access to the UIApplication and UIWindow context for Xamarin plugins on iOS.
    /// </summary>
    /// <remarks>This code is included with the generated iOS adapter code for the Xamarin plugin.</remarks>
    public static class PluginContext
    {
        public static void Initialize(Object application)
        {
            PluginContext.Application = application as UIApplication;
            if (PluginContext.Application == null)
            {
                throw new ArgumentException("An instance of " + typeof(UIApplication).FullName + " is required.");
            }

            UIApplication.Notifications.ObserveDidFinishLaunching(OnLaunched);
            UIApplication.Notifications.ObserveDidEnterBackground(OnPause);
            UIApplication.Notifications.ObserveWillEnterForeground(OnResume);
        }

        internal static UIApplication Application
        {
            get;
            private set;
        }

        internal static UIWindow CurrentWindow
        {
            get;
            private set;
        }

        static void OnLaunched(Object sender, UIApplicationLaunchEventArgs e)
        {
            PluginContext.CurrentWindow = PluginContext.Application.KeyWindow;
        }

        static void OnPause(Object sender, NSNotificationEventArgs e)
        {
        }

        static void OnResume(Object sender, NSNotificationEventArgs e)
        {
        }
    }
}

