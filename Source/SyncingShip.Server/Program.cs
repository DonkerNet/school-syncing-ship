using System;

namespace SyncingShip.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "SyncingShip SERVER";
            Console.ForegroundColor = ConsoleColor.White;

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
    }
}
