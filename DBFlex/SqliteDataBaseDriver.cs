using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using methanum;

namespace DBFlex {
    internal class SqliteDataBaseDriver : BaseDataBaseDriver {
        public string ConnectionString;
        private SQLiteConnection _connection;

        public SqliteDataBaseDriver(string connectionString) {
            ConnectionString = connectionString;
        }

        private SQLiteConnection Connect() {
            if (_connection == null) {
                _connection = new SQLiteConnection(ConnectionString);
                _connection.Open();
            }

            return _connection;
        }

        public override Event ExecuteSql(Event evt, string sql, Dictionary<string, object> parameters) {
            var errorMessage = "";
            var stackTrace = "";
            var recordCount = 0;

            try {
                var conn = Connect();
                using (var command = conn.CreateCommand()) {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;

                    foreach (var parameter in parameters) {
                        command.Parameters.Add(new SQLiteParameter(parameter.Key, parameter.Value));
                    }

                    var reader = command.ExecuteReader();

                    while (reader.Read()) {
                        recordCount++;

                        for (int i = 0; i < reader.FieldCount; i++) {
                            var fieldName = reader.GetName(i);
                            var val = reader[i];
                            var typeVal = val.GetType();

                            var obj = evt.GetObj(fieldName);
                            var currentField = (System.Collections.IList) obj ?? ConstructGenericList(typeVal);

                            currentField.Add(reader[i]);

                            evt.SetData(fieldName, currentField);
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
    }
}