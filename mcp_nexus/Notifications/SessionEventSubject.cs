namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Subject for session events using Observer Pattern
    /// </summary>
    public class SessionEventSubject : ISessionEventSubject
    {
        #region Private Fields

        private readonly List<ISessionEventObserver> m_observers = new();
        private readonly object m_lock = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Attaches an observer to receive session events
        /// </summary>
        /// <param name="observer">Observer to attach</param>
        public void Attach(ISessionEventObserver observer)
        {
            if (observer == null) return;

            lock (m_lock)
            {
                if (!m_observers.Contains(observer))
                {
                    m_observers.Add(observer);
                }
            }
        }

        /// <summary>
        /// Detaches an observer from receiving session events
        /// </summary>
        /// <param name="observer">Observer to detach</param>
        public void Detach(ISessionEventObserver observer)
        {
            if (observer == null) return;

            lock (m_lock)
            {
                m_observers.Remove(observer);
            }
        }

        /// <summary>
        /// Notifies all observers of a session created event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        public async Task NotifySessionCreatedAsync(string sessionId, ISessionEventData eventData)
        {
            List<ISessionEventObserver> observers;
            lock (m_lock)
            {
                observers = new List<ISessionEventObserver>(m_observers);
            }

            var tasks = observers.Select(observer => 
                SafeNotifyAsync(() => observer.OnSessionCreatedAsync(sessionId, eventData)));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Notifies all observers of a session closed event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        public async Task NotifySessionClosedAsync(string sessionId, ISessionEventData eventData)
        {
            List<ISessionEventObserver> observers;
            lock (m_lock)
            {
                observers = new List<ISessionEventObserver>(m_observers);
            }

            var tasks = observers.Select(observer => 
                SafeNotifyAsync(() => observer.OnSessionClosedAsync(sessionId, eventData)));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Notifies all observers of a session error event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        public async Task NotifySessionErrorAsync(string sessionId, ISessionEventData eventData)
        {
            List<ISessionEventObserver> observers;
            lock (m_lock)
            {
                observers = new List<ISessionEventObserver>(m_observers);
            }

            var tasks = observers.Select(observer => 
                SafeNotifyAsync(() => observer.OnSessionErrorAsync(sessionId, eventData)));

            await Task.WhenAll(tasks);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Safely notifies an observer, catching and logging any exceptions
        /// </summary>
        /// <param name="notificationAction">Notification action to execute</param>
        private static async Task SafeNotifyAsync(Func<Task> notificationAction)
        {
            try
            {
                await notificationAction();
            }
            catch (Exception ex)
            {
                // Log the exception but don't let it propagate
                // In a real implementation, you would use proper logging here
                Console.WriteLine($"Error notifying observer: {ex.Message}");
            }
        }

        #endregion
    }
}
