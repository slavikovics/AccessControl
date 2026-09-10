using System.Security.Cryptography;
using System.Text;

namespace SecureApp.Api.Services;

public class ConfidentialCrypto(IConfiguration configuration)
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key = DeriveKey(configuration["Encryption:Key"]
        ?? throw new InvalidOperationException("Encryption:Key is not configured."));

    private static byte[] DeriveKey(string configuredKey)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
    }

    public byte[] Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        try
        {
            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var result = new byte[NonceSize + cipherBytes.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(cipherBytes, 0, result, NonceSize, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSize + cipherBytes.Length, TagSize);
            return result;
        }
        finally
        {
            Array.Clear(plainBytes);
        }
    }

    public string Decrypt(byte[] blob)
    {
        var nonce = blob[..NonceSize];
        var tag = blob[^TagSize..];
        var cipherBytes = blob[NonceSize..^TagSize];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        try
        {
            return Encoding.UTF8.GetString(plainBytes);
        }
        finally
        {
            Array.Clear(plainBytes);
        }
    }
}
