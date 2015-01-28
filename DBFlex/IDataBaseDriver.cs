using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using methanum;

namespace DBFlex {
    interface IDataBaseDriver {
        Event ExecuteSql(Event evt, string sql, Dictionary<string, object> parameters);
    }
}
