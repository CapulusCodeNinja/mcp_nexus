using System;
using System.Threading;
using System.Threading.Tasks;

namespace mcp_nexus.Helper
{
    public interface ICdbSession : IDisposable
    {
        bool IsActive { get; }
        Task<bool> StartSession(string target, string? arguments);
        Task<bool> StopSession();
        Task<string> ExecuteCommand(string command);
        Task<string> ExecuteCommand(string command, CancellationToken externalCancellationToken);
        void CancelCurrentOperation();
    }
}
