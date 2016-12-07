﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using SyncingShip.Protocol.Entities;
using SyncingShip.Protocol.RequestBodies;
using SyncingShip.Protocol.ResponseBodies;
using SyncingShip.Protocol.Utils;

namespace SyncingShip.Protocol
{
    public class SyncServer
    {
        private readonly int _listeningPort;
        private readonly Encoding _messageEncoding;

        private Thread _listenerThread;
        private TcpListener _listener;
        private bool _canListen;

        public ServerStatus Status { get; private set; }

        public SyncListCallback ListCallback { get; set; }
        public SyncGetCallback GetCallback { get; set; }
        public SyncPutCallback PutCallback { get; set; }
        public SyncDeleteCallback DeleteCallback { get; set; }

        public SyncServer(int listeningPort)
        {
            ServicePointManager.ServerCertificateValidationCallback = null;
            _listeningPort = listeningPort;
            _messageEncoding = Encoding.GetEncoding(SyncConstants.MessageEncoding);
        }

        public void Start()
        {
            if (Status != ServerStatus.Stopped)
                return;

            Status = ServerStatus.Starting;

            _listenerThread = new Thread(Listen);
            _listenerThread.Start();
        }

        private void Listen()
        {
            _listener = new TcpListener(IPAddress.Any, _listeningPort);

            _listener.Start();
            _canListen = true;
            Status = ServerStatus.Started;

            while (_canListen)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread requestThread = new Thread(HandleRequest);
                    requestThread.Start(client);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.Interrupted)
                        throw;
                }
            }

            Status = ServerStatus.Stopped;
        }

        private void HandleRequest(object parameter)
        {
            TcpClient client = (TcpClient)parameter;
            client.ReceiveTimeout = 500; // TODO: Dirty workaround for not knowing the content byte length

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    string requestMessage;

                    // Read the request
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

                        requestMessage = _messageEncoding.GetString(memoryStream.ToArray());
                    }
                    
                    IResponseBody responseBody = null;

                    // Split the message into the header and body
                    string[] requestMessageParts = requestMessage.Split(new[] { "\r\n\r\n" }, 2, StringSplitOptions.None);

                    // Validate the message: must have a header, header must match protocol specification
                    if (requestMessageParts.Length >= 1)
                    {
                        // Validate the header: must have a verb and protocol/version
                        string[] headerParts = requestMessageParts[0].Split(' ');
                        if (headerParts.Length >= 2)
                        {
                            string verb = headerParts[0].ToUpper();
                            string protocolVersion = headerParts[1];

                            bool isValidProtocol = SyncConstants.ProtocolVersion.Equals(protocolVersion, StringComparison.OrdinalIgnoreCase);

                            if (isValidProtocol)
                                responseBody = ProcessRequest(verb, requestMessageParts.Length >= 2 ? requestMessageParts[1] : null);
                        }
                    }

                    if (responseBody == null)
                        responseBody = new ErrorResponseBody { status = SyncStatusCode.BadRequest };

                    // Write the response message
                    byte[] responseMessageBytes = CreateResponseMessageBytes(responseBody);
                    stream.Write(responseMessageBytes, 0, responseMessageBytes.Length);
                }
            }
            finally
            {
                if (client.Connected)
                    client.Close();
            }
        }

        public void Stop()
        {
            Status = ServerStatus.Stopping;
            _canListen = false;
            _listener.Stop();
        }

        private byte[] CreateResponseMessageBytes(IResponseBody responseBody)
        {
            string responseBodyJson = JsonConvert.SerializeObject(responseBody);
            string responseMessage = $"{SyncVerbs.Response} {SyncConstants.ProtocolVersion}\r\n\r\n{responseBodyJson}";
            return _messageEncoding.GetBytes(responseMessage);
        }

        private IResponseBody ProcessRequest(string verb, string requestJson)
        {
            switch (verb)
            {
                case SyncVerbs.List:
                    {
                        ICollection<SyncFileInfo> fileInfoList;
                        SyncResult result = ListCallback.Invoke(out fileInfoList);
                        if (fileInfoList == null || result.StatusCode != SyncStatusCode.Ok)
                            return new ErrorResponseBody { status = result.StatusCode, message = result .Message};
                        return new ListResponseBody
                        {
                            status = result.StatusCode,
                            files = fileInfoList
                                .Select(r => new ListResponseBody.File { filename = r.FileName, checksum = r.Checksum })
                                .ToArray()
                        };
                    }

                case SyncVerbs.Get:
                    {
                        GetRequestBody getRequestBody = JsonConvert.DeserializeObject<GetRequestBody>(requestJson);
                        SyncFile file;
                        SyncResult result = GetCallback.Invoke(getRequestBody.filename, out file);
                        if (file == null || result.StatusCode != SyncStatusCode.Ok)
                            return new ErrorResponseBody { status = result.StatusCode, message = result.Message };
                        return new GetResponseBody
                        {
                            status = result.StatusCode,
                            filename = file.FileName,
                            checksum = file.Checksum,
                            content = Convert.ToBase64String(file.Content)
                        };
                    }

                case SyncVerbs.Put:
                    {
                        PutRequestBody putRequestBody = JsonConvert.DeserializeObject<PutRequestBody>(requestJson);
                        SyncFile file = new SyncFile
                        {
                            FileName = putRequestBody.filename,
                            Content = Convert.FromBase64String(putRequestBody.content)
                        };
                        file.Checksum = ChecksumUtil.CreateChecksum(file.Content);
                        SyncResult result = PutCallback.Invoke(putRequestBody.original_checksum, file);
                        if (result.StatusCode != SyncStatusCode.Ok)
                            return new ErrorResponseBody { status = result.StatusCode, message = result.Message };
                        return new PutResponseBody
                        {
                            status = result.StatusCode
                        };
                    }

                case SyncVerbs.Delete:
                    {
                        DeleteRequestBody deleteRequestBody = JsonConvert.DeserializeObject<DeleteRequestBody>(requestJson);
                        SyncResult result = DeleteCallback.Invoke(deleteRequestBody.filename, deleteRequestBody.checksum);
                        if (result.StatusCode != SyncStatusCode.Ok)
                            return new ErrorResponseBody { status = result.StatusCode, message = result.Message };
                        return new DeleteResponseBody
                        {
                            status = result.StatusCode
                        };
                    }
            }

            return null;
        }

        public enum ServerStatus
        {
            Stopped = 0,
            Starting = 1,
            Started = 2,
            Stopping = 3
        }

        public delegate SyncResult SyncListCallback(out ICollection<SyncFileInfo> files);
        public delegate SyncResult SyncGetCallback(string fileName, out SyncFile file);
        public delegate SyncResult SyncPutCallback(string originalChecksum, SyncFile file);
        public delegate SyncResult SyncDeleteCallback(string fileName, string checksum);
    }
}