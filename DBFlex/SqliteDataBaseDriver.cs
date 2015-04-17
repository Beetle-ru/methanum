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

        public override List<Event> Chopper(Event initiator, string sql, Dictionary<string, object> parameters) {
            var events = new List<Event>();
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
                        var evt = initiator.GetResponsForEvent();

                        for (int i = 0; i < reader.FieldCount; i++) {
                            var fieldName = reader.GetName(i);
                            if (reader[i] is System.DBNull) {
                                evt.SetData(fieldName, null);
                            }
                            else {
                                var val = reader[i];
                                evt.SetData(fieldName, val);
                            }
                        }

                        evt.SetData("@RecordCount", recordCount);
                        evt.SetData("@HasRows", reader.HasRows);
                        events.Add(evt);

                        recordCount++;
                    }
                }
            } catch (Exception e) {
                var errorMessage = e.Message;
                var stackTrace = e.StackTrace;

                var evt = initiator.GetResponsForEvent();
                evt.SetData("@ErrorMessage", errorMessage);
                evt.SetData("@StackTrace", stackTrace);
                evt.SetData("@RecordCount", recordCount);
            }

            if (events.Any()) events.Last().SetData("@HasRows", false);

            return events;
        }
    }
}