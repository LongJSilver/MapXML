using System;
using System.IO;
using System.Text;

namespace MapXML.Utils
{
    /// <summary>
    /// StringWriter_WithEncoding is a subclass of StringWriter that allows the caller to set the encoding.
    /// This is necessary because the default StringWriter uses UTF-16 encoding, and there is no direct way to change it.
    /// </summary>
    public class StringWriter_WithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        /// <summary>
        /// Initializes a new instance of the StringWriter_WithEncoding class with the specified encoding.
        /// </summary>
        /// <param name="encoding">The encoding to use.</param>
        public StringWriter_WithEncoding(Encoding encoding)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        /// <summary>
        /// Initializes a new instance of the StringWriter_WithEncoding class with the specified format provider and encoding.
        /// </summary>
        /// <param name="formatProvider">An IFormatProvider object that controls formatting.</param>
        /// <param name="encoding">The encoding to use.</param>
        public StringWriter_WithEncoding(IFormatProvider formatProvider, Encoding encoding)
            : base(formatProvider)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        /// <summary>
        /// Initializes a new instance of the StringWriter_WithEncoding class with the specified StringBuilder and encoding.
        /// </summary>
        /// <param name="sb">The StringBuilder to write to.</param>
        /// <param name="encoding">The encoding to use.</param>
        public StringWriter_WithEncoding(StringBuilder sb, Encoding encoding)
            : base(sb)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        /// <summary>
        /// Initializes a new instance of the StringWriter_WithEncoding class with the specified StringBuilder, format provider, and encoding.
        /// </summary>
        /// <param name="sb">The StringBuilder to write to.</param>
        /// <param name="formatProvider">An IFormatProvider object that controls formatting.</param>
        /// <param name="encoding">The encoding to use.</param>
        public StringWriter_WithEncoding(StringBuilder sb, IFormatProvider formatProvider, Encoding encoding)
            : base(sb, formatProvider)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        /// <summary>
        /// Gets the encoding in which the output is written.
        /// </summary>
        public override Encoding Encoding => _encoding;
    }
}