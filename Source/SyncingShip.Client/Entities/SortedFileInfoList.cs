using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SyncingShip.Protocol.Entities;

namespace SyncingShip.Client.Entities
{
    /// <summary>
    /// Contains all the files of the client and server side and their statuses.
    /// </summary>
    public class SortedFileInfoList : IEnumerable<SyncFileInfo>
    {
        /// <summary>
        /// Gets the files that are only available on the client side because they were newly added.
        /// </summary>
        public List<SyncFileInfo> ClientNew { get; }
        /// <summary>
        /// Gets the files where the client side has versions newer than those of the server.
        /// </summary>
        public List<SyncFileInfo> ClientModified { get; }
        /// <summary>
        /// Gets the files that have been deleted from the client but the server still has them.
        /// </summary>
        public List<SyncFileInfo> ClientDeleted { get; }

        /// <summary>
        /// Gets the files that are only available on the server side because they were newly added.
        /// </summary>
        public List<SyncFileInfo> ServerNew { get; }
        /// <summary>
        /// Gets the files where the server side has versions newer than those of the client.
        /// </summary>
        public List<SyncFileInfo> ServerModified { get; }
        /// <summary>
        /// Gets the files that have been deleted from the server but the client still has them.
        /// </summary>
        public List<SyncFileInfo> ServerDeleted { get; }

        /// <summary>
        /// Gets the files that are available on both the client and server sides and have been unaltered.
        /// </summary>
        public List<SyncFileInfo> Unmodified { get; }

        public SortedFileInfoList()
        {
            ClientNew = new List<SyncFileInfo>();
            ClientModified = new List<SyncFileInfo>();
            ClientDeleted = new List<SyncFileInfo>();

            ServerNew = new List<SyncFileInfo>();
            ServerModified = new List<SyncFileInfo>();
            ServerDeleted = new List<SyncFileInfo>();

            Unmodified = new List<SyncFileInfo>();
        }

        public IEnumerator<SyncFileInfo> GetEnumerator()
        {
            return ClientNew
                .Union(ClientModified)
                .Union(ClientDeleted)
                .Union(ServerNew)
                .Union(ServerModified)
                .Union(ServerDeleted)
                .Union(Unmodified)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}