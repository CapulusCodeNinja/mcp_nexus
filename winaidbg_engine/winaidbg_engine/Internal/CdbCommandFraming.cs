namespace WinAiDbg.Engine.Internal;

/// <summary>
/// Provides command framing helpers for emitting WinAiDbg sentinel markers around a CDB command.
/// </summary>
internal static class CdbCommandFraming
{
    /// <summary>
    /// Creates the command lines that emit start and end sentinel markers around a CDB command.
    /// </summary>
    /// <param name="command">The CDB command to execute.</param>
    /// <returns>The command lines to send to CDB, in order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
    public static IReadOnlyList<string> CreateSentinelWrappedLines(string command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return new[]
        {
            $".echo {CdbSentinels.StartMarker}",
            command,
            $".echo {CdbSentinels.EndMarker}",
        };
    }
}

