using System;
using System.IO;
using log4net;
using log4net.Config;

namespace SyncingShip.Server
{
    class Program
    {
        private static ILog _log;

        static void Main(string[] args)
        {
            Console.Title = "SyncingShip SERVER";

            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            _log = LogManager.GetLogger(typeof(Program));

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            bool canRun = true;

            ServerService service = new ServerService();
            service.Start();

            Console.WriteLine("Type 'exit' to... Exit. DUH!\r\n");

            while (canRun)
            {
                string command = Console.ReadLine()?.ToLower();
                if (string.IsNullOrEmpty(command))
                    continue;

                switch (command)
                {
                    case "exit":
                        canRun = false;
                        break;
                }
            }

            service.Stop();
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            _log.Error("An unhandled exception occured.", args.ExceptionObject as Exception);
        }
    }
}
