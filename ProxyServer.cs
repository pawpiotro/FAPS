using System;
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

        private static void changePort()
        {
            
            ctsListener.Cancel();  //stop service
            Console.WriteLine("new port:");
            String new_port = Console.ReadLine();
            listener.Port = new_port;
            listener.startService();
            
        }
        private static void doSmth()        // TEMP
        {
            ctsListener.Cancel();
        }
        private static void doSmthElse()    // TEMP
        {

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
                    "1. Change port\n2. Do smth\n3. Do smth else\n0. Exit\n");
                input =  Console.ReadLine();
                Console.WriteLine(input);
                if (Int32.TryParse(input, out num))          // check if input is number;
                {
                    if (num.Equals(0))
                    {
                        ctsListener.Cancel();
                        ctsScheduler.Cancel();
                        ctsMiddleman.Cancel();
                        Environment.Exit(0);    // TEMP
                    }
                    switch (num)
                    {
                        case 1:
                            changePort();
                            break;
                        case 2:
                            doSmth();
                            break;
                        case 3:
                            doSmthElse();
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
        
        public static int Main(String[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            if (args.Length >= 2)
            {
                
                monitor = new Middleman(ctsMiddleman.Token);

                scheduler = new Scheduler(monitor, ctsScheduler.Token);
                listener = new Listener(args[0], args[1], monitor, ctsListener.Token);

                menu();

                ctsListener.Dispose();
                ctsScheduler.Dispose();
                ctsMiddleman.Dispose();

            }
            else
            {
                Console.WriteLine("Not enough arguments.");
            }
            
            Console.WriteLine("\nPress ENTER to exit...");
            Console.Read();
            return 0;
        }

    }
}
