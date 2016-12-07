namespace SyncingShip.Protocol.ResponseBodies
{
    internal class GetResponseBody : IResponseBody
    {
        public SyncStatusCode status { get; set; }
        public string filename { get; set; }
        public string checksum { get; set; }
        public string content { get; set; }
    }
}