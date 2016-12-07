namespace SyncingShip.Protocol.ResponseBodies
{
    internal class DeleteResponseBody : IResponseBody
    {
        public SyncStatusCode status { get; set; }
    }
}