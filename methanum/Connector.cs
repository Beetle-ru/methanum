using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;

namespace methanum {
    public delegate void CbHandler(Event evt);

    public class Connector : IListener {
        private readonly Dictionary<string, CbHandler> _handlers;
        private readonly Dictionary<Guid, CbHandler> _responseHandlers;
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

        /// <summary>
        /// If true then a messages don't lose after disconnection. A messages will be delivered after connect
        /// </summary>
        public bool IsSaveMode;

        protected virtual void OnReceive(Event evt) {
            CbHandler handler = ReceiveEvent;
            if (handler != null) handler.BeginInvoke(evt, null, null);
        }

        //localhost:2255
        public Connector(string address) {
            _handlers = new Dictionary<string, CbHandler>();
            _responseHandlers = new Dictionary<Guid, CbHandler>();
            _fired = new List<Guid>();

            _binding = new NetTcpBinding();

            _endpointToAddress = new EndpointAddress(string.Format("net.tcp://{0}", address));

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

        public void Fire(Event evt) {
            lock (_eventQueue) {
                _eventQueue.Add(evt);
            }
        }

        public void Fire(Event evt, CbHandler responseHandler) {
            lock (_responseHandlers) {
                _responseHandlers[evt.TransactionId] = responseHandler;
            }

            Fire(evt);
        }

        public void Fire(Event evt, string backDestination) {
            evt.BackDestination = backDestination;
            Fire(evt);
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
                    Console.WriteLine(e.Message); // TODO make logger

                    Conect();
                }

                Thread.Sleep(1000);
            }
        }

        public void SetHandler(string destination, CbHandler handler) {
            _handlers[destination] = handler;
        }

        public void Receive(Event evt) {
            if (_fired.Contains(evt.Id)) { // echo bloking
                _fired.Remove(evt.Id);
                return;
            }

            if (evt.IsResponse()) {
                if (_responseHandlers.ContainsKey(evt.TransactionId)) {
                    _responseHandlers[evt.TransactionId].BeginInvoke(evt, null, null);

                    lock (_responseHandlers) {
                        _responseHandlers.Remove(evt.TransactionId);
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