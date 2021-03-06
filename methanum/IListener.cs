﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace methanum {
    public delegate void DelegateReceive(Event evt);
    interface IListener {
        [OperationContract(IsOneWay = true)]
        void Receive(Event evt);
    }
}
