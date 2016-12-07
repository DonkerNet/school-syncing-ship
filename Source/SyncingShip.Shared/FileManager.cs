using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace SyncingShip.Shared
{
    public class FileManager
    {
        private readonly string _fileDirectory;

        public FileManager(string fileDirectory)
        {
            _fileDirectory = fileDirectory;
        }

        public IEnumerable<string> GetFilePaths()
        {
            return Directory.GetFiles(_fileDirectory, "*.*", SearchOption.TopDirectoryOnly);
        }

        public byte[] GetFileContent(string fileName)
        {
            string path = Path.Combine(_fileDirectory, fileName);
            byte[] bytes;
            using (FileStream fileStream = File.OpenRead(path))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();
                }
            }
            return bytes;
        }

        public void SaveFileContent(string fileName, byte[] content)
        {
            if (content == null)
                content = new byte[0];
            string path = Path.Combine(_fileDirectory, fileName);
            using (FileStream fileStream = File.OpenWrite(path))
            {
                fileStream.SetLength(0);
                fileStream.Write(content, 0, content.Length);
                fileStream.Flush();
            }
        }

        public void DeleteFile(string fileName)
        {
            string path = Path.Combine(_fileDirectory, fileName);
            if (File.Exists(path))
                File.Delete(path);
        }

        public bool FileExists(string fileName)
        {
            string path = Path.Combine(_fileDirectory, fileName);
            return File.Exists(path);
        }
    }
}