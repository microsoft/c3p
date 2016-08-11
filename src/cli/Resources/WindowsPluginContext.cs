// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Microsoft.C3P
{
    /// <summary>
    /// Provides access to the CoreWindow context for Xamarin plugins on Windows.
    /// </summary>
    /// <remarks>This code is included with the generated Windows adapter code for the Xamarin plugin.</remarks>
    public static class PluginContext
    {
        public static void Initialize(object application)
        {
        }

        internal static object Application
        {
            get
            {
                // There is no application context instance on Windows, because
                // the CoreApplication class is static.
                return null;
            }
        }

        internal static CoreWindow CurrentWindow
        {
            get
            {
                return CoreApplication.MainView?.CoreWindow;
            }
        }
    }
}

