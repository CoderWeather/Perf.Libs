using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Utilities;

public static class EncryptUtils {
    public static (string Base64, byte[] Salt) HashPassword(string password) {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);

        var encryptedBytes = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            10000,
            32
        );

        var base64 = Convert.ToBase64String(encryptedBytes);

        return (base64, salt);
    }

    public static string HashPassword(string password, byte[] salt) {
        var encryptedBytes = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            10000,
            32
        );

        var base64 = Convert.ToBase64String(encryptedBytes);

        return base64;
    }

    public static bool Verify(string password, string storedHash) {
        var splits = storedHash.Split('.');
        var salt = Convert.FromBase64String(splits[0]);
        var hash = splits[1];

        var hashedPassword = HashPassword(password, salt);

        return hashedPassword == hash;
    }
}
