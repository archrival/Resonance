using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Resonance.Common
{
    public static class HashExtensions
    {
        private static readonly Lazy<MD5> Md5 = new Lazy<MD5>(MD5.Create);
        private static readonly Random Random = new Random();
        private static readonly Lazy<SHA1> Sha1 = new Lazy<SHA1>(SHA1.Create);
        private static readonly Lazy<SHA256> Sha256 = new Lazy<SHA256>(SHA256.Create);
        private static readonly Lazy<SHA384> Sha384 = new Lazy<SHA384>(SHA384.Create);
        private static readonly Lazy<SHA512> Sha512 = new Lazy<SHA512>(SHA512.Create);

        public static string ComputeHash(string plainText, HashType hashType, byte[] saltBytes)
        {
            // If salt is not specified, generate it.
            saltBytes = saltBytes ?? GenerateSalt();

            // Convert plain text into a byte array.
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Allocate array, which will hold plain text and salt.
            var plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];

            // Copy plain text bytes into resulting array.
            for (var i = 0; i < plainTextBytes.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainTextBytes[i];
            }

            // Append salt bytes to the resulting array.
            for (var i = 0; i < saltBytes.Length; i++)
            {
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
            }

            HashAlgorithm hash;

            // Initialize appropriate hashing algorithm class.
            switch (hashType)
            {
                case HashType.SHA1:
                    hash = Sha1.Value;
                    break;

                case HashType.SHA256:
                    hash = Sha256.Value;
                    break;

                case HashType.SHA384:
                    hash = Sha384.Value;
                    break;

                case HashType.SHA512:
                    hash = Sha512.Value;
                    break;

                default:
                    hash = Md5.Value;
                    break;
            }

            // Compute hash value of our plain text with appended salt.
            var hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

            // Create array which will hold hash and original salt bytes.
            var hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];

            // Copy hash bytes into resulting array.
            for (var i = 0; i < hashBytes.Length; i++)
            {
                hashWithSaltBytes[i] = hashBytes[i];
            }

            // Append salt bytes to the result.
            for (var i = 0; i < saltBytes.Length; i++)
            {
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];
            }

            // Convert result into a base64-encoded string.
            var hashValue = Convert.ToBase64String(hashWithSaltBytes);

            // Return the result.
            return hashValue;
        }

        public static byte[] GenerateSalt()
        {
            // Define min and max salt sizes.
            const int minSaltSize = 8;
            const int maxSaltSize = 24;

            // Generate a random number for the size of the salt.
            var saltSize = Random.Next(minSaltSize, maxSaltSize);

            // Allocate a byte array, which will hold the salt.
            var saltBytes = new byte[saltSize];

            // Initialize a random number generator.
            var rng = RandomNumberGenerator.Create();

            // Fill the salt with cryptographically strong byte values.
            rng.GetBytes(saltBytes);

            return saltBytes;
        }

        public static string GetFileHash(string path, HashType hashType)
        {
            return GetHash(File.ReadAllBytes(path), hashType);
        }

        public static string GetHash(this byte[] bytes, HashType hashType)
        {
            var sb = new StringBuilder();

            HashAlgorithm hashAlgorithm;

            switch (hashType)
            {
                case HashType.SHA1:
                    hashAlgorithm = Sha1.Value;
                    break;

                case HashType.SHA256:
                    hashAlgorithm = Sha256.Value;
                    break;

                case HashType.SHA384:
                    hashAlgorithm = Sha384.Value;
                    break;

                case HashType.SHA512:
                    hashAlgorithm = Sha512.Value;
                    break;

                default:
                    hashAlgorithm = Md5.Value;
                    break;
            }

            var result = hashAlgorithm.ComputeHash(bytes);

            foreach (var b in result)
            {
                sb.Append(b.ToString("x2"));
            }

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

        public static bool VerifyHash(string plainText, HashType hashType, string hashValue)
        {
            // Convert base64-encoded hash value into a byte array.
            var hashWithSaltBytes = Convert.FromBase64String(hashValue);

            // We must know size of hash (without salt).
            int hashSizeInBits;

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
            var hashSizeInBytes = hashSizeInBits / 8;

            // Make sure that the specified hash value is long enough.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
                return false;

            // Allocate array to hold original salt bytes retrieved from hash.
            var saltBytes = new byte[hashWithSaltBytes.Length - hashSizeInBytes];

            // Copy salt from the end of the hash to the new array.
            for (var i = 0; i < saltBytes.Length; i++)
            {
                saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];
            }

            // Compute a new hash string.
            var expectedHashString = ComputeHash(plainText, hashType, saltBytes);

            // If the computed hash matches the specified hash,
            // the plain text value must be correct.
            return hashValue == expectedHashString;
        }
    }
}