using System;
using System.Data.SqlClient;
using CryptoExchangeClient.Data;

namespace CryptoExchangeClient.Services
{
    public sealed class WalletAccountingService
    {
        private long EnsureWalletExists(SqlConnection connection, SqlTransaction transaction, long userId, long assetId)
        {
            using (SqlCommand findCommand = new SqlCommand(
                "SELECT id FROM dbo.[wallets] WHERE user_id = @user_id AND asset_id = @asset_id;",
                connection,
                transaction))
            {
                findCommand.Parameters.AddWithValue("@user_id", userId);
                findCommand.Parameters.AddWithValue("@asset_id", assetId);

                object existingWalletId = findCommand.ExecuteScalar();
                if (existingWalletId != null && existingWalletId != DBNull.Value)
                {
                    return Convert.ToInt64(existingWalletId);
                }
            }

            using (SqlCommand insertCommand = new SqlCommand(
                "INSERT INTO dbo.[wallets] (user_id, asset_id, available_balance, locked_balance) " +
                "VALUES (@user_id, @asset_id, 0, 0);" +
                "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                connection,
                transaction))
            {
                insertCommand.Parameters.AddWithValue("@user_id", userId);
                insertCommand.Parameters.AddWithValue("@asset_id", assetId);

                object newWalletId = insertCommand.ExecuteScalar();
                return Convert.ToInt64(newWalletId);
            }
        }

        private decimal GetAvailableBalance(SqlConnection connection, SqlTransaction transaction, long walletId)
        {
            using (SqlCommand command = new SqlCommand(
                "SELECT available_balance FROM dbo.[wallets] WHERE id = @wallet_id;",
                connection,
                transaction))
            {
                command.Parameters.AddWithValue("@wallet_id", walletId);

                object result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception("Кошелек не найден.");
                }

                return Convert.ToDecimal(result);
            }
        }

        private void AddWalletTransaction(
            SqlConnection connection,
            SqlTransaction transaction,
            long walletId,
            string transactionType,
            decimal amount,
            string note)
        {
            using (SqlCommand command = new SqlCommand(
                "INSERT INTO dbo.[wallet_transactions] (wallet_id, transaction_type, amount, note) " +
                "VALUES (@wallet_id, @transaction_type, @amount, @note);",
                connection,
                transaction))
            {
                command.Parameters.AddWithValue("@wallet_id", walletId);
                command.Parameters.AddWithValue("@transaction_type", transactionType);
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@note", note);
                command.ExecuteNonQuery();
            }
        }

        public void ApplyConfirmedDeposit(long userId, long assetId, decimal amount, string note)
        {
            using (SqlConnection connection = Db.CreateConnection())
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        long walletId = EnsureWalletExists(connection, transaction, userId, assetId);

                        using (SqlCommand updateCommand = new SqlCommand(
                            "UPDATE dbo.[wallets] " +
                            "SET available_balance = available_balance + @amount " +
                            "WHERE id = @wallet_id;",
                            connection,
                            transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@amount", amount);
                            updateCommand.Parameters.AddWithValue("@wallet_id", walletId);
                            updateCommand.ExecuteNonQuery();
                        }

                        AddWalletTransaction(
                            connection,
                            transaction,
                            walletId,
                            "deposit",
                            amount,
                            note
                        );

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void ApplySentWithdrawal(long userId, long assetId, decimal amount, decimal feeAmount, string note)
        {
            using (SqlConnection connection = Db.CreateConnection())
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        long walletId = EnsureWalletExists(connection, transaction, userId, assetId);

                        decimal currentBalance = GetAvailableBalance(connection, transaction, walletId);
                        decimal totalToDeduct = amount + feeAmount;

                        if (currentBalance < totalToDeduct)
                        {
                            throw new Exception("Недостаточно средств на кошельке для выполнения вывода.");
                        }

                        using (SqlCommand updateCommand = new SqlCommand(
                            "UPDATE dbo.[wallets] " +
                            "SET available_balance = available_balance - @total " +
                            "WHERE id = @wallet_id;",
                            connection,
                            transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@total", totalToDeduct);
                            updateCommand.Parameters.AddWithValue("@wallet_id", walletId);
                            updateCommand.ExecuteNonQuery();
                        }

                        AddWalletTransaction(
                            connection,
                            transaction,
                            walletId,
                            "withdrawal",
                            -amount,
                            note
                        );

                        if (feeAmount > 0)
                        {
                            AddWalletTransaction(
                                connection,
                                transaction,
                                walletId,
                                "fee",
                                -feeAmount,
                                "Комиссия за вывод"
                            );
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}