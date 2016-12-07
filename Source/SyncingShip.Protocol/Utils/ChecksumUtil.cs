using System;
using System.IO;
using System.Security.Cryptography;

namespace SyncingShip.Protocol.Utils
{
    public static class ChecksumUtil
    {
        private static readonly HashAlgorithm Algorithm;

        static ChecksumUtil()
        {
            Algorithm = HashAlgorithm.Create(SyncConstants.ContentHashAlgorithm);
        }

        public static string CreateChecksum(byte[] bytes)
        {
            byte[] checksumBytes = Algorithm.ComputeHash(bytes);
            return CreateChecksumString(checksumBytes);
        }

        public static string CreateChecksum(Stream stream)
        {
            byte[] checksumBytes = Algorithm.ComputeHash(stream);
            return CreateChecksumString(checksumBytes);
        }

        private static string CreateChecksumString(byte[] checksumBytes)
        {
            return BitConverter.ToString(checksumBytes)
                .Replace("-", string.Empty)
                .ToLower();
        }
    }
}