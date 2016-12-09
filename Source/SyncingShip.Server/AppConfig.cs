using System.Configuration;
using System.Net;

namespace SyncingShip.Server
{
    public static class AppConfig
    {
        public static string FileDirectory { get; }
        public static int ServerPort { get; }

        static AppConfig()
        {
            FileDirectory = ConfigurationManager.AppSettings["FileDirectory"];
            ServerPort = int.Parse(ConfigurationManager.AppSettings["ServerPort"]);
        }
    }
}