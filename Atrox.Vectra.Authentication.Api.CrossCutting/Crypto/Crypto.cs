namespace CrossCutting.Crypto;

public class Crypto : ICrypto
{
    public string Encrypt(string text) => text;
    public string Decrypt(string encryptedText) => encryptedText;
}
