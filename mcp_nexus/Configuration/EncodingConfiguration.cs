using System.Diagnostics;
using System.Text;

namespace mcp_nexus.Configuration
{
    /// <summary>
    /// Centralized encoding configuration for all external interfaces
    /// Ensures consistent UTF-8 encoding across:
    /// - CDB process streams (stdin, stdout, stderr)
    /// - Console streams (stdout, stderr) 
    /// - HTTP responses (Content-Type charset)
    /// - JSON serialization
    /// </summary>
    public static class EncodingConfiguration
    {
        /// <summary>
        /// The standard encoding for all external text interfaces
        /// UTF-8 without BOM for maximum compatibility
        /// </summary>
        public static Encoding DefaultEncoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Content-Type charset parameter for HTTP responses
        /// </summary>
        public const string HttpCharset = "charset=utf-8";

        /// <summary>
        /// Gets the full Content-Type header value for JSON responses
        /// </summary>
        public static string JsonContentType => $"application/json; {HttpCharset}";

        /// <summary>
        /// Gets the full Content-Type header value for plain text responses
        /// </summary>
        public static string TextContentType => $"text/plain; {HttpCharset}";

        /// <summary>
        /// Gets the full Content-Type header value for HTML responses
        /// </summary>
        public static string HtmlContentType => $"text/html; {HttpCharset}";

        /// <summary>
        /// Configures console encoding for stdio mode
        /// Sets both stdout and stderr to UTF-8
        /// </summary>
        public static void ConfigureConsoleEncoding()
        {
            Console.OutputEncoding = DefaultEncoding;
            Console.InputEncoding = DefaultEncoding;
            // Note: Console.Error encoding is set via Console.OutputEncoding
        }

        /// <summary>
        /// Creates a ProcessStartInfo with UTF-8 encoding configured for all streams
        /// </summary>
        public static ProcessStartInfo CreateUtf8ProcessStartInfo(string fileName, string arguments = "")
        {
            return new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                StandardInputEncoding = DefaultEncoding,
                StandardOutputEncoding = DefaultEncoding,
                StandardErrorEncoding = DefaultEncoding,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }
    }
}

