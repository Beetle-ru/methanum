using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using methanum;

namespace methanum {
    [ServiceContract(CallbackContract = typeof(IListener))]
    public interface IGate {
        [OperationContract]
        void Subscribe();

        [OperationContract]
        void Kill();

        [OperationContract]
        void Fire(Event evt);
    }
}
