using Nexus.Engine.Models;

using ExtensionCommandInfo = Nexus.Engine.Extensions.Models.CommandInfo;
using ExtensionCommandState = Nexus.Engine.Extensions.Models.CommandState;

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
    public static CommandInfo ToEngineCommandInfo(ExtensionCommandInfo extensionCommandInfo)
    {
        return new CommandInfo
        {
            CommandId = extensionCommandInfo.CommandId,
            Command = extensionCommandInfo.Command,
            State = MapExtensionCommandStateToEngineCommandState(extensionCommandInfo.State),
            QueuedTime = extensionCommandInfo.QueuedTime,
            StartTime = extensionCommandInfo.StartTime,
            EndTime = extensionCommandInfo.EndTime,
            Output = extensionCommandInfo.Output,
            IsSuccess = extensionCommandInfo.IsSuccess,
            ErrorMessage = extensionCommandInfo.ErrorMessage
        };
    }

    /// <summary>
    /// Maps an extension command state to an engine command state.
    /// </summary>
    /// <param name="extensionState">The extension command state.</param>
    /// <returns>The corresponding engine command state.</returns>
    private static CommandState MapExtensionCommandStateToEngineCommandState(ExtensionCommandState extensionState)
    {
        return extensionState switch
        {
            ExtensionCommandState.Queued => CommandState.Queued,
            ExtensionCommandState.Executing => CommandState.Executing,
            ExtensionCommandState.Completed => CommandState.Completed,
            ExtensionCommandState.Failed => CommandState.Failed,
            ExtensionCommandState.Cancelled => CommandState.Cancelled,
            ExtensionCommandState.Timeout => CommandState.Timeout,
            _ => CommandState.Failed, // Default or unknown state
        };
    }
}
