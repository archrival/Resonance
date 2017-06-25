using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Resonance.Common
{
    public static class HashExtensions
    {
        public static string ComputeHash(string plainText, HashType hashType, byte[] saltBytes)
        {
            // If salt is not specified, generate it.
            saltBytes = saltBytes ?? GenerateSalt();

            // Convert plain text into a byte array.
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Allocate array, which will hold plain text and salt.
            byte[] plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];

            // Copy plain text bytes into resulting array.
            for (int i = 0; i < plainTextBytes.Length; i++)
                plainTextWithSaltBytes[i] = plainTextBytes[i];

            // Append salt bytes to the resulting array.
            for (int i = 0; i < saltBytes.Length; i++)
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];

            HashAlgorithm hash;

            // Initialize appropriate hashing algorithm class.
            switch (hashType)
            {
                case HashType.SHA1:
                    hash = SHA1.Create();
                    break;

                case HashType.SHA256:
                    hash = SHA256.Create();
                    break;

                case HashType.SHA384:
                    hash = SHA384.Create();
                    break;

                case HashType.SHA512:
                    hash = SHA512.Create();
                    break;

                default:
                    hash = MD5.Create();
                    break;
            }

            // Compute hash value of our plain text with appended salt.
            byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

            // Create array which will hold hash and original salt bytes.
            byte[] hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];

            // Copy hash bytes into resulting array.
            for (int i = 0; i < hashBytes.Length; i++)
                hashWithSaltBytes[i] = hashBytes[i];

            // Append salt bytes to the result.
            for (int i = 0; i < saltBytes.Length; i++)
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

            // Convert result into a base64-encoded string.
            string hashValue = Convert.ToBase64String(hashWithSaltBytes);

            // Return the result.
            return hashValue;
        }

        public static byte[] GenerateSalt()
        {
            // Define min and max salt sizes.
            const int minSaltSize = 8;
            const int maxSaltSize = 24;

            // Generate a random number for the size of the salt.
            Random random = new Random();
            int saltSize = random.Next(minSaltSize, maxSaltSize);

            // Allocate a byte array, which will hold the salt.
            var saltBytes = new byte[saltSize];

            // Initialize a random number generator.
            var rng = RandomNumberGenerator.Create();

            // Fill the salt with cryptographically strong byte values.
            rng.GetBytes(saltBytes);

            return saltBytes;
        }

        public static string GetHash(this byte[] bytes, HashType hashType)
        {
            StringBuilder sb = new StringBuilder();

            HashAlgorithm hashAlgorithm;

            switch (hashType)
            {
                case HashType.SHA1:
                    hashAlgorithm = SHA1.Create();
                    break;

                case HashType.SHA256:
                    hashAlgorithm = SHA256.Create();
                    break;

                case HashType.SHA384:
                    hashAlgorithm = SHA384.Create();
                    break;

                case HashType.SHA512:
                    hashAlgorithm = SHA512.Create();
                    break;

                default:
                    hashAlgorithm = MD5.Create();
                    break;
            }

            var result = hashAlgorithm.ComputeHash(bytes);

            foreach (var b in result)
            {
                sb.Append(b.ToString("x2"));
            }

            hashAlgorithm.Dispose();

            return sb.ToString();
        }

        public static string GetHash(this FileInfo fileInfo, HashType hashType)
        {
            return GetHash(File.ReadAllBytes(fileInfo.FullName), hashType);
        }

        public static string GetHash(this string value, HashType hashType, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            return GetHash(encoding.GetBytes(value), hashType);
        }

        public static string GetMd5Hash(this string value)
        {
            using (var md5 = MD5.Create())
            {
                md5.Initialize();
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(value));

                var sBuilder = new StringBuilder();

                foreach (byte t in hash)
                {
                    sBuilder.Append(t.ToString("x2"));
                }

                return sBuilder.ToString();
            }
        }

        public static bool VerifyHash(string plainText, HashType hashType, string hashValue)
        {
            // Convert base64-encoded hash value into a byte array.
            byte[] hashWithSaltBytes = Convert.FromBase64String(hashValue);

            // We must know size of hash (without salt).
            int hashSizeInBits, hashSizeInBytes;

            // Size of hash is based on the specified algorithm.
            switch (hashType)
            {
                case HashType.SHA1:
                    hashSizeInBits = 160;
                    break;

                case HashType.SHA256:
                    hashSizeInBits = 256;
                    break;

                case HashType.SHA384:
                    hashSizeInBits = 384;
                    break;

                case HashType.SHA512:
                    hashSizeInBits = 512;
                    break;

                default:
                    hashSizeInBits = 128;
                    break;
            }

            // Convert size of hash from bits to bytes.
            hashSizeInBytes = hashSizeInBits / 8;

            // Make sure that the specified hash value is long enough.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
                return false;

            // Allocate array to hold original salt bytes retrieved from hash.
            byte[] saltBytes = new byte[hashWithSaltBytes.Length - hashSizeInBytes];

            // Copy salt from the end of the hash to the new array.
            for (int i = 0; i < saltBytes.Length; i++)
                saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];

            // Compute a new hash string.
            string expectedHashString = ComputeHash(plainText, hashType, saltBytes);

            // If the computed hash matches the specified hash,
            // the plain text value must be correct.
            return hashValue == expectedHashString;
        }
    }
}