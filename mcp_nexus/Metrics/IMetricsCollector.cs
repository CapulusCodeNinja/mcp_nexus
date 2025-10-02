namespace mcp_nexus.Metrics
{
    public interface IMetricsCollector
    {
        void IncrementCounter(string name, double value = 1.0, Dictionary<string, string>? tags = null);
        void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null);
        void SetGauge(string name, double value, Dictionary<string, string>? tags = null);
        void RecordExecutionTime(string operationName, TimeSpan duration, Dictionary<string, string>? tags = null);
        void RecordCommandExecution(string commandType, TimeSpan duration, bool success);
        void RecordSessionEvent(string eventType, Dictionary<string, string>? additionalTags = null);
        MetricsSnapshot GetSnapshot();
    }
}
