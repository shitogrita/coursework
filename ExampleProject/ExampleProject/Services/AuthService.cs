using System;
using System.Data.SqlClient;
using CryptoExchangeClient.Data;
using CryptoExchangeClient.Helpers;

namespace CryptoExchangeClient.Services
{
    public sealed class AuthService
    {
        public bool Login(string email, string password, out long userId, out string roleName)
        {
            const string sql =
                "SELECT TOP (1) u.id, r.role_name " +
                "FROM dbo.[users] u " +
                "LEFT JOIN dbo.[user_roles] ur ON ur.user_id = u.id " +
                "LEFT JOIN dbo.[roles] r ON r.id = ur.role_id " +
                "WHERE u.email = @email " +
                "AND u.password_hash = @password_hash " +
                "ORDER BY CASE WHEN r.role_name = 'admin' THEN 0 ELSE 1 END;";

            string hash = PasswordHelper.ComputeSha256(password);

            using (SqlConnection connection = Db.CreateConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@password_hash", hash);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            userId = 0;
                            roleName = string.Empty;
                            return false;
                        }

                        userId = Convert.ToInt64(reader["id"]);
                        roleName = reader["role_name"] == DBNull.Value
                            ? "trader"
                            : Convert.ToString(reader["role_name"]);

                        return true;
                    }
                }
            }
        }
    }
}