using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Debugger;
using mcp_nexus_tests.Mocks;

namespace mcp_nexus_tests.Helpers
{
    /// <summary>
    /// Helper class that provides realistic CDB mocks for all tests
    /// This replaces simple string-returning mocks with realistic behavior simulation
    /// </summary>
    public static class RealisticCdbTestHelper
    {
        /// <summary>
        /// Creates a realistic CDB session mock that simulates actual CDB behavior
        /// Use this instead of Mock&lt;ICdbSession&gt; in all tests
        /// </summary>
        public static ICdbSession CreateRealisticCdbSession(ILogger logger)
        {
            var mock = new RealisticCdbSessionMock(logger);
            
            // Session is started automatically in constructor
            
            return mock;
        }

        /// <summary>
        /// Creates a realistic CDB session mock with custom command behaviors
        /// </summary>
        public static ICdbSession CreateRealisticCdbSession(ILogger logger, Action<RealisticCdbSessionMock> configure)
        {
            var mock = new RealisticCdbSessionMock(logger);
            
            // Apply custom configuration
            configure(mock);
            
            // Session is started automatically in constructor
            
            return mock;
        }

        /// <summary>
        /// Creates a realistic CDB session mock that simulates failure scenarios
        /// </summary>
        public static ICdbSession CreateFailingCdbSession(ILogger logger, bool shouldFailStartSession = false, bool shouldFailStopSession = false, bool shouldThrowOnCancel = false)
        {
            var mock = new RealisticCdbSessionMock(logger);
            mock.ConfigureBehavior(shouldFailStartSession, shouldFailStopSession, shouldThrowOnCancel);
            
            // Session is started automatically in constructor
            
            return mock;
        }

        /// <summary>
        /// Creates a realistic CDB session mock that simulates recovery scenarios
        /// </summary>
        public static ICdbSession CreateRecoveryCdbSession(ILogger logger, params bool[] isActiveSequence)
        {
            var mock = new RealisticCdbSessionMock(logger);
            mock.SetIsActiveSequence(isActiveSequence);
            
            // Session is started automatically in constructor
            
            return mock;
        }

        /// <summary>
        /// Creates a realistic CDB session mock that simulates the exact bugs we found
        /// </summary>
        public static ICdbSession CreateBugSimulatingCdbSession(ILogger logger)
        {
            var mock = new RealisticCdbSessionMock(logger);
            
            // Add behaviors that simulate the bugs we found
            mock.AddCommandBehavior("!analyze -v", new CdbCommandBehavior
            {
                StdoutLines = new[]
                {
                    "DBGENG:  Find 'C:\\BUILD\\work\\e0dd96435fde7cb0\\framework\\serviceapp\\src\\serviceapp\\singleservice.cpp'",
                    "DBGENG:  Not using checksum for source file search",
                    "DBGENG:  Scan srcsrv SRV* for:",
                    "DBGENG:    'AvastSvc!C:\\BUILD\\work\\e0dd96435fde7cb0\\framework\\serviceapp\\src\\serviceapp\\singleservice.cpp'",
                    "SRCSRV:  powershell -NonInteractive -command \"mkdir (Split-Path 'C:\\Program Files (x86)\\Windows Kits\\10\\Debuggers\\x64\\src\\DEV\\AvastClient\\framework\\serviceapp\\src\\serviceapp\\singleservice.cpp\\41f36a4e4d6daa33fef1563271c1b19fcf2b3a38\\singleservice.cpp')\"",
                    "DBGENG:    file not found with srcsrv",
                    "DBGENG:  Scan all paths for:",
                    "DBGENG:    'C:\\BUILD\\work\\e0dd96435fde7cb0\\framework\\serviceapp\\src\\serviceapp\\singleservice.cpp'",
                    "DBGENG:  Scan all paths for:",
                    "DBGENG:    'BUILD\\work\\e0dd96435fde7cb0\\framework\\serviceapp\\src\\serviceapp\\singleservice.cpp'",
                    "DBGENG:      check 'srv\\*/mnt/c/inetpub/wwwroot/workingdir/work_20251005_131547_036/source\\BUILD\\work\\e0dd96435fde7cb0\\framework\\serviceapp\\src\\serviceapp\\singleservice.cpp'",
                    "DBGENG:      check ' .echo MCP_NEXUS_SENTINEL_COMMAND_END\\BUILD\\work\\e0dd96435fde7cb0\\framework\\serviceapp\\src\\serviceapp\\singleservice.cpp'"
                },
                StderrLines = new[]
                {
                    "Error: Source file not found",
                    "Warning: Symbol server timeout"
                },
                ExecutionDelay = TimeSpan.FromMilliseconds(100),
                CompletionDelay = TimeSpan.FromMilliseconds(50)
            });

            mock.AddCommandBehavior(".srcfix+", new CdbCommandBehavior
            {
                StdoutLines = new[] { "Source server settings updated" },
                StderrLines = new string[0],
                ExecutionDelay = TimeSpan.FromMilliseconds(10),
                CompletionDelay = TimeSpan.FromMilliseconds(5)
            });

            mock.AddCommandBehavior("k", new CdbCommandBehavior
            {
                StdoutLines = new[]
                {
                    " # Child-SP          RetAddr           Call Site",
                    "00 00000000`00000000 00000000`00000000 0x00000000`00000000",
                    "01 00000000`00000000 00000000`00000000 0x00000000`00000000"
                },
                StderrLines = new string[0],
                ExecutionDelay = TimeSpan.FromSeconds(30), // Long execution
                CompletionDelay = TimeSpan.FromMilliseconds(100)
            });

            mock.AddCommandBehavior("!invalid", new CdbCommandBehavior
            {
                StdoutLines = new string[0],
                StderrLines = new[] { "Unknown command: !invalid" },
                ExecutionDelay = TimeSpan.FromMilliseconds(50),
                CompletionDelay = TimeSpan.FromMilliseconds(10),
                ShouldFail = true
            });

            // Session is started automatically in constructor
            
            return mock;
        }

