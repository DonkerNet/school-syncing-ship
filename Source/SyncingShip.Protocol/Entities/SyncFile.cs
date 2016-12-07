namespace SyncingShip.Protocol.Entities
{
    public class SyncFile
    {
        public string FileName { get; set; }
        public string Checksum { get; set; }
        public byte[] Content { get; set; }
    }
}