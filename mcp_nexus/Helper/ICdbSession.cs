using System.Threading;
using System.Threading.Tasks;

namespace mcp_nexus.Helper
{
	public interface ICdbSession
	{
		bool IsActive { get; }
		Task<bool> StartSession(string target, string? arguments = null);
		Task<bool> StopSession();
		Task<string> ExecuteCommand(string command);
		Task<string> ExecuteCommand(string command, CancellationToken externalCancellationToken);
		void CancelCurrentOperation();
	}
}
