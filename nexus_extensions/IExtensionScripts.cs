namespace Nexus.Engine.Extensions
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
