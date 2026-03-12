using System.Data;
using CryptoExchangeClient.Data;

namespace CryptoExchangeClient.Services
{
    public sealed class LookupService
    {
        public DataTable GetUsers()
        {
            return Db.ExecuteDataTable(
                "SELECT id, email FROM dbo.[users] ORDER BY email;"
            );
        }

        public DataTable GetAssets()
        {
            return Db.ExecuteDataTable(
                "SELECT id, code + ' - ' + name AS display_name FROM dbo.[assets] ORDER BY code;"
            );
        }

        public DataTable GetTradingPairs()
        {
            return Db.ExecuteDataTable(
                "SELECT id, symbol FROM dbo.[trading_pairs] ORDER BY symbol;"
            );
        }
    }
}