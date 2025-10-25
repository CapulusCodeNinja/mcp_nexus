using Nexus.Engine.Extensions.Core;

namespace Nexus.Engine.Extensions
{
    public class ExtensionScripts : IExtensionScripts
    {
        private readonly ExtensionExecutor m_Executer;
        private readonly ExtensionManager m_Manager;

        public static IExtensionScripts Instance { get; } = new ExtensionScripts();

        private ExtensionScripts()
        {
            m_Manager = new ExtensionManager();
            m_Executer = new ExtensionExecutor(m_Manager);
        }

        public string ExecuteAsync(
            string extensionName,
            string sessionId,
            object? parameters,
            string commandId,
            Action<string>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            return m_Executer.ExecuteAsync(
                extensionName,
                sessionId,
                parameters,
                commandId,
                progressCallback,
                cancellationToken);
        }
    }
}
