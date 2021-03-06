﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using methanum;

namespace ClentExamle {
    class Program {
        static void Main(string[] args) {
            //args = new[] {"localhost:4444"};
            //args = new[] {"vm-sd3-test01:4441"};
            if ((!args.Any())) {
                Console.WriteLine("Usage:");
                Console.WriteLine("ClentExample.exe coreAddress:port");
                Environment.Exit(1);
            }
            var userName = "";

            while (String.IsNullOrWhiteSpace(userName)) {
                Console.WriteLine("Please write user name:");
                userName = Console.ReadLine();   
            }

            try {
                var maingate = new Connector(args[0]);

                maingate.SetHandler("message", MsgHandler);

                Console.WriteLine("Hello {0}, now you can send messages", userName);

                while (true) {
                    var msg = Console.ReadLine();
                    var evt = new Event("message");
                    evt.SetData("name", userName);
                    evt.SetData("text", msg);

                    maingate.Fire(evt);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        static private void MsgHandler(Event evt) {
            Console.WriteLine("[{0}] >> {1}", evt.GetStr("name"), evt.GetStr("text"));
        }

    }
}
