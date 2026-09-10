namespace SecureApp.Api.Models;

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
