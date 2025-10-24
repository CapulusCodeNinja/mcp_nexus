using nexus.engine;

namespace nexus.protocol.Loader;

/// <summary>
/// Loads the debug engine.
/// </summary>
internal static class EngineLoader
{
    private static IDebugEngine m_DebugEngine = new DebugEngine();

    public static IDebugEngine GetDebugEngine()
    {
        return m_DebugEngine;
    }
}
