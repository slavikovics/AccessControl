using System.Security.Cryptography;
using System.Text;

namespace SecureApp.Api.Services;

// AES-256-GCM authenticated encryption for confidential fields at rest.
// Layout per blob: [12-byte nonce][ciphertext][16-byte tag].
public class ConfidentialCrypto(IConfiguration configuration)
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key = DeriveKey(configuration["Encryption:Key"]
        ?? throw new InvalidOperationException("Encryption:Key is not configured."));

    private static byte[] DeriveKey(string configuredKey)
    {
        // Accept a passphrase of any length from configuration/environment and
        // derive a fixed 256-bit key from it, rather than requiring an exact
        // base64-encoded 32-byte value.
        return SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
    }

    public byte[] Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var result = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + cipherBytes.Length, TagSize);
        return result;
    }

    public string Decrypt(byte[] blob)
    {
        var nonce = blob[..NonceSize];
        var tag = blob[^TagSize..];
        var cipherBytes = blob[NonceSize..^TagSize];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
