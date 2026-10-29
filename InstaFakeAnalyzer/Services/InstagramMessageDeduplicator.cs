namespace InstaFakeAnalyzer.Services;

public sealed class InstagramMessageDeduplicator
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);
    private const int MaximumEntries = 10_000;

    private readonly object _lock = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public bool TryBegin(string messageKey)
    {
        lock (_lock)
        {
            RemoveExpiredEntries(DateTimeOffset.UtcNow);
            if (_entries.ContainsKey(messageKey))
                return false;

            _entries.Add(messageKey, new Entry());
            TrimCompletedEntries();
            return true;
        }
    }

    public void MarkCompleted(string messageKey)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(messageKey, out var entry))
                entry.CompletedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Release(string messageKey)
    {
        lock (_lock)
            _entries.Remove(messageKey);
    }

    private void RemoveExpiredEntries(DateTimeOffset now)
    {
        var expiredKeys = _entries
            .Where(item => item.Value.CompletedAt is { } completedAt &&
                           now - completedAt >= Retention)
            .Select(item => item.Key)
            .ToArray();

        foreach (var key in expiredKeys)
            _entries.Remove(key);
    }

    private void TrimCompletedEntries()
    {
        while (_entries.Count > MaximumEntries)
        {
            var completedEntries = _entries
                .Where(item => item.Value.CompletedAt.HasValue)
                .OrderBy(item => item.Value.CompletedAt)
                .Take(1)
                .ToArray();

            if (completedEntries.Length == 0)
                return;

            _entries.Remove(completedEntries[0].Key);
        }
    }

    private sealed class Entry
    {
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
