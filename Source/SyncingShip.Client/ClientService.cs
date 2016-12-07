using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using SyncingShip.Client.Entities;
using SyncingShip.Protocol;
using SyncingShip.Protocol.Entities;
using SyncingShip.Shared;

namespace SyncingShip.Client
{
    public class ClientService
    {
        private readonly FileManager _fileManager;
        private readonly ChecksumManager _checksumManager;
        private readonly SyncClient _client;

        public ClientService()
        {
            _fileManager = new FileManager(ConfigurationManager.AppSettings["FileDirectory"]);
            _checksumManager = new ChecksumManager(
                ConfigurationManager.AppSettings["FileDirectory"],
                ConfigurationManager.AppSettings["ChecksumDirectory"]);
            _client = new SyncClient(
                IPAddress.Parse(ConfigurationManager.AppSettings["ServerIp"]),
                int.Parse(ConfigurationManager.AppSettings["ServerPort"]));
        }

        public void ShowList()
        {
            SortedFileInfoList sortedFileInfoList = GetSortedFileInfoList();

            // Client file info

            if (sortedFileInfoList.ClientNew.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("UNVERSIONED:");
                foreach (SyncFileInfo newClientFile in sortedFileInfoList.ClientNew)
                {
                    Console.WriteLine("  {0}", newClientFile.FileName);
                }
            }

            if (sortedFileInfoList.ClientModified.Count > 0 )
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("MODIFIED:");
                foreach (SyncFileInfo modifiedClientFile in sortedFileInfoList.ClientModified)
                {
                    Console.WriteLine("  {0}", modifiedClientFile.FileName);
                }
            }

            if (sortedFileInfoList.ClientDeleted.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("REMOVED:");
                foreach (SyncFileInfo deletedClientFile in sortedFileInfoList.ClientDeleted)
                {
                    Console.WriteLine("  {0}", deletedClientFile.FileName);
                }
            }

            // Server file info

            if (sortedFileInfoList.ServerNew.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("PENDING:");
                foreach (SyncFileInfo newServerFile in sortedFileInfoList.ServerNew)
                {
                    Console.WriteLine("  {0}", newServerFile.FileName);
                }
            }

            if (sortedFileInfoList.ServerModified.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("OUTDATED:");
                foreach (SyncFileInfo modifiedServerFile in sortedFileInfoList.ServerModified)
                {
                    Console.WriteLine("  {0}", modifiedServerFile.FileName);
                }
            }

            if (sortedFileInfoList.ServerDeleted.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("OBSOLETE:");
                foreach (SyncFileInfo deletedServerFile in sortedFileInfoList.ServerDeleted)
                {
                    Console.WriteLine("  {0}", deletedServerFile.FileName);
                }
            }

            // Other file info

