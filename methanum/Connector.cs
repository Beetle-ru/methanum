using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace methanum {
    public delegate void CbHandler(Event evt);

    public class Connector : IListener {
        private readonly Dictionary<string, CbHandler> _handlers;
        private NetTcpBinding _binding;
        private EndpointAddress _endpointToAddress;
        private InstanceContext _instance;
        private DuplexChannelFactory<IGate> _channelFactory;
        private IGate _channel;
        private List<Guid> _fired;
        private Thread _fireThread;
        private List<Event> _eventQueue;

        private Thread _keepAliveThread;

        public event CbHandler ReceiveEvent;

        protected virtual void OnReceive(Event evt) {
            CbHandler handler = ReceiveEvent;
            if (handler != null) handler.BeginInvoke(evt, null, null);
        }

        //localhost:2255
        public Connector(string address) {
            _handlers = new Dictionary<string, CbHandler>();
            _fired = new List<Guid>();

            _binding = new NetTcpBinding();
            _endpointToAddress = new EndpointAddress(string.Format("net.tcp://{0}", address));

            _instance = new InstanceContext(this);

            Conect();

            _eventQueue = new List<Event>();

            _fireThread = new Thread(FireProc);
            _fireThread.IsBackground = true;
            _fireThread.Start();

            _keepAliveThread = new Thread(KeepAliveProc);
            _keepAliveThread.IsBackground = true;
            _keepAliveThread.Start();
        }

        private void Conect() {
            var isSubscribed = false;

            while (!isSubscribed) {
                try {
                    _channelFactory = new DuplexChannelFactory<IGate>(_instance, _binding, _endpointToAddress);

                    _channel = _channelFactory.CreateChannel();

                    _channel.Subscribe();
                    isSubscribed = true;
                }
                catch (Exception e) {
                    if (!(e is EndpointNotFoundException)) throw e;

                    Thread.Sleep(1000);
                }
            }
        }

        public void Fire(Event evt) {
            lock (_eventQueue) {
                _eventQueue.Add(evt);
            }
        }

        private void FireProc() {
            while (true) {
                if (_eventQueue.Any()) {
                    Event evt;

                    lock (_eventQueue) {
                        evt = _eventQueue.First();
                    }
                    
                    try {
                        _fired.Add(evt.Id);
                        _channel.Fire(evt);

                        lock (_eventQueue) {
                            _eventQueue.Remove(evt);
                        }
                    }
                    catch (Exception e) {
                        if (!(e is CommunicationObjectFaultedException)) throw e;

                        Conect();
                    }
                } else Thread.Sleep(10);
            }
        }

        private void KeepAliveProc() {
            while (true) {
                var evt = new Event("keepAlive");

                try {
                    _channel.Fire(evt);
                } catch (Exception e) {
                    if (!(e is CommunicationObjectFaultedException))
                        throw e;

                    Conect();
                }

                Thread.Sleep(1000);
            }
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