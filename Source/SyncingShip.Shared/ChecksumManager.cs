using System.IO;
using SyncingShip.Protocol.Utils;

namespace SyncingShip.Shared
{
    public class ChecksumManager
    {
        private readonly string _fileDirectory;
        private readonly string _checksumDirectory;

        public ChecksumManager(string fileDirectory, string checksumDirectory)
        {
            _fileDirectory = fileDirectory;
            _checksumDirectory = checksumDirectory;
        }

        public string CreateChecksum(string fileName)
        {
            string path = Path.Combine(_fileDirectory, fileName);
            using (FileStream fileStream = File.OpenRead(path))
                return ChecksumUtil.CreateChecksum(fileStream);
        }

        public string GetChecksum(string fileName)
        {
            string path = Path.Combine(_checksumDirectory, fileName + ".checksum");
            if (!File.Exists(path))
                return null;
            using (StreamReader reader = new StreamReader(path))
                return reader.ReadLine();
        }

        public void SaveChecksum(string fileName, string checksum)
        {
            string path = Path.Combine(_checksumDirectory, fileName + ".checksum");
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.Write(checksum);
                writer.Flush();
            }
        }

        public void DeleteChecksum(string fileName)
        {
            string path = Path.Combine(_checksumDirectory, fileName + ".checksum");
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}