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
            try {
                var uris = new[] {new Uri(string.Format("net.tcp://localhost:{0}", 2255))};

                SHost = new ServiceHost(typeof (Gate), uris);
                SHost.Open();

                foreach (Uri uri2 in SHost.BaseAddresses) {
                    Console.WriteLine("\t{0}", uri2.ToString());
                }
                Console.WriteLine("The service is ready.");
            }
            catch (TimeoutException timeProblem) {
                Console.WriteLine(timeProblem.Message);
            }
            catch (CommunicationException commProblem) {
                Console.WriteLine(commProblem.Message);
            }
        }

        public void Stop() {
            SHost.Close();
        }
    }
}