namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for garbage collection health information with proper encapsulation
    /// </summary>
    public interface IGcHealth
    {
        /// <summary>Gets whether garbage collection is healthy</summary>
        bool IsHealthy { get; }

        /// <summary>Gets number of Gen0 collections</summary>
        int Gen0Collections { get; }

        /// <summary>Gets number of Gen1 collections</summary>
        int Gen1Collections { get; }

        /// <summary>Gets number of Gen2 collections</summary>
        int Gen2Collections { get; }

        /// <summary>Gets total number of collections</summary>
        int TotalCollections { get; }

        /// <summary>Gets a message describing the GC health</summary>
        string Message { get; }

        /// <summary>
        /// Sets the GC health information
        /// </summary>
        /// <param name="isHealthy">Whether garbage collection is healthy</param>
        /// <param name="gen0Collections">Number of Gen0 collections</param>
        /// <param name="gen1Collections">Number of Gen1 collections</param>
        /// <param name="gen2Collections">Number of Gen2 collections</param>
        /// <param name="totalCollections">Total number of collections</param>
        /// <param name="message">Health message</param>
        void SetGcInfo(bool isHealthy, int gen0Collections, int gen1Collections, 
            int gen2Collections, int totalCollections, string message);
    }
}
