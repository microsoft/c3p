// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Android.App;
using Android.OS;

namespace Microsoft.C3P
{
    /// <summary>
    /// Provides access to the Application and Activity context for Xamarin plugins on Android.
    /// </summary>
    /// <remarks>This code is included with the generated Android adapter code for the Xamarin plugin.</remarks>
    public static class PluginContext
    {
        public static void Initialize(Object application)
        {
            PluginContext.Application = application as Application;
            if (PluginContext.Application == null)
            {
                throw new ArgumentException("An instance of " + typeof(Application).FullName + " is required.");
            }

            PluginContext.Application.RegisterActivityLifecycleCallbacks(new ActivityLifecycleListener());
        }

        internal static Application Application
        {
            get;
            private set;
        }

        internal static Activity CurrentActivity
        {
            get;
            private set;
        }

        class ActivityLifecycleListener : Java.Lang.Object, Application.IActivityLifecycleCallbacks
        {

            public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
            {
            }

            public void OnActivityDestroyed(Activity activity)
            {
            }

            public void OnActivityPaused(Activity activity)
            {
                PluginContext.CurrentActivity = null;
            }

            public void OnActivityResumed(Activity activity)
            {
                PluginContext.CurrentActivity = activity;
            }

            public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
            {
            }

            public void OnActivityStarted(Activity activity)
            {
            }

            public void OnActivityStopped(Activity activity)
            {
            }
        }
    }
}

