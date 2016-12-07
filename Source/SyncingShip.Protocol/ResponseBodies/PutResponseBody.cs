namespace SyncingShip.Protocol.ResponseBodies
{
    internal class PutResponseBody : IResponseBody
    {
        public SyncStatusCode status { get; set; }
    }
}