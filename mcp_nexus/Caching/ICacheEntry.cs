namespace mcp_nexus.Caching
{
    /// <summary>
    /// Interface for cache entry with proper encapsulation.
    /// Provides methods for managing cached values with metadata tracking.
    /// </summary>
    /// <typeparam name="TValue">The type of the cached value.</typeparam>
    public interface ICacheEntry<TValue>
    {
        /// <summary>
        /// Gets the cached value.
        /// </summary>
        TValue Value { get; }

        /// <summary>
        /// Gets the creation timestamp.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the last accessed timestamp.
        /// </summary>
        DateTime LastAccessed { get; }

        /// <summary>
        /// Gets the expiration timestamp.
        /// </summary>
        DateTime ExpiresAt { get; }

        /// <summary>
        /// Gets the access count.
        /// </summary>
        long AccessCount { get; }

        /// <summary>
        /// Gets the size in bytes.
        /// </summary>
        long SizeBytes { get; }

        /// <summary>
        /// Updates the last accessed time and increments access count.
        /// </summary>
        void UpdateAccess();

        /// <summary>
        /// Updates the value and size.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="sizeBytes">The new size in bytes.</param>
        void UpdateValue(TValue value, long sizeBytes);

        /// <summary>
        /// Checks if the entry is expired.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the entry is expired; otherwise, <c>false</c>.
        /// </returns>
        bool IsExpired();
    }
}
