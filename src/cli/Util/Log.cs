// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Diagnostics;

namespace Microsoft.C3P
{
    /// <summary>
    /// Logs to the console with coloring based on the level of each message.
    /// </summary>
    static class Log
    {
        public static bool IsVerboseOutputEnabled { get; set; }

        public static void Verbose(string format, params object[] args)
        {
            if (Log.IsVerboseOutputEnabled)
            {
                Debug.WriteLine(format ?? String.Empty, args);
                try
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(format ?? String.Empty, args);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        public static void Message(string format, params object[] args)
        {
            Debug.WriteLine(format ?? String.Empty, args);
            Console.WriteLine(format ?? String.Empty, args);
        }

        public static void Important(string format, params object[] args)
        {
            Debug.WriteLine(format ?? String.Empty, args);
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(format ?? String.Empty, args);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static void Warning(string format, params object[] args)
        {
            Debug.WriteLine(format ?? String.Empty, args);
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(format ?? String.Empty, args);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static void Error(string format, params object[] args)
        {
            Debug.WriteLine(format ?? String.Empty, args);
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(format ?? String.Empty, args);
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}
