using System.Collections.Concurrent;

namespace SecureApp.Api.Services;

// Simple in-memory brute-force mitigation: after too many failed logins for a
// username, further attempts are rejected for a cooldown window regardless of
// whether the password supplied is correct.
public class LoginAttemptTracker
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan Lockout = TimeSpan.FromSeconds(30);

    private record Entry(int FailedCount, DateTime LastFailureUtc);

    private readonly ConcurrentDictionary<string, Entry> _attempts = new(StringComparer.OrdinalIgnoreCase);

    public bool IsLockedOut(string username)
    {
        if (!_attempts.TryGetValue(username, out var entry)) return false;
        if (entry.FailedCount < MaxFailedAttempts) return false;
        return DateTime.UtcNow - entry.LastFailureUtc < Lockout;
    }

    public void RecordFailure(string username)
    {
        _attempts.AddOrUpdate(
            username,
            _ => new Entry(1, DateTime.UtcNow),
            (_, prev) => new Entry(prev.FailedCount + 1, DateTime.UtcNow));
    }

    public void RecordSuccess(string username) => _attempts.TryRemove(username, out _);
}
