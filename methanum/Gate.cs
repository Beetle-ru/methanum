using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using methanum;

namespace methanum {
    public class Gate : IGate {
        private static List<OperationContext> _subscribers;

        public Gate() {
            if (_subscribers == null)
                _subscribers = new List<OperationContext>();
        }

        public void Subscribe() {
            var oc = OperationContext.Current;

            _subscribers.RemoveAll(c => c.SessionId == oc.SessionId);
            _subscribers.Add(oc);

            Console.WriteLine("(subscribe \"{0}\")", oc.SessionId);
        }

        public void Kill() {
            var oc = OperationContext.Current;
            _subscribers.RemoveAll(c => c.SessionId == oc.SessionId);

            Console.WriteLine("(kill \"{0}\")", oc.SessionId);
        }

        public void Fire(Event evt) {
            var currentOperationContext = OperationContext.Current;
            var remoteEndpointMessageProperty =
                currentOperationContext.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as
                    RemoteEndpointMessageProperty;
            var ip = "";
            var port = 0;

            if (remoteEndpointMessageProperty != null) {
                ip = remoteEndpointMessageProperty.Address;
                port = remoteEndpointMessageProperty.Port;
            }

            Console.WriteLine("(Fire (event \"{0}\") (from \"{1}:{2}\") (subscribers {3}))", evt.Id, ip, port, _subscribers.Count);

            for (var i = _subscribers.Count - 1; i >= 0; i--) {
                var oc = _subscribers[i];

                if (oc.Channel.State == CommunicationState.Opened) {
                    var channel = oc.GetCallbackChannel<IListener>();


                    try {
                        ((DelegateReceive) (channel.Receive)).BeginInvoke(evt, null, null);
                    }
                    catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }
                }
                else {
                    _subscribers.RemoveAt(i);
                    Console.WriteLine("(dead \"{0}\")", oc.SessionId);
                }
            }
        }
    }
}