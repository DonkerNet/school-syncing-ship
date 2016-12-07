using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SyncingShip.Protocol.Entities;
using SyncingShip.Protocol.Exceptions;
using SyncingShip.Protocol.RequestBodies;
using SyncingShip.Protocol.ResponseBodies;
using SyncingShip.Protocol.Utils;

namespace SyncingShip.Protocol
{
    /// <summary>
    /// Client for communicating with the file sync server.
    /// </summary>
    public class SyncClient
    {
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private readonly Encoding _messageEncoding;

        public SyncClient(IPAddress ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            _messageEncoding = Encoding.GetEncoding(SyncConstants.MessageEncoding);
        }

        /// <summary>
        /// Gets a list with the names and checksums of all the files on the server.
        /// </summary>
        public List<SyncFileInfo> GetFileList()
        {
            byte[] requestMessageBytes = CreateRequestMessageBytes(SyncVerbs.List);
            string responseMessage = ExecuteRequest(requestMessageBytes);

            ListResponseBody responseBody = ParseAndValidateResponse<ListResponseBody>(responseMessage);

            return responseBody.files
                .Select(f => new SyncFileInfo { FileName = f.filename, Checksum = f.checksum })
                .ToList();
        }

        /// <summary>
        /// Retrieves a file from the server.
        /// </summary>
        public SyncFile GetFile(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), "The file name cannot be null.");
            if (fileName.Length == 0)
                throw new ArgumentException("The file name cannot be empty.", nameof(fileName));

            GetRequestBody requestBody = new GetRequestBody
            {
                filename = fileName
            };

            byte[] requestMessageBytes = CreateRequestMessageBytes(SyncVerbs.Get, requestBody);
            string responseMessage = ExecuteRequest(requestMessageBytes);

            GetResponseBody responseBody = ParseAndValidateResponse<GetResponseBody>(responseMessage);

            return new SyncFile
            {
                FileName = fileName,
                Checksum = responseBody.checksum,
                Content = Convert.FromBase64String(responseBody.content)
            };
        }

        /// <summary>
        /// Adds a new file to the server.
        /// </summary>
        public SyncFile AddFile(string fileName, byte[] contentBytes)
        {
            return PutFile(fileName, string.Empty, contentBytes);
        }

        /// <summary>
        /// Updates an existing file on the server.
        /// </summary>
        public SyncFile UpdateFile(string fileName, string originalChecksum, byte[] contentBytes)
        {
            if (originalChecksum == null)
                throw new ArgumentNullException(nameof(originalChecksum), "The original checksum cannot be null.");
            if (originalChecksum.Length == 0)
                throw new ArgumentException("The original checksum cannot be empty.", nameof(originalChecksum));

            return PutFile(fileName, originalChecksum, contentBytes);
        }

        /// <summary>
        /// Adds or updates an existing file on the server.
        /// </summary>
        private SyncFile PutFile(string fileName, string originalChecksum, byte[] contentBytes)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), "The file name cannot be null.");
            if (fileName.Length == 0)
                throw new ArgumentException("The file name cannot be empty.", nameof(fileName));
            if (contentBytes == null)
                throw new ArgumentNullException(nameof(contentBytes), "The content cannot be null.");

            string checksum = ChecksumUtil.CreateChecksum(contentBytes);

            PutRequestBody requestBody = new PutRequestBody
            {
                filename = fileName,
                checksum = checksum,
                original_checksum = originalChecksum,
                content = Convert.ToBase64String(contentBytes)
            };

            byte[] requestMessageBytes = CreateRequestMessageBytes(SyncVerbs.Put, requestBody);
            string responseMessage = ExecuteRequest(requestMessageBytes);

            ParseAndValidateResponse<PutResponseBody>(responseMessage);
            
            return new SyncFile
            {
                FileName = fileName,
                Content = contentBytes,
                Checksum = checksum
            };
        }

        /// <summary>
        /// Deletes a file from the server.
        /// </summary>
        public void DeleteFile(string fileName, string originalChecksum)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), "The file name cannot be null.");
            if (fileName.Length == 0)
                throw new ArgumentException("The file name cannot be empty.", nameof(fileName));
            if (originalChecksum == null)
                throw new ArgumentNullException(nameof(originalChecksum), "The original checksum cannot be null.");
            if (originalChecksum.Length == 0)
                throw new ArgumentException("The original checksum cannot be empty.", nameof(originalChecksum));

            DeleteRequestBody requestBody = new DeleteRequestBody
            {
                filename = fileName,
                checksum = originalChecksum
            };

            byte[] requestMessageBytes = CreateRequestMessageBytes(SyncVerbs.Delete, requestBody);
            string responseMessage = ExecuteRequest(requestMessageBytes);

            ParseAndValidateResponse<DeleteResponseBody>(responseMessage);
        }

        /// <summary>
        /// Creates the request message and returns it as a byte array.
        /// </summary>
        private byte[] CreateRequestMessageBytes(string verb, object requestBody = null)
        {
            string requestMessage;

            if (requestBody != null)
            {
                string requestBodyJson = JsonConvert.SerializeObject(requestBody);
                requestMessage = $"{verb} {SyncConstants.ProtocolVersion}\r\n\r\n{requestBodyJson}";
            }
            else
            {
                requestMessage = $"{verb} {SyncConstants.ProtocolVersion}";
            }

            return _messageEncoding.GetBytes(requestMessage);
        }

        /// <summary>
        /// Executes the request and returns the response message.
        /// </summary>
        private string ExecuteRequest(byte[] requestMessageBytes)
        {
            // NOTE:
            // Content will first be loaded into memory before being written to a file.
            // Should be fun with very large files!  ;-)

            string responseMessage;

            TcpClient client = new TcpClient();
            client.ReceiveTimeout = 1000; // TODO: Dirty workaround for not knowing the content byte length

            try
            {
                client.Connect(_ipAddress, _port);

                using (NetworkStream stream = client.GetStream())
                {
                    // Write the request to the stream
                    stream.Write(requestMessageBytes, 0, requestMessageBytes.Length);

                    // Read the response
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        try
                        {
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            // THIS BLOCKS IF THERE IS NOTHING TO READ!!! We need to know the byte length of the content!
                            {
                                memoryStream.Write(buffer, 0, bytesRead);
                            }
                        }
                        catch (IOException ex)
                        {
                            // TODO: Dirty dirty dirty
                            SocketException sEx = ex.InnerException as SocketException;
                            if (sEx == null || sEx.SocketErrorCode != SocketError.TimedOut)
                                throw;
                        }

                        responseMessage = _messageEncoding.GetString(memoryStream.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SyncException("Communication with the server failed.", ex);
            }
            finally
            {
                // ALWAYS close the connection, even when an error occured
                if (client.Connected)
                    client.Close();
            }

            return responseMessage;
        }

        /// <summary>
        /// Validates the response and returns the deserialized JSON body.
        /// </summary>
        private T ParseAndValidateResponse<T>(string responseMessage)
            where T : IResponseBody
        {
            // Split the message into the header and body
            string[] responseMessageParts = responseMessage.Split(new[] { "\r\n\r\n" }, 2, StringSplitOptions.None);

            // Validate the message: must have a header and body, header must match protocol specification
            if (responseMessageParts.Length < 2 || !responseMessageParts[0].Equals($"{SyncVerbs.Response} {SyncConstants.ProtocolVersion}", StringComparison.OrdinalIgnoreCase))
                throw new SyncException("Invalid response message.");

            T result;

            try
            {
                result = JsonConvert.DeserializeObject<T>(responseMessageParts[1]);
            }
            catch (Exception ex)
            {
                throw new SyncException("Failed to parse the response body.", ex);
            }

            if (result.status != SyncStatusCode.Ok)
            {
                ErrorResponseBody errorResponseBody = result as ErrorResponseBody;
                string message = !string.IsNullOrEmpty(errorResponseBody?.message) ? errorResponseBody.message : result.status.ToString();
                throw new SyncException(message, result.status);
            }

            return result;
        }
    }
}