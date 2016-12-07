namespace SyncingShip.Protocol.ResponseBodies
{
    internal class ListResponseBody : IResponseBody
    {
        public SyncStatusCode status { get; set; }
        public File[] files { get; set; }

        public class File
        {
            public string filename { get; set; }
            public string checksum { get; set; }
        }
    }
}