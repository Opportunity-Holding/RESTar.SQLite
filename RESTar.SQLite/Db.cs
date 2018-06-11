using System;
using System.Data;
using System.Data.SQLite;

namespace RESTar.SQLite
{
    public static class Db
    {
        internal static int Query(string sql)
        {
            var res = 0;
            Query(sql, command => res = command.ExecuteNonQuery());
            return res;
        }

        private static void Query(string sql, Action<SQLiteCommand> action)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection) {CommandType = CommandType.Text})
                    action(command);
            }
        }

        internal static void Query(string sql, Action<SQLiteDataReader> rowAction)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection) {CommandType = CommandType.Text})
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        rowAction(reader);
            }
        }

        internal static void Transact(Action<SQLiteCommand> commandAction)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection) {CommandType = CommandType.Text})
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            commandAction(command);
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                        }
                    }
                }
            }
        }
    }
}