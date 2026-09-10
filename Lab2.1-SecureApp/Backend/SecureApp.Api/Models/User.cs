namespace SecureApp.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ConfidentialRecord> ConfidentialRecords { get; set; } = [];
    public List<PublicRecord> PublicRecords { get; set; } = [];
}
