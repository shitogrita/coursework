using System;
using System.Data;
using System.Data.SqlClient;

namespace CryptoExchangeClient.Data
{
    public static class Db
    {
        public const string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=CryptoExchangeDB;Integrated Security=true;";

        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static DataTable ExecuteDataTable(string sql)
        {
            using (SqlConnection connection = CreateConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = CreateConnection())
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        return table;
                    }
                }
            }
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = CreateConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteNonQuery();
                }
            }
        }

        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = CreateConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteScalar();
                }
            }
        }
    }
}