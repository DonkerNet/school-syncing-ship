using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using SyncingShip.Protocol;
using SyncingShip.Protocol.Entities;
using SyncingShip.Protocol.Utils;
using SyncingShip.Shared;

namespace SyncingShip.Server
{
    public class ServerService : IDisposable
    {
        private readonly FileManager _fileManager;
        private readonly ChecksumManager _checksumManager;
        private readonly SyncServer _server;
        private readonly ILog _log;

        public SyncServer.ServerStatus ServerStatus => _server.Status;

        public ServerService()
        {
            _fileManager = new FileManager(AppConfig.FileDirectory);
            _checksumManager = new ChecksumManager(AppConfig.FileDirectory, null);
            _server = new SyncServer(AppConfig.ServerPort);
            _server.ListCallback += ListCallback;
            _server.GetCallback += GetCallback;
            _server.PutCallback += PutCallback;
            _server.DeleteCallback += DeleteCallback;
            _log = LogManager.GetLogger(GetType());
            _server.Start();
        }

        public void Dispose()
        {
            _server.Stop();
        }

        #region Callback methods

        private SyncResult ListCallback(out ICollection<SyncFileInfo> files)
        {
            _log.Info("Retrieving local file list.");

            files = GetLocalFileList();
            return new SyncResult { StatusCode = SyncStatusCode.Ok };
        }

        private SyncResult GetCallback(string fileName, out SyncFile file)
        {
            _log.InfoFormat("Retrieving local file '{0}'.", fileName);

            if (string.IsNullOrEmpty(fileName))
            {
                _log.Warn("No file name was specified.");
                file = null;
                return new SyncResult { StatusCode = SyncStatusCode.BadRequest, Message = "No file name specified." };
            }

            byte[] fileBytes = _fileManager.GetFileContent(fileName);

            if (fileBytes == null)
            {
                _log.WarnFormat("File '{0}' not found.", fileName);
                file = null;
                return new SyncResult { StatusCode = SyncStatusCode.NotFound };
            }

            file = new SyncFile
            {
                FileName = fileName,
                Content = fileBytes,
                Checksum = ChecksumUtil.CreateChecksum(fileBytes)
            };

            _log.InfoFormat("File '{0}' retrieved.", fileName);

            return new SyncResult { StatusCode = SyncStatusCode.Ok };
        }

        private SyncResult PutCallback(string originalChecksum, SyncFile file)
        {
            _log.InfoFormat("Putting file '{0}' with original checksum '{1}'.", file.FileName, originalChecksum);

            if (string.IsNullOrEmpty(file.FileName))
            {
                _log.Warn("No file name was specified.");
                return new SyncResult { StatusCode = SyncStatusCode.BadRequest, Message = "No file name specified." };
            }

            bool fileExists = _fileManager.FileExists(file.FileName);
            bool originalChecksumSpecified = !string.IsNullOrEmpty(originalChecksum);

            if (!fileExists && originalChecksumSpecified)
            {
                _log.WarnFormat("File '{0}' not found.", file.FileName);
                return new SyncResult { StatusCode = SyncStatusCode.NotFound };
            }

            if (fileExists && !originalChecksumSpecified)
            {
                _log.WarnFormat("File '{0}' exists but no original checksum was specified.", file.FileName);
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
                    _log.WarnFormat("File '{0}' original checksum does not match current checksum.", file.FileName);
                    return new SyncResult
                    {
                        StatusCode = SyncStatusCode.FileConflict,
                        Message = "The file checksum does not match the original checksum."
                    };
                }
            }

            _fileManager.SaveFileContent(file.FileName, file.Content);

            _log.InfoFormat("File '{0}' saved.", file.FileName);

            return new SyncResult
            {
                StatusCode = SyncStatusCode.Ok
            };
        }

        private SyncResult DeleteCallback(string fileName, string checksum)
        {
            _log.InfoFormat("Deleting file '{0}' with checksum '{1}'.", fileName, checksum);

            if (string.IsNullOrEmpty(fileName))
            {
                _log.Warn("No file name was specified.");
                return new SyncResult { StatusCode = SyncStatusCode.BadRequest, Message = "No file name specified." };
            }

            if (!_fileManager.FileExists(fileName))
            {
                _log.WarnFormat("File '{0}' not found.", fileName);
                return new SyncResult { StatusCode = SyncStatusCode.NotFound };
            }

            if (string.IsNullOrEmpty(checksum))
            {
                _log.WarnFormat("Checksum not specified for file '{0}'.", fileName);
                return new SyncResult { StatusCode = SyncStatusCode.BadRequest, Message = "No checksum specicified." };
            }

            string localChecksum = _checksumManager.CreateChecksum(fileName);
            if (localChecksum != checksum)
            {
                _log.WarnFormat("File '{0}' specified checksum does not match current checksum.", fileName);
                return new SyncResult
                {
                    StatusCode = SyncStatusCode.FileConflict,
                    Message = "The file checksum does not match the specified checksum."
                };
            }

            _fileManager.DeleteFile(fileName);

            _log.InfoFormat("File '{0}' deleted.", fileName);

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