namespace CrossCutting.Crypto;

public interface ICrypto
{
    string Encrypt(string text);
    string Decrypt(string encryptedText);
}
