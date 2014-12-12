using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using methanum;
using System.Configuration;

namespace DBFlex {
    internal class Program {
        public static KeyValueConfigurationCollection Configuration;
        public static string ConnectionString;
        public static string CoreAddress;
        public static Connector MainGate;
        public static int Tasks;
        public static object Locker;

        private static void Main(string[] args) {
            Configuration = ConfigurationManager.OpenExeConfiguration("").AppSettings.Settings;
            ConnectionString = GetSetting("ConnectionString");
            CoreAddress = GetSetting("CoreAddress");

            Locker = new object();

            MainGate = new Connector(CoreAddress);

            MainGate.SetHandler("DBFlex.DirectSQL", DirectSql);

            Connector.HoldProcess();
        }

        private static string GetSetting(string settingName) {
            var mode = Configuration["Mode"].Value;
            var setKey = String.Format("{0}@{1}", settingName, mode);
            return Configuration[setKey].Value;
        }

        private static void DirectSql(Event evt) {
            var sql = evt.GetStr("@SQL");

            lock (Locker) {
                Tasks++;
            }

            Console.WriteLine("BEGIN [{0}] Tasks = {1}", evt.Id, Tasks);

            var parameters = new Dictionary<string, object>();
            foreach (var o in evt.Data) {
                if (o.Key.StartsWith(":")) parameters.Add(o.Key, o.Value);
            }

            var respEvt = ExecuteSql(evt.GetResponsForEvent(), sql, parameters);

            MainGate.Fire(respEvt);

            lock (Locker) {
                Tasks--;
            }

            Console.WriteLine("DONE_ [{0}] Tasks = {1}", evt.Id, Tasks);
        }

        private static Event ExecuteSql(Event evt, string sql, Dictionary<string, object> parameters) {
            var errorMessage = "";
            var stackTrace = "";
            var recordCount = 0;

            try {
                using (var conn = Connect()) {
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = sql;
                        command.CommandType = CommandType.Text;

                        foreach (var parameter in parameters) {
                            command.Parameters.Add(parameter.Key, parameter.Value);
                        }

                        var reader = command.ExecuteReader();

                        while (reader.Read()) {
                            recordCount++;

                            for (int i = 0; i < reader.FieldCount; i++) { 
                                var fieldName = reader.GetName(i);
                                var val = reader[i];
                                var typeVal = val.GetType();

                                var obj = evt.GetObj(fieldName);
                                var currentField = (System.Collections.IList)obj ?? ConstructGenericList(typeVal);

                                currentField.Add(reader[i]);

                                evt.SetData(fieldName, currentField);


                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                errorMessage = e.Message;
                stackTrace = e.StackTrace;
            }

            evt.SetData("@ErrorMessage", errorMessage);
            evt.SetData("@StackTrace", stackTrace);
            evt.SetData("@RecordCount", recordCount);

            return evt;
        }

        private static OracleConnection Connect() {
            var conn = new OracleConnection(ConnectionString);
            conn.Open();

            return conn;
        }

        public static System.Collections.IList ConstructGenericList(Type t) {
            return (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t));
        }
    }
}