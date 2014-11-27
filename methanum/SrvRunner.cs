using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace methanum {
    public class SrvRunner {
        private ServiceHost SHost;

        public void Start(int port) {
            var uris = new[] { new Uri(string.Format("net.tcp://localhost:{0}", port)) };

            SHost = new ServiceHost(typeof (Gate), uris);
            SHost.Open();

            foreach (Uri uri2 in SHost.BaseAddresses) {
                Console.WriteLine("Start on: {0}", uri2.ToString());
            }
        }

        public void Stop() {
            SHost.Close();
        }
    }
}