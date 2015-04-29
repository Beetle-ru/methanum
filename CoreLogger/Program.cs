using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using methanum;
using System.Threading;

namespace CoreLogger {
    class Program {
        public static Queue<Event> EventPull;
        public static DateTime Today;
        public static string FileName;
        public static StreamWriter FileStreamWriter;
        public static object SreamLocker;
        public const int Refrashtimeout = 5000; // ms

        static void Main(string[] args) {
            args = new[] {"localhost:2255"};
            if ((!args.Any())) {
                Console.WriteLine("Usage:");
                Console.WriteLine("ClentExample.exe coreAddress:port");
                Environment.Exit(1);
            }

            EventPull = new Queue<Event>();

            Today = DateTime.Today;

            SreamLocker = new object();

            FileOpen();

            var lwt = new Thread(LogWriterThread);
            lwt.IsBackground = true;
            lwt.Start();

            var maingate = new Connector(args[0]);
            maingate.ReceiveEvent += AllEvents;

            Connector.HoldProcess();
        }

        static void AllEvents(Event evt) {
            
            lock (EventPull) {
                if (evt.Destination != "keepAlive") {
                    EventPull.Enqueue(evt);
                    Console.Write(".");
                }
            }
        }

        static void LogWriterThread() {
            var isHas = false;
            var putWhite = false;

            while (true) {
                lock (EventPull) {
                    isHas = EventPull.Any();

                    if (isHas) {
                        var evt = EventPull.Dequeue();

                        WriteLog(evt.ToString());

                        if (!putWhite) {
                            putWhite = true;
                            Console.WriteLine("");
                        }
                        Console.WriteLine(evt.Id);
                        
                    }
                }

                if (!isHas) {
                    Thread.Sleep(Refrashtimeout);
                    putWhite = false;
                }
            }
            
        }

        static string GeneratefileName() {
            FileName = String.Format("log\\{0:0000}{1:00}{2:00}.log", Today.Year, Today.Month, Today.Day);
            return FileName;
        }

        static bool DayChanged() {
            return Today < DateTime.Today;
        }

        static void FileOpen() {
            lock (SreamLocker) {
                Directory.CreateDirectory("log");
                var filename = GeneratefileName();

                if (FileStreamWriter != null) {
                    FileStreamWriter.Close();
                    FileStreamWriter.Dispose();
                }

                FileStreamWriter = new StreamWriter(filename, true);
                FileStreamWriter.AutoFlush = true;
            }
        }

        static void WriteLog(string message) {
            lock (FileName) {
                if (DayChanged() || FileStreamWriter == null) {
                    Today = DateTime.Today;
                    FileOpen();
                }
            }

            lock (SreamLocker) {
                FileStreamWriter.WriteLine(message);
            }
        }
    }
}
