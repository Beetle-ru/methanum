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
        public static string FsPatternPath;
        public static Connector MainGate;
        public static int Tasks;
        public static object Locker;
        public static IPatternGetter FsPatterns;

        private static void Main(string[] args) {
            Configuration = ConfigurationManager.OpenExeConfiguration("").AppSettings.Settings;
            ConnectionString = GetSetting("ConnectionString");
            CoreAddress = GetSetting("CoreAddress");
            FsPatternPath = GetSetting("FSPatternPath");

            FsPatterns = new FsPatternGettern();
            FsPatterns.SetSource(FsPatternPath);

            Locker = new object();

            MainGate = new Connector(CoreAddress);

            MainGate.SetHandler("DBFlex.DirectSQL", DirectSql);
            MainGate.SetHandler("DBFlex.Pattern", PatternExec);

            Connector.HoldProcess();
        }

        private static string GetSetting(string settingName) {
            var mode = Configuration["Mode"].Value;
            var setKey = String.Format("{0}@{1}", settingName, mode);
            return Configuration[setKey].Value;
        }

        private static void BeginTask() {
            lock (Locker) {
                Tasks++;
            }
        }

        private static void EndTask() {
            lock (Locker) {
                Tasks--;
            }
        }

        private static void DirectSql(Event evt) {
            var sql = evt.GetStr("@SQL");

            ExecTaskResp(evt, sql);
        }

        private static void PatternExec(Event evt) {
            var patternName = evt.GetStr("@Pattern");
            var sql = FsPatterns.GetPattern(patternName);
            
            ExecTaskResp(evt, sql);
        }

        private static void ExecTaskResp(Event evt, string sql) {
            BeginTask();

            Console.WriteLine("BEGIN [{0}] Tasks = {1}", evt.Id, Tasks);

            var parameters = new Dictionary<string, object>();
            foreach (var o in evt.Data) {
                if (o.Key.StartsWith(":"))
                    parameters.Add(o.Key, o.Value);
            }

            var respEvt = ExecuteSql(evt.GetResponsForEvent(), sql, parameters);

            MainGate.Fire(respEvt);

            EndTask();

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