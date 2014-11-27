using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace methanum {
    public delegate void CbHandler(Event evt);

    public class Connector : IListener{
        private readonly Dictionary<string, CbHandler> _handlers;
        private EndpointAddress _endpointToAddress;
        private InstanceContext _instance;
        private DuplexChannelFactory<IGate> _channelFactory;
        private IGate _channel;
        private List<Guid> _fired;

        public event CbHandler ReceiveEvent;

        protected virtual void OnReceive(Event evt) {
            CbHandler handler = ReceiveEvent;
            if (handler != null) handler.BeginInvoke(evt, null, null);
        }

        //localhost:2255
        public Connector(string address) {
            _handlers = new Dictionary<string, CbHandler>();
            _fired = new List<Guid>();

            var binding = new NetTcpBinding();
            _endpointToAddress = new EndpointAddress(string.Format("net.tcp://{0}", address));

            _instance = new InstanceContext(this);
            _channelFactory = new DuplexChannelFactory<IGate>(_instance, binding, _endpointToAddress);

            _channel = _channelFactory.CreateChannel();
            _channel.Subscribe();
        }

        public void Fire(Event evt) {
            _fired.Add(evt.Id);
            _channel.Fire(evt);
        }

        public void SetHandler(string operation, CbHandler handler) {
            _handlers[operation] = handler;
        }

        public void Receive(Event evt) {
            if (_fired.Contains(evt.Id)) { // echo bloking
                _fired.Remove(evt.Id);
                return;
            }

            foreach (var cbHandler in _handlers) {
                if (cbHandler.Key == evt.Operation) {
                    cbHandler.Value.BeginInvoke(evt, null, null);
                    break;
                }
            }

            OnReceive(evt);
        }
    }
}
