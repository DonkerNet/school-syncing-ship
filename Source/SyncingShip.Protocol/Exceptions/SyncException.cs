using System;

namespace SyncingShip.Protocol.Exceptions
{
    public class SyncException : Exception
    {
        public SyncStatusCode? StatusCode { get; set; }

        public SyncException(string message)
            : base(message)
        {
        }

        public SyncException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        public SyncException(string message, SyncStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}