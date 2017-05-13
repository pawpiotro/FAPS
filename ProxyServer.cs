using System;
using System.Threading;

namespace FAPS
{
    class ProxyServer
    {
        private static CancellationTokenSource cts = new CancellationTokenSource();

        private static Scheduler scheduler;
        private static Middleman monitor;
        private static Listener listener;

        private static void changePort() { }
        private static void doSmth() { cts.Cancel(false); }
        private static void doSmthElse() { }
        
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
                        Environment.Exit(0);
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
            monitor = new Middleman();
            //scheduler = new Scheduler(monitor, cts.Token);
            //scheduler.startThread();
            listener = new Listener(monitor, cts.Token);
            listener.startThread();
            
            menu();

            cts.Dispose();
            Console.WriteLine("\nPress ENTER to exit...");
            Console.Read();
            return 0;
        }

    }
}
