namespace SyncingShip.Protocol.RequestBodies
{
    internal class PutRequestBody
    {
        public string filename { get; set; }
        public string checksum { get; set; }
        public string original_checksum { get; set; }
        public string content { get; set; }
    }
}