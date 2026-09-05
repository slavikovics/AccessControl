namespace SecureApp.Api.Models;

// Confidential data: title and content are stored AES-256-GCM encrypted at rest,
// so a raw copy of the database file alone does not disclose the plaintext.
public class ConfidentialRecord
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public byte[] EncryptedTitle { get; set; } = [];
    public byte[] EncryptedContent { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
