using System;

namespace SyncingShip.Protocol.Entities
{
    public class SyncFileInfo
    {
        public string FileName { get; set; }
        public string Checksum { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                int fileNameHc = string.IsNullOrEmpty(FileName) ? 0 : FileName.ToLower().GetHashCode();
                int checksumHc = string.IsNullOrEmpty(Checksum) ? 0 : Checksum.GetHashCode();
                return (fileNameHc * 397) ^ checksumHc;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SyncFileInfo);
        }

        public bool Equals(SyncFileInfo other)
        {
            if (other == null)
                return false;

            if ((string.IsNullOrEmpty(FileName) && !string.IsNullOrEmpty(other.FileName))
                || (!string.IsNullOrEmpty(FileName) && string.IsNullOrEmpty(other.FileName)))
                return false;
            if (!string.Equals(FileName, other.FileName, StringComparison.OrdinalIgnoreCase))
                return false;
            if ((string.IsNullOrEmpty(Checksum) && !string.IsNullOrEmpty(other.Checksum))
                || (!string.IsNullOrEmpty(Checksum) && string.IsNullOrEmpty(other.Checksum)))
                return false;
            if (!string.Equals(Checksum, other.Checksum, StringComparison.Ordinal))
                return false;

            return true;
        }
    }
}