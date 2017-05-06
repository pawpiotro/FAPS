using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace FAPS
{
    class ProxyServer
    {
        private static Scheduler scheduler;
        private static Monitor monitor;
        private static Listener listener;

        private static void changePort() { }
        private static void doSmth() { }
        private static void doSmthElse() { }
        
        private static void menu()
        {
            String input;
            int num;
            while(true)
            {
                Console.Clear();
                listener.printConnected();
                Console.WriteLine("===============");
                Console.Write(
                    "1. Change port\n2. Do smth\n3. Do smth else\n0. Exit\n");
                Thread.Sleep(1000);
                input =  Console.ReadLine();
                Console.WriteLine(input);
                
                if (Int32.TryParse(input, out num))          // check if input is number;
                {
                    if (num.Equals(0))
                        break;
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
            monitor = new Monitor();
            scheduler = new Scheduler(monitor);
            listener = new Listener(monitor);
            Thread listening = new Thread(listener.StartListening);
            listening.Start();
            menu();
            //Console.WriteLine("\nPress ENTER to exit...");
            //Console.Read();
            return 0;
        }

    }
}
