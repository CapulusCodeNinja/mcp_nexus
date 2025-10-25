using Nexus.Engine.Models;

using ExtensionCommandInfo = Nexus.Engine.Extensions.Models.CommandInfo;

namespace Nexus.Engine.Internal;

/// <summary>
/// Maps between extension command info and engine command info.
/// </summary>
internal static class ExtensionCommandMapper
{
    /// <summary>
    /// Converts an extension command info object to an engine command info object.
    /// </summary>
    /// <param name="extensionCommandInfo">The extension command info to convert.</param>
    /// <returns>The converted engine command info.</returns>
    /// <remarks>
    /// Both extension and engine command info now use the shared CommandState from nexus_engine_share,
    /// so no state conversion is needed.
    /// </remarks>
    public static CommandInfo ToEngineCommandInfo(ExtensionCommandInfo extensionCommandInfo)
    {
        return new CommandInfo
        {
            CommandId = extensionCommandInfo.CommandId,
            Command = extensionCommandInfo.Command,
            State = extensionCommandInfo.State,
            QueuedTime = extensionCommandInfo.QueuedTime,
            StartTime = extensionCommandInfo.StartTime,
            EndTime = extensionCommandInfo.EndTime,
            Output = extensionCommandInfo.Output,
            IsSuccess = extensionCommandInfo.IsSuccess,
            ErrorMessage = extensionCommandInfo.ErrorMessage
        };
    }
}
