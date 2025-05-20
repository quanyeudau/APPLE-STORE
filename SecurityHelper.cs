using System;
using System.Security.Cryptography;

public struct SecurityHelper
{
    // Method to hash a password using PBKDF2
    public string HashPassword(string password)
    {
        // Generate a random salt
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] salt = new byte[32];
            rng.GetBytes(salt);

            // Hash the password with PBKDF2
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                // Combine salt and hash into a single byte array
                byte[] hashBytes = new byte[salt.Length + hash.Length];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

                // Return the combined salt and hash as a Base64 string
                return Convert.ToBase64String(hashBytes);
            }
        }
    }

    // Method to verify a password against a stored hash
    public bool VerifyPassword(string enteredPassword, string storedPassword)
    {
        // Decode the stored password from Base64
        byte[] hashBytes = Convert.FromBase64String(storedPassword);

        // Extract the salt and hash from the stored password
        byte[] salt = new byte[32];
        byte[] storedHash = new byte[hashBytes.Length - salt.Length];
        Buffer.BlockCopy(hashBytes, 0, salt, 0, salt.Length);
        Buffer.BlockCopy(hashBytes, salt.Length, storedHash, 0, storedHash.Length);

        // Hash the entered password with the extracted salt
        using (var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, salt, 10000))
        {
            byte[] hash = pbkdf2.GetBytes(32);

            // Compare the entered password's hash with the stored hash
            for (int i = 0; i < storedHash.Length; i++)
            {
                if (storedHash[i] != hash[i])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
