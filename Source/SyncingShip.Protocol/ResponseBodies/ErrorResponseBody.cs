namespace SyncingShip.Protocol.ResponseBodies
{
    internal class ErrorResponseBody : IResponseBody
    {
        public SyncStatusCode status { get; set; }
        public string message { get; set; }
    }
}