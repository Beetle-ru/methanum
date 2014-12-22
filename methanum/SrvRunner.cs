using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace methanum {
    public class SrvRunner {
        private ServiceHost _sHost;

        public void Start(int port) {
            var uris = new[] { new Uri(string.Format("net.tcp://0.0.0.0:{0}", port)) };
            
            _sHost = new ServiceHost(typeof (Gate), uris);

            _sHost.Open();

            foreach (Uri uri2 in _sHost.BaseAddresses) {
                Console.WriteLine("Start on: {0}", uri2.ToString());
            }
        }

        public void Stop() {
            _sHost.Close();
        }
    }
}