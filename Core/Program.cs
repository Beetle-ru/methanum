using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using methanum;

namespace Core {
    internal class Program {
        private static void Main(string[] args) {
            int port = 0;
            args = new[] { "2255" };
            if ((!args.Any()) || (!int.TryParse(args[0], out port))) {
                Console.WriteLine("Usage:");
                Console.WriteLine("Core.exe port");
                Environment.Exit(1);
            }

            try {
                var coreSrv = new SrvRunner();
                coreSrv.Start(port);

                Console.WriteLine("The Core is ready.");
                Console.WriteLine("Press <ENTER> to terminate Core.");
                Console.ReadLine();

                coreSrv.Stop();
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
    }
}