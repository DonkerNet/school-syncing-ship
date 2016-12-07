using System;
using System.IO;
using log4net;
using log4net.Config;
using SyncingShip.Protocol.Exceptions;

namespace SyncingShip.Client
{
    class Program
    {
        private static ILog _log;

        static void Main()
        {
            Console.Title = "SyncingShip CLIENT";

            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            _log = LogManager.GetLogger(typeof(Program));

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            ClientService service = new ClientService();

            bool canRun = true;

            _log.Info("Client started. You can type commands now.");

            while (canRun)
            {
                string command = Console.ReadLine()?.ToLower();
                if (string.IsNullOrEmpty(command))
                    continue;

                switch (command)
                {
                    case "list":
                        try
                        {
                            service.ShowList();
                        }
                        catch (Exception ex)
                        {
                            _log.Error("Failed to show the list.", ex);
                        }
                        break;

                    case "sync":
                        try
                        {
                            service.PerformSync();
                        }
                        catch (SyncException ex)
                        {
                            _log.Error("Sync error {0}", ex);
                        }
                        break;

                    case "exit":
                        canRun = false;
                        break;

                    default:
                        _log.Error("Huh?");
                        break;
                }
            }

            _log.Info("Client stopped.");
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            _log.Error("An unhandled exception occured.", args.ExceptionObject as Exception);
        }
    }
}
