using System.Collections.Concurrent;

namespace SecureApp.Api.Services;

// Lab 3.1: an in-process, non-persisted data store. Nothing here ever touches
// disk (no EF Core, no file, no serialization to a cache) -- every record
// lives only as long as this store's dictionaries are reachable, and is gone
// the moment the process restarts. Confidential entries are encrypted with
// the same AES-256-GCM key as the persisted confidential records, immediately
// on write, so their ciphertext form is what actually occupies the heap.
public class InMemoryRecordStore(ConfidentialCrypto crypto)
{
    public record ConfidentialEntry(int Id, int OwnerId, byte[] EncryptedTitle, byte[] EncryptedContent, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
    public record PublicEntry(int Id, int OwnerId, string Title, string Content, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

    private readonly ConcurrentDictionary<int, ConfidentialEntry> _confidential = new();
    private readonly ConcurrentDictionary<int, PublicEntry> _public = new();
    private int _nextConfidentialId;
    private int _nextPublicId;

    public IEnumerable<ConfidentialEntry> ListConfidential(int ownerId) =>
        _confidential.Values.Where(e => e.OwnerId == ownerId).OrderByDescending(e => e.UpdatedAtUtc);

    public ConfidentialEntry? GetConfidential(int ownerId, int id) =>
        _confidential.TryGetValue(id, out var e) && e.OwnerId == ownerId ? e : null;

    public ConfidentialEntry CreateConfidential(int ownerId, string title, string content)
    {
        var id = Interlocked.Increment(ref _nextConfidentialId);
        var now = DateTime.UtcNow;
        var entry = new ConfidentialEntry(id, ownerId, Encrypt(title), Encrypt(content), now, now);
        _confidential[id] = entry;
        return entry;
    }

    public ConfidentialEntry? UpdateConfidential(int ownerId, int id, string title, string content)
    {
        if (!_confidential.TryGetValue(id, out var existing) || existing.OwnerId != ownerId) return null;
        var updated = existing with { EncryptedTitle = Encrypt(title), EncryptedContent = Encrypt(content), UpdatedAtUtc = DateTime.UtcNow };
        _confidential[id] = updated;
        return updated;
    }

    public bool DeleteConfidential(int ownerId, int id)
    {
        if (!_confidential.TryGetValue(id, out var existing) || existing.OwnerId != ownerId) return false;
        return _confidential.TryRemove(id, out _);
    }

    public string DecryptTitle(ConfidentialEntry e) => crypto.Decrypt(e.EncryptedTitle);
    public string DecryptContent(ConfidentialEntry e) => crypto.Decrypt(e.EncryptedContent);

    public IEnumerable<PublicEntry> ListPublic(int ownerId) =>
        _public.Values.Where(e => e.OwnerId == ownerId).OrderByDescending(e => e.UpdatedAtUtc);

    public PublicEntry? GetPublic(int ownerId, int id) =>
        _public.TryGetValue(id, out var e) && e.OwnerId == ownerId ? e : null;

    public PublicEntry CreatePublic(int ownerId, string title, string content)
    {
        var id = Interlocked.Increment(ref _nextPublicId);
        var now = DateTime.UtcNow;
        var entry = new PublicEntry(id, ownerId, title, content, now, now);
        _public[id] = entry;
        return entry;
    }

    public PublicEntry? UpdatePublic(int ownerId, int id, string title, string content)
    {
        if (!_public.TryGetValue(id, out var existing) || existing.OwnerId != ownerId) return null;
        var updated = existing with { Title = title, Content = content, UpdatedAtUtc = DateTime.UtcNow };
        _public[id] = updated;
        return updated;
    }

    public bool DeletePublic(int ownerId, int id)
    {
        if (!_public.TryGetValue(id, out var existing) || existing.OwnerId != ownerId) return false;
        return _public.TryRemove(id, out _);
    }

    private byte[] Encrypt(string plaintext) => crypto.Encrypt(plaintext);
}