            if (sortedFileInfoList.Unmodified.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("UNMODIFIED:");
                foreach (SyncFileInfo unmodifiedFile in sortedFileInfoList.Unmodified)
                {
                    Console.WriteLine("  {0}", unmodifiedFile.FileName);
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        public void PerformSync()
        {
            Console.WriteLine("File syncing started.");

            SortedFileInfoList sortedFileInfoList = GetSortedFileInfoList();

            // Server side changes

            if (sortedFileInfoList.ClientNew.Count > 0)
            {
                Console.WriteLine("Uploading new files to server...");

                foreach (SyncFileInfo fileInfo in sortedFileInfoList.ClientNew)
                {
                    SyncFile file = _client.AddFile(fileInfo.FileName, _fileManager.GetFileContent(fileInfo.FileName));
                    _checksumManager.SaveChecksum(file.FileName, file.Checksum);
                }
            }

            if (sortedFileInfoList.ClientModified.Count > 0)
            {
                Console.WriteLine("Uploading modified files to server...");

                foreach (SyncFileInfo fileInfo in sortedFileInfoList.ClientModified)
                {
                    string originalChecksum = _checksumManager.GetChecksum(fileInfo.FileName);
                    SyncFile file = _client.UpdateFile(fileInfo.FileName, originalChecksum, _fileManager.GetFileContent(fileInfo.FileName));
                    _checksumManager.SaveChecksum(file.FileName, file.Checksum);
                }
            }

            if (sortedFileInfoList.ClientDeleted.Count > 0)
            {
                Console.WriteLine("Removing files from server...");

                foreach (SyncFileInfo fileInfo in sortedFileInfoList.ClientDeleted)
                {
                    string originalChecksum = _checksumManager.GetChecksum(fileInfo.FileName);
                    _client.DeleteFile(fileInfo.FileName, originalChecksum);
                    _checksumManager.DeleteChecksum(fileInfo.FileName);
                }
            }

            // Client side changes

            if (sortedFileInfoList.ServerNew.Count > 0)
            {
                Console.WriteLine("Downloading new files from server...");

                foreach (SyncFileInfo fileInfo in sortedFileInfoList.ServerNew)
                {
                    SyncFile file = _client.GetFile(fileInfo.FileName);
                    _fileManager.SaveFileContent(file.FileName, file.Content);
                    _checksumManager.SaveChecksum(file.FileName, file.Checksum);
                }
            }

            if (sortedFileInfoList.ServerModified.Count > 0)
            {
                Console.WriteLine("Downloading modified files from server...");

                foreach (SyncFileInfo fileInfo in sortedFileInfoList.ServerModified)
                {
                    SyncFile file = _client.GetFile(fileInfo.FileName);
                    _fileManager.SaveFileContent(file.FileName, file.Content);
                    _checksumManager.SaveChecksum(file.FileName, file.Checksum);
                }
            }

            if (sortedFileInfoList.ServerDeleted.Count > 0)
            {
                Console.WriteLine("Removing obsolete files...");

                foreach (SyncFileInfo fileInfo in sortedFileInfoList.ServerDeleted)
                {
                    _fileManager.DeleteFile(fileInfo.FileName);
                    _checksumManager.DeleteChecksum(fileInfo.FileName);
                }
            }

            // Overwrite checksums of unmodified files, just to be safe

            if (sortedFileInfoList.Unmodified.Count > 0)
            {
                Console.WriteLine("Saving checksums of unmodified files (just to be safe)...");

                foreach (SyncFileInfo fileInfo in sortedFileInfoList.Unmodified)
                {
                    _checksumManager.SaveChecksum(fileInfo.FileName, fileInfo.Checksum);
                }
            }

            Console.WriteLine("File syncing finished.");
        }

        private SortedFileInfoList GetSortedFileInfoList()
        {
            SortedFileInfoList result = new SortedFileInfoList();

            // First, we retrieve all the files from the client and server
            List<SyncFileInfo> serverFiles = _client.GetFileList();
            List<SyncFileInfo> clientFiles = GetLocalFileList();

            // Next, we start comparing these files so we can categorize them
            foreach (SyncFileInfo serverFile in serverFiles)
            {
                SyncFileInfo foundClientFile = null;

                // Check all the client files against the server files
                foreach (SyncFileInfo clientFile in clientFiles)
                {
                    // If a client file's name matches that of the server file, we have a file that exists both server side and client side
                    if (string.Equals(clientFile.FileName, serverFile.FileName))
                    {
                        // If the checksums match, nothing has been changed on either side, so we categorize it as UNMODIFIED
                        if (clientFile.Checksum == serverFile.Checksum)
                            result.Unmodified.Add(clientFile);
                        // Otherwise, we have to check the reason for the checksum difference
                        else
                        {
                            string storedChecksum = _checksumManager.GetChecksum(clientFile.FileName);

                            // If the original/previous checksum of the client file matches the one of the server, the client file is a newer/updated version
                            if (string.Equals(storedChecksum, serverFile.Checksum))
                                result.ClientModified.Add(clientFile);
                            // Otherwise, the server has a newer version
                            else
                                result.ServerModified.Add(serverFile);
                        }

                        foundClientFile = clientFile;
                        break;
                    }
                }

                // If a client file that matches the server file was found, we can remove it from the client file list since we just categorized it
                if (foundClientFile != null)
                    clientFiles.Remove(foundClientFile);
                // Otherwise, we need to check the reason why the server has a file that the client does not have
                else
                {
                    string storedChecksum = _checksumManager.GetChecksum(serverFile.FileName);

                    // If no orginal/previous checksum was stored on the client side for the server's file, the server has a new file
                    if (string.IsNullOrEmpty(storedChecksum))
                        result.ServerNew.Add(serverFile);
                    // Otherwise, it means the client previously had the file but it was removed
                    else
                        result.ClientDeleted.Add(serverFile);
                }
            }

            // Now check the remaining client files that were not present on the server
            foreach (SyncFileInfo clientFile in clientFiles)
            {
                string storedChecksum = _checksumManager.GetChecksum(clientFile.FileName);

                // If a checksum was not previously stored, it means the client has a new file
                if (string.IsNullOrEmpty(storedChecksum))
                    result.ClientNew.Add(clientFile);
                // Otherwise, it means the server once had it but it was deleted
                else
                    result.ServerDeleted.Add(clientFile);
            }

            return result;
        }

        private List<SyncFileInfo> GetLocalFileList()
        {
            List<SyncFileInfo> localFileList = new List<SyncFileInfo>();

            IEnumerable<string> filePaths = _fileManager.GetFilePaths();

            foreach (FileInfo fileInfo in filePaths.Select(fp => new FileInfo(fp)))
            {
                SyncFileInfo file = new SyncFileInfo
                {
                    FileName = fileInfo.Name,
                    Checksum = _checksumManager.CreateChecksum(fileInfo.Name)
                };

                localFileList.Add(file);
            }

            return localFileList;
        }
    }
}