using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace methanum {
    public class SrvRunner {
        private ServiceHost _sHost;

        public void Start(int port) {
            _sHost = new ServiceHost(typeof (Gate));
            _sHost.Credentials.WindowsAuthentication.AllowAnonymousLogons = true;

            var binding = new NetTcpBinding();
            binding.ReaderQuotas.MaxArrayLength = Int32.MaxValue;
            binding.Security.Mode = SecurityMode.None;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
            binding.Security.Transport.ProtectionLevel = ProtectionLevel.None;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.None;

            _sHost.AddServiceEndpoint(typeof(IGate), binding, string.Format("net.tcp://0.0.0.0:{0}", port));

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