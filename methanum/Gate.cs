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
        static private List<OperationContext> _subscribers;

        public Gate() {
            if (_subscribers == null)
                _subscribers = new List<OperationContext>();
        }

        public void Subscribe() {
            var oc = OperationContext.Current;

            if (_subscribers.Contains(oc)) {
                _subscribers.Remove(oc);
            }

            _subscribers.Add(oc);
        }

        public void Fire(Event evt) {
            var currentOperationContext = OperationContext.Current;
            var remoteEndpointMessageProperty = currentOperationContext.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            var ip = "";
            var port = 0;

            if (remoteEndpointMessageProperty != null) {
                ip = remoteEndpointMessageProperty.Address;
                port = remoteEndpointMessageProperty.Port;
            }

            Console.WriteLine("Fire [{0}] from {1}:{2}", evt.Id, ip, port);

            for (var i = _subscribers.Count - 1; i >= 0; i--) {
                var operationContext = _subscribers[i];

                if (operationContext.Channel.State == CommunicationState.Opened) {
                    var channel = operationContext.GetCallbackChannel<IListener>();

                    try {
                        ((DelegateReceive) (channel.Receive)).BeginInvoke(evt, null, null);
                        //channel.Receive(evt); // old
                    }
                    catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }
                } else _subscribers.RemoveAt(i);
            }
        }
    }
}
