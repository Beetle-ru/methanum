﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace methanum
{
    [DataContract]
    [KnownType(typeof(List<bool>))]
    [KnownType(typeof(List<byte>))]
    [KnownType(typeof(List<sbyte>))]
    [KnownType(typeof(List<char>))]
    [KnownType(typeof(List<decimal>))]
    [KnownType(typeof(List<double>))]
    [KnownType(typeof(List<float>))]
    [KnownType(typeof(List<int>))]
    [KnownType(typeof(List<uint>))]
    [KnownType(typeof(List<long>))]
    [KnownType(typeof(List<ulong>))]
    [KnownType(typeof(List<object>))]
    [KnownType(typeof(List<short>))]
    [KnownType(typeof(List<ushort>))]
    [KnownType(typeof(List<string>))]
    [KnownType(typeof(List<DateTime>))]
    public class Event {
        /// <summary>
        /// The Unique id of the event
        /// </summary>
        [DataMember]
        public Guid Id { set; get; }

        /// <summary>
        /// The transaction Id of a events for example it can use for request-response iterations
        /// </summary>
        [DataMember]
        public Guid Transaction { set; get; }

        /// <summary>
        /// Process information
        /// </summary>
        [DataMember]
        public string FromProcess { get; set; }

        /// <summary>
        /// DataTime of event generation
        /// </summary>
        [DataMember]
        public DateTime DataTime { get; set; }

        /// <summary>
        /// Handler name
        /// </summary>
        [DataMember]
        public string Destination { get; set; }

        /// <summary>
        /// It is allowing to generate response
        /// </summary>
        [DataMember]
        public string BackDestination { get; set; }

        /// <summary>
        /// Contains data
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Data { get; set; }

        public Event() {
            Init();
        }

        public Event(string destination) {
            Init();
            Destination = destination;
            BackDestination = "";
        }

        public Event(string destination, Guid transaction) {
            Init();
            Destination = destination;
            BackDestination = "";
            Transaction = transaction;
        }

        private void Init() {
            Data = new Dictionary<string, object>();
            Id = Guid.NewGuid();
            Transaction = Guid.NewGuid();
            DataTime = DateTime.Now;
            var proc = Process.GetCurrentProcess();
            FromProcess = String.Format("{0}, ID[{1}]", proc.ProcessName, proc.Id);
        }


        public Event GetResponsForEvent(string destination) {
            var evt = new Event(destination);
            evt.Transaction = Transaction;
            return evt;
        }

        public Event GetResponsForEvent() {
            var newDestination = string.IsNullOrWhiteSpace(BackDestination) ? "response@[" + Destination + "]" : BackDestination;
            return GetResponsForEvent(newDestination);
        }

        public bool IsResponse() {
            return Transaction != Guid.Empty;
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

        public void SetData(string key, object ojb) {
            Data[key] = ojb;
        }

        public object GetObj(string key) {
            if (!Data.ContainsKey(key))
                return null;
            return Data[key];
        }

        public object GetObj(string key, int index) {
            if (!Data.ContainsKey(key))
                return null;
            return GetItm(key, index);
        }

        public double GetDbl(string key) {
            if (!Data.ContainsKey(key))
                return Double.NaN;
            return Convert.ToDouble(Data[key]);
        }

        public double GetDbl(string key, int index) {
            if (!Data.ContainsKey(key))
                return Double.NaN;
            return Convert.ToDouble(GetItm(key, index));
        }

        public int GetInt(string key) {
            if (!Data.ContainsKey(key))
                return Int32.MinValue;
            return Convert.ToInt32(Data[key]);
        }

        public int GetInt(string key, int index) {
            if (!Data.ContainsKey(key))
                return Int32.MinValue;
            return Convert.ToInt32(GetItm(key, index));
        }

        public bool GetBool(string key) {
            if (!Data.ContainsKey(key))
                return false;
            return Convert.ToBoolean(Data[key]);
        }

        public bool GetBool(string key, int index) {
            if (!Data.ContainsKey(key))
                return false;
            return Convert.ToBoolean(GetItm(key, index));
        }

        public object GetItm(string key, int index) {
            return ((IList) Data[key])[index];
        }

        public string GetStr(string key) {
            if (!Data.ContainsKey(key))
                return null;
            return Convert.ToString(Data[key]);
        }

        public string GetStr(string key, int index) {
            if (!Data.ContainsKey(key))
                return null;
            return Convert.ToString(GetItm(key, index));
        }

        public void SetCustomData(string key, object value) {
            var serializer = new JavaScriptSerializer();

            var str = serializer.Serialize(value);

            SetData(key, str);
        }

        public object GetCustom(string key, Type valueType) {
            if (!Data.ContainsKey(key))
                return null;
            if (Data[key].GetType() != typeof(string))
                return null;

            var serializer = new JavaScriptSerializer();
            var str = (string) Data[key];
            var obj = serializer.Deserialize(str, valueType);

            return obj;
        }
    }
}