        /// <summary>
        /// Creates a realistic CDB session mock that simulates timeout scenarios
        /// </summary>
        public static ICdbSession CreateTimeoutSimulatingCdbSession(ILogger logger)
        {
            var mock = new RealisticCdbSessionMock(logger);
            
            // Add behaviors that simulate timeout scenarios
            mock.AddCommandBehavior("long-running-command", new CdbCommandBehavior
            {
                StdoutLines = new[] { "Starting long operation..." },
                StderrLines = new string[0],
                ExecutionDelay = TimeSpan.FromMinutes(5), // Very long execution
                CompletionDelay = TimeSpan.FromMilliseconds(100)
            });

            mock.AddCommandBehavior("hanging-command", new CdbCommandBehavior
            {
                StdoutLines = new[] { "This command will hang" },
                StderrLines = new string[0],
                ExecutionDelay = TimeSpan.FromHours(1), // Never completes
                CompletionDelay = TimeSpan.FromMilliseconds(100)
            });

            // Session is started automatically in constructor
            
            return mock;
        }

        /// <summary>
        /// Creates a realistic CDB session mock that simulates error scenarios
        /// </summary>
        public static ICdbSession CreateErrorSimulatingCdbSession(ILogger logger)
        {
            var mock = new RealisticCdbSessionMock(logger);
            
            // Add behaviors that simulate error scenarios
            mock.AddCommandBehavior("failing-command", new CdbCommandBehavior
            {
                StdoutLines = new string[0],
                StderrLines = new[] { "Command failed with error" },
                ExecutionDelay = TimeSpan.FromMilliseconds(50),
                CompletionDelay = TimeSpan.FromMilliseconds(10),
                ShouldFail = true
            });

            mock.AddCommandBehavior("stderr-only-command", new CdbCommandBehavior
            {
                StdoutLines = new string[0],
                StderrLines = new[] { "Error: This command only produces stderr" },
                ExecutionDelay = TimeSpan.FromMilliseconds(100),
                CompletionDelay = TimeSpan.FromMilliseconds(50)
            });

            // Session is started automatically in constructor
            
            return mock;
        }
    }
}
