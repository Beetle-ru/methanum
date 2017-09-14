using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;

namespace methanum {
    public delegate void CbHandler(Event evt);

    public class Connector : IListener {
        private Dictionary<string, CbHandler> _handlers;
        private Dictionary<Guid, CbHandler> _responseHandlers;
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

        private bool _isSubscribed;

        private object _channelSync = new object();

        /// <summary>
        /// If true then a messages don't lose after disconnection. A messages will be delivered after connect
        /// </summary>
        public bool IsSaveMode;

        protected virtual void OnReceive(Event evt) {
            CbHandler handler = ReceiveEvent;
            if (handler != null) handler.BeginInvoke(evt, null, null);
        }

        //localhost:2255
        public Connector(string ipAddress) {
            init(ipAddress);
        }

        private void init(string ipAddress) {
            _handlers = new Dictionary<string, CbHandler>();
            _responseHandlers = new Dictionary<Guid, CbHandler>();
            _fired = new List<Guid>();

            _binding = new NetTcpBinding();
            _binding.Security.Mode = SecurityMode.None;
            _binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
            _binding.Security.Transport.ProtectionLevel = ProtectionLevel.None;
            _binding.Security.Message.ClientCredentialType = MessageCredentialType.None;

            _endpointToAddress = new EndpointAddress(string.Format("net.tcp://{0}", ipAddress));

            _instance = new InstanceContext(this);

            IsSaveMode = false;

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
            _isSubscribed = false;

            while (!_isSubscribed) {
                try {
                    _channelFactory = new DuplexChannelFactory<IGate>(_instance, _binding, _endpointToAddress);
                    
                    _channel = _channelFactory.CreateChannel();

                    _channel.Subscribe();
                    _isSubscribed = true;
                }
                catch (Exception e) {
                    if (!(e is EndpointNotFoundException)) throw e;

                    Thread.Sleep(1000);
                }
            }
        }

        private void ReConect() {
            lock (_channelSync) {
                try {
                    _channel.Kill();
                }
                catch (Exception e) {
                    Console.WriteLine("(ReConect-exception  \"{0}\"", e.Message);
                }
                Conect();
            }
        }

        public void Fire(Event evt) {
            //if (_handlers.ContainsKey(evt.Destination)) { // Send a local message without external fiering
            //    _handlers[evt.Destination].BeginInvoke(evt, null, null);
            //    return;
            //}

            lock (_eventQueue) {
                _eventQueue.Add(evt);
            }
        }

        public void Fire(Event evt, CbHandler responseHandler) {
            lock (_responseHandlers) {
                _responseHandlers[evt.Transaction] = responseHandler;
            }

            Fire(evt);
        }

        public void Fire(Event evt, string backDestination) {
            evt.BackDestination = backDestination;
            Fire(evt);
        }

        public void Fire(Event evt, string backDestination, Guid transaction) {
            evt.Transaction = transaction;
            Fire(evt, backDestination);
        }

        private void FireProc() {
            while (true) {
                var isHasEventsToFire = false;

                lock (_eventQueue) {
                    isHasEventsToFire = _eventQueue.Any();
                }

                if (_isSubscribed && isHasEventsToFire) {
                    Event evt;

                    lock (_eventQueue) {
                        evt = _eventQueue.First();
                    }

                    try {
                        _fired.Add(evt.Id);

                        if (IsSaveMode) _channel.Fire(evt); // for recovery events after connection

                        lock (_eventQueue) {
                            _eventQueue.Remove(evt);
                        }

                        if (!IsSaveMode) _channel.Fire(evt); // for don`t recovery events after connection
                    }
                    catch (Exception) {
                        if (_isSubscribed)
                            _isSubscribed = false;
                        ReConect();
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
                    Console.WriteLine("(KeepAliveProc-exception  \"{0}\"", e.Message);

                    ReConect();
                }

                Thread.Sleep(1000);
            }
        }

        public void SetHandler(string destination, CbHandler handler) {
            _handlers[destination] = handler;
        }

        public void DeleteHandler(string destination) {
            if(_handlers.ContainsKey(destination)) _handlers.Remove(destination);
        }

        public void Receive(Event evt) {
            if (_fired.Contains(evt.Id)) { // echo bloking
                _fired.Remove(evt.Id);
                return;
            }

            if (evt.IsResponse()) {
                if (_responseHandlers.ContainsKey(evt.Transaction)) {
                    _responseHandlers[evt.Transaction].BeginInvoke(evt, null, null);

                    lock (_responseHandlers) {
                        _responseHandlers.Remove(evt.Transaction);
                    }
                }
            }

            if (_handlers.ContainsKey(evt.Destination)) {
                _handlers[evt.Destination].BeginInvoke(evt, null, null);
            }

            OnReceive(evt);
        }

        static public void HoldProcess() {
            var processName = Process.GetCurrentProcess().ProcessName;
            var defColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("The {0} is ready", processName);
            Console.WriteLine("Press <Enter> to terminate {0}", processName);

            Console.ForegroundColor = defColor;

            Console.ReadLine();
        }
    }
}