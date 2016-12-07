using System;
using SyncingShip.Protocol.Exceptions;

namespace SyncingShip.Client
{
    class Program
    {
        static void Main()
        {
            Console.Title = "SyncingShip CLIENT";
            Console.ForegroundColor = ConsoleColor.White;

            ClientService service = new ClientService();

            bool canRun = true;

            Console.WriteLine("Client started.\r\nYou can type commands now.\r\n");

            while (canRun)
            {
                string command = Console.ReadLine()?.ToLower();
                if (string.IsNullOrEmpty(command))
                    continue;

                switch (command)
                {
                    case "list":
                        service.ShowList();
                        break;

                    case "sync":
                        try
                        {
                            service.PerformSync();
                        }
                        catch (SyncException ex)
                        {
                            Console.WriteLine("Error {0}: {1}", ex.StatusCode, ex.Message);
                        }
                        break;

                    case "exit":
                        canRun = false;
                        break;

                    default:
                        Console.WriteLine("Huh?");
                        break;
                }
            }

            Console.WriteLine("Client stopped.");
        }
    }
}
