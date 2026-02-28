using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CrossCutting.Crypto;

public class Crypto(IConfiguration configuration, ILogger<Crypto> logger) : ICrypto
{
    private const string Prefix = "enc:v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<Crypto> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string Encrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var key = GetKeyBytes();
        var plainBytes = Encoding.UTF8.GetBytes(text);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tagBytes = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tagBytes);

        var payload = string.Join(
            ".",
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tagBytes),
            Convert.ToBase64String(cipherBytes));

        return Prefix + payload;
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        if (!encryptedText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            _logger.LogDebug("Crypto value is not prefixed with '{prefix}'. Returning value as-is.", Prefix);
            return encryptedText;
        }

        var payload = encryptedText[Prefix.Length..];
        var parts = payload.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new InvalidOperationException("Invalid encrypted payload format.");
        }

        try
        {
            var nonce = Convert.FromBase64String(parts[0]);
            var tag = Convert.FromBase64String(parts[1]);
            var cipher = Convert.FromBase64String(parts[2]);

            if (nonce.Length != NonceSize || tag.Length != TagSize)
            {
                throw new InvalidOperationException("Invalid encrypted payload sizes.");
            }

            var key = GetKeyBytes();
            var plainBytes = new byte[cipher.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plainBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new InvalidOperationException("Unable to decrypt value. Verify Security:Crypto:Key and encrypted payload.", ex);
        }
    }

    private byte[] GetKeyBytes()
    {
        var keyBase64 = _configuration["Security:Crypto:Key"];
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException("Missing configuration value Security:Crypto:Key.");
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Security:Crypto:Key must be a valid Base64 string.", ex);
        }

        if (keyBytes.Length is not 16 and not 24 and not 32)
        {
            throw new InvalidOperationException("Security:Crypto:Key must decode to 16, 24, or 32 bytes.");
        }

        return keyBytes;
    }
}
