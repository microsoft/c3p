// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace Microsoft.C3P
{
    /// <summary>
    /// When applied to a constructor or method, indicates the projection of that
    /// constructor or method omits the first parameter; the value for that
    /// parameter is supplied implicitly by C3P at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    class ImplicitContextAttribute : System.Attribute
    {
        public const string Application = "application";
        public const string Window = "window";

        public ImplicitContextAttribute(string contextKind)
        {
            this.ContextKind = contextKind;
        }

        /// <summary>
        /// Gets the kind of the implicit context, such as Application or Window.
        /// </summary>
        public string ContextKind { get; private set; }
    }
}
