using System.Configuration;
using System.Net;

namespace SyncingShip.Client
{
    public static class AppConfig
    {
        public static string FileDirectory { get; }
        public static string ChecksumDirectory { get; }
        public static IPAddress ServerIp { get; }
        public static int ServerPort { get; }

        static AppConfig()
        {
            FileDirectory = ConfigurationManager.AppSettings["FileDirectory"];
            ChecksumDirectory = ConfigurationManager.AppSettings["ChecksumDirectory"];
            ServerIp = IPAddress.Parse(ConfigurationManager.AppSettings["ServerIp"]);
            ServerPort = int.Parse(ConfigurationManager.AppSettings["ServerPort"]);
        }
    }
}