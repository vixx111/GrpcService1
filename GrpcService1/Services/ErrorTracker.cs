using System;
using System.Collections.Concurrent;

namespace GrpcService1.Services
{
    public class ErrorTracker
    {
        private readonly ConcurrentQueue<DateTime> _errorTimestamps = new();
        private readonly object _lock = new();

        public void RecordError()
        {
            _errorTimestamps.Enqueue(DateTime.UtcNow);
        }

        public int GetRecentErrorCount()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-1);

            while (_errorTimestamps.TryPeek(out var time) && time < cutoff)
            {
                _errorTimestamps.TryDequeue(out _);
            }

            return _errorTimestamps.Count;
        }
    }
}