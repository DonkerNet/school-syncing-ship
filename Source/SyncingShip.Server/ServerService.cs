using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using SyncingShip.Protocol;
using SyncingShip.Protocol.Entities;
using SyncingShip.Protocol.Utils;
using SyncingShip.Shared;

namespace SyncingShip.Server
{
    public class ServerService
    {
        private readonly FileManager _fileManager;
        private readonly ChecksumManager _checksumManager;
        private readonly SyncServer _server;

        public SyncServer.ServerStatus ServerStatus => _server.Status;

        public ServerService()
        {
            _fileManager = new FileManager(ConfigurationManager.AppSettings["FileDirectory"]);
            _checksumManager = new ChecksumManager(
                ConfigurationManager.AppSettings["FileDirectory"],
                null);
            _server = new SyncServer(int.Parse(ConfigurationManager.AppSettings["ServerPort"]));
            _server.ListCallback += ListCallback;
            _server.GetCallback += GetCallback;
            _server.PutCallback += PutCallback;
            _server.DeleteCallback += DeleteCallback;
        }

        public void Start()
        {
            Console.WriteLine("Starting server...");
            _server.Start();
            Console.WriteLine("Server started.");
        }

        public void Stop()
        {
            Console.WriteLine("Stopping server...");
            _server.Stop();
            Console.WriteLine("Server stopped.");
        }

        #region Callback methods

        private SyncResult ListCallback(out ICollection<SyncFileInfo> files)
        {
            Console.WriteLine("Retrieving local file list.");

            files = GetLocalFileList();
            return new SyncResult { StatusCode = SyncStatusCode.Ok };
        }

        private SyncResult GetCallback(string fileName, out SyncFile file)
        {
            Console.WriteLine("Retrieving local file '{0}'.", fileName);

            if (string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("No file name was specified.");
                file = null;
                return new SyncResult { StatusCode = SyncStatusCode.BadRequest, Message = "No file name specified." };
            }

            byte[] fileBytes = _fileManager.GetFileContent(fileName);

            if (fileBytes == null)
            {
                Console.WriteLine("File '{0}' not found.", fileName);
                file = null;
                return new SyncResult { StatusCode = SyncStatusCode.NotFound };
            }

            file = new SyncFile
            {
                FileName = fileName,
                Content = fileBytes,
                Checksum = ChecksumUtil.CreateChecksum(fileBytes)
            };

            Console.WriteLine("File '{0}' retrieved.", fileName);

            return new SyncResult { StatusCode = SyncStatusCode.Ok };
        }

        private SyncResult PutCallback(string originalChecksum, SyncFile file)
        {
            Console.WriteLine("Putting file '{0}' with original checksum '{1}'.", file.FileName, originalChecksum);

            if (string.IsNullOrEmpty(file.FileName))
            {
                Console.WriteLine("No file name was specified.");
                return new SyncResult { StatusCode = SyncStatusCode.BadRequest, Message = "No file name specified." };
            }

            bool fileExists = _fileManager.FileExists(file.FileName);
            bool originalChecksumSpecified = !string.IsNullOrEmpty(originalChecksum);

            if (!fileExists && originalChecksumSpecified)
            {
                Console.WriteLine("File '{0}' not found.", file.FileName);
                return new SyncResult { StatusCode = SyncStatusCode.NotFound };
            }

            if (fileExists && !originalChecksumSpecified)
            {
                Console.WriteLine("File '{0}' exists but no original checksum was specified.", file.FileName);
                return new SyncResult
                {
                    StatusCode = SyncStatusCode.FileConflict,
                    Message = "The file already exists but no original checksum was specified."
                };
            }

            if (fileExists)
            {
                string localChecksum = _checksumManager.CreateChecksum(file.FileName);
                if (localChecksum != originalChecksum)
                {
                    Console.WriteLine("File '{0}' original checksum does not match current checksum.", file.FileName);
                    return new SyncResult
                    {
                        StatusCode = SyncStatusCode.FileConflict,
                        Message = "The file checksum does not match the original checksum."
                    };
                }
            }

            _fileManager.SaveFileContent(file.FileName, file.Content);

            Console.WriteLine("File '{0}' saved.", file.FileName);

            return new SyncResult
            {
                StatusCode = SyncStatusCode.Ok
            };
        }

        private SyncResult DeleteCallback(string fileName, string checksum)
        {
            Console.WriteLine("Deleting file '{0}' with checksum '{1}'.", fileName, checksum);

            if (string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("No file name was specified.");
                return new SyncResult { StatusCode = SyncStatusCode.BadRequest, Message = "No file name specified." };
            }

            if (!_fileManager.FileExists(fileName))
            {
                Console.WriteLine("File '{0}' not found.", fileName);
                return new SyncResult { StatusCode = SyncStatusCode.NotFound };
            }

            if (string.IsNullOrEmpty(checksum))
            {
                Console.WriteLine("Checksum not specified for file '{0}'.", fileName);
                return new SyncResult { StatusCode = SyncStatusCode.BadRequest, Message = "No checksum specicified." };
            }

            string localChecksum = _checksumManager.CreateChecksum(fileName);
            if (localChecksum != checksum)
            {
                Console.WriteLine("File '{0}' specified checksum does not match current checksum.", fileName);
                return new SyncResult
                {
                    StatusCode = SyncStatusCode.FileConflict,
                    Message = "The file checksum does not match the specified checksum."
                };
            }

            _fileManager.DeleteFile(fileName);

            Console.WriteLine("File '{0}' deleted.", fileName);

            return new SyncResult
            {
                StatusCode = SyncStatusCode.Ok
            };
        }

        #endregion

        private List<SyncFileInfo> GetLocalFileList()
        {
            List<SyncFileInfo> localFileList = new List<SyncFileInfo>();

            IEnumerable<string> filePaths = _fileManager.GetFilePaths();

            foreach (FileInfo fileInfo in filePaths.Select(fp => new FileInfo(fp)))
            {
                SyncFileInfo file = new SyncFileInfo
                {
                    FileName = fileInfo.Name,
                    Checksum = _checksumManager.CreateChecksum(fileInfo.FullName)
                };

                localFileList.Add(file);
            }

            return localFileList;
        }
    }
}