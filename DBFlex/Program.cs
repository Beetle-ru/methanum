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
        public static string DataBaseDriverName;
        public static Connector MainGate;
        public static int Tasks;
        public static object Locker;
        public static IPatternGetter FsPatterns;
        public static IDataBaseDriver DataBaseDriver;

        private static void Main(string[] args) {
            Configuration = ConfigurationManager.OpenExeConfiguration("").AppSettings.Settings;
            ConnectionString = GetSetting("ConnectionString");
            CoreAddress = GetSetting("CoreAddress");
            FsPatternPath = GetSetting("FSPatternPath");
            DataBaseDriverName = GetSetting("DataBaseDriver");

            FsPatterns = new FsPatternGettern();
            FsPatterns.SetSource(FsPatternPath);

            Locker = new object();

            DataBaseDriver = CreateDataBaseDriver(DataBaseDriverName);

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

            Console.WriteLine("REQ  [{0}] Tasks = {1} Destination = \"{2}\"", evt.Id, Tasks, evt.Destination);

            var parameters = new Dictionary<string, object>();
            foreach (var o in evt.Data) {
                if (o.Key.StartsWith(":"))
                    parameters.Add(o.Key, o.Value);
            }

            var respEvt = DataBaseDriver.ExecuteSql(evt.GetResponsForEvent(), sql, parameters);

            MainGate.Fire(respEvt);

            EndTask();

            Console.WriteLine("RESP [{0}] Tasks = {1} Destination = \"{2}\"", evt.Id, Tasks, evt.BackDestination);
        }

        private static IDataBaseDriver CreateDataBaseDriver(string dataBaseDriverName) {
            IDataBaseDriver dataBaseDriver = null;

            switch (dataBaseDriverName.ToUpper()) {
                case "ORACLE" :
                    dataBaseDriver = new OracleDataBaseDriver(ConnectionString);
                    break;
                case "SQLITE":
                    dataBaseDriver = new SqliteDataBaseDriver(ConnectionString);
                    break;
                default :
                    throw new Exception("Не верное имя драйвера базы = " + dataBaseDriverName);

            }

            return dataBaseDriver;
        }
    }
}