using Nexus.Extensions.Models;

namespace nexus.extensions
{
    public interface IExtensionScripts
    {
        string ExecuteAsync(
            string extensionName,
            string sessionId,
            object? parameters,
            Action<string>? progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}
