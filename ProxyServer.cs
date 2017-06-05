using System;
using System.Net;
using System.Threading;

namespace FAPS
{
    class ProxyServer
    {
        private static CancellationTokenSource ctsListener = new CancellationTokenSource();
        private static CancellationTokenSource ctsScheduler = new CancellationTokenSource();
        private static CancellationTokenSource ctsMiddleman = new CancellationTokenSource();

        private static Scheduler scheduler;
        private static Middleman monitor;
        private static Listener listener;

        private static String address;
        private static String port;

        private static bool running;

        private static void changePort()
        {
            Console.WriteLine("new port:");
            String new_port = Console.ReadLine();
            if (isDigitsOnly(new_port))
            {
                ctsListener.Cancel();  //stop service
                port = new_port;
                ctsListener = new CancellationTokenSource();
                Console.WriteLine("Starting listener on port " + port);
                listener = new Listener(address, port, monitor, ctsListener.Token);
            }
            else
            {
                Console.WriteLine("Invalid port number");
            }
        }
        private static void stopService()        // TEMP
        {
            ctsListener.Cancel();
            ctsScheduler.Cancel();
            ctsMiddleman.Cancel();
            Console.WriteLine("Service stopped");
        }
        private static void startService()    // TEMP
        {
            ctsListener = new CancellationTokenSource();
            ctsScheduler = new CancellationTokenSource();
            ctsMiddleman = new CancellationTokenSource();
            startProxyServer();
        }
        
        private static void menu()
        {
            String input;
            int num;
            while(true)
            {
                //Console.Clear();
                listener.printConnected();
                Console.WriteLine("===============");
                Console.Write(
                    "1. Change port\n2. Stop service\n3. Start service\n0. Exit\n");
                Console.WriteLine("===============");
                input =  Console.ReadLine();
                if (Int32.TryParse(input, out num))          // check if input is number;
                {
                    switch (num)
                    {
                        case 0:
                            ctsListener.Cancel();
                            ctsScheduler.Cancel();
                            ctsMiddleman.Cancel();
                            Environment.Exit(0);
                            break;
                        case 1:
                            changePort();
                            break;
                        case 2:
                            if (running.Equals(true))
                            {
                                stopService();
                                running = false;
                            }
                            break;
                        case 3:
                            if (running.Equals(false))
                            {
                                startService();
                                running = true;
                            }
                            break;
                        default:
                            Console.WriteLine("Invalid input");
                            break;
                    }
                } else
                {
                    Console.WriteLine("Invalid input");
                }
            }
        }
        
        private static void startProxyServer()
        {
            monitor = new Middleman(ctsMiddleman.Token);

            //scheduler = new Scheduler(monitor, ctsScheduler.Token);
            listener = new Listener(address, port, monitor, ctsListener.Token);

        }

        public static int Main(String[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            if (args.Length >= 2)
            {
                address = args[0];
                port = args[1];
                if(isValidIP(address) && isDigitsOnly(port))
                {
                    startProxyServer();
                    running = true;

                    menu();
                }
                else
                {
                    Console.WriteLine("Invalid address or port");
                }
            }
            else
            {
                Console.WriteLine("Not enough arguments.");
            }

            ctsListener.Dispose();
            ctsScheduler.Dispose();
            ctsMiddleman.Dispose();
            Console.WriteLine("\nPress ENTER to exit...");
            Console.Read();
            return 0;
        }

        private static bool isDigitsOnly(string s)
        {
            foreach (char c in s)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        private static bool isValidIP(string s)
        {
            IPAddress ip;
            return IPAddress.TryParse(s, out ip);
        }

    }
}
