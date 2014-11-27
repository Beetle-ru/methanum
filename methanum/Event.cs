using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace methanum
{
    [DataContract]
    [KnownType(typeof(List<object>))]
    [KnownType(typeof(List<int>))]
    [KnownType(typeof(List<double>))]
    [KnownType(typeof(List<string>))]
    [KnownType(typeof(List<bool>))]
    [KnownType(typeof(List<char>))]
    [KnownType(typeof(List<byte>))]
    public class Event {
        [DataMember]
        public Guid Id { set; get; }

        [DataMember]
        public Guid AccessId { set; get; }

        [DataMember]
        public string FromProcess { get; set; }

        [DataMember]
        public DateTime DataTime { get; set; }

        [DataMember]
        public string Operation { get; set; }

        [DataMember]
        public Dictionary<string, object> Data { get; set; }

        public Event() {
            Init();
        }

        public Event(string operation) {
            Init();
            Operation = operation;
        }

        private void Init() {
            Data = new Dictionary<string, object>();
            Id = Guid.NewGuid();
            AccessId = new Guid();
            DataTime = DateTime.Now;
            var proc = Process.GetCurrentProcess();
            FromProcess = String.Format("{0}, ID[{1}]", proc.ProcessName, proc.Id);
        }

        public override string ToString() {

            var properties = GetType().GetProperties();

            var sb = new StringBuilder();
            sb.AppendFormat("[{0}]", GetType().Name);

            foreach (var property in properties) {
                if (property.Name == "Data") {
                    sb.Append("\nData = ");
                    string s = string.Format(" {0}", '{');
                    s = Data.Keys.Aggregate(s,
                        (current, key) => current + String.Format("\n  {0}\t:{1}", key, Data[key]));
                    sb.AppendFormat("{0}\n{1}", s, '}');

                }
                else sb.AppendFormat("\n{0} = {1};", property.Name, property.GetValue(this, null));
            }

            return sb.ToString();
        }

    }
}
