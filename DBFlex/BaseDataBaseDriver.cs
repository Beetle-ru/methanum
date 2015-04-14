using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using methanum;

namespace DBFlex {
    abstract class BaseDataBaseDriver : IDataBaseDriver {
        public abstract Event ExecuteSql(Event evt, string sql, Dictionary<string, object> parameters);
        public abstract List<Event> Chopper(Event initiator, string sql, Dictionary<string, object> parameters);

        protected System.Collections.IList ConstructGenericList(Type t) {
            return (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t));
        }
    }
}
