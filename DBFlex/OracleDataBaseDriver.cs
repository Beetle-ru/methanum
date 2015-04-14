using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using methanum;

namespace DBFlex {
    class OracleDataBaseDriver : BaseDataBaseDriver {
        public string ConnectionString;

        public OracleDataBaseDriver(string connectionString) {
            ConnectionString = connectionString;
        }

        private OracleConnection Connect() {
            var conn = new OracleConnection(ConnectionString);
            conn.Open();

            return conn;
        }

        public override Event ExecuteSql(Event evt, string sql, Dictionary<string, object> parameters) {
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
            } catch (Exception e) {
                errorMessage = e.Message;
                stackTrace = e.StackTrace;
            }

            evt.SetData("@ErrorMessage", errorMessage);
            evt.SetData("@StackTrace", stackTrace);
            evt.SetData("@RecordCount", recordCount);

            return evt;
        }

        public override List<Event> Chopper(Event initiator, string sql, Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }
    }
}
