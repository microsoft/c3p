// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;

namespace Microsoft.C3P
{
    /// <summary>
    /// Wraps a TextWriter and manages tab size and indentation levels.
    /// </summary>
    class CodeWriter : IDisposable
    {
        private TextWriter textWriter;

        public CodeWriter(TextWriter textWriter)
        {
            this.textWriter = textWriter;
            this.TabSize = 4;
        }

        public CodeWriter(string path) : this(File.CreateText(path))
        {
        }

        public int TabSize
        {
            get;
            set;
        }

        public int IndentLevel
        {
            get;
            private set;
        }

        public void Code()
        {
            this.textWriter.WriteLine();
        }

        public void Code(string line)
        {
            this.textWriter.Write(new String(' ', this.TabSize * this.IndentLevel));
            this.textWriter.WriteLine(line.Replace("\t", new String(' ', this.TabSize)));
        }

        public void Code(params string[] lines)
        {
            foreach (string line in lines)
            {
                this.Code(line);
            }
        }

        public CodeWriter Indent()
        {
            CodeWriter indentedWriter = new CodeWriter(this.textWriter);
            indentedWriter.TabSize = this.TabSize;
            indentedWriter.IndentLevel = this.IndentLevel + 1;
            return indentedWriter;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.textWriter.Dispose();
            }
        }
    }
}
