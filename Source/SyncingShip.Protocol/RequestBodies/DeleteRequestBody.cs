namespace SyncingShip.Protocol.RequestBodies
{
    internal class DeleteRequestBody
    {
        public string filename { get; set; }
        public string checksum { get; set; }
    }
}