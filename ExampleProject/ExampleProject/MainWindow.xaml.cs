using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CryptoExchangeClient.Data;
using CryptoExchangeClient.Services;

namespace CryptoExchangeClient
{
    public partial class MainWindow : Window
    {
        private readonly long _currentUserId;
        private readonly string _currentUserEmail;
        private readonly LookupService _lookupService = new LookupService();
        private readonly WalletAccountingService _walletAccountingService = new WalletAccountingService();
        private readonly string _currentRole;



        public MainWindow(long userId, string email, string role)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentUserEmail = email;
            _currentRole = role;

            CurrentUserTextBlock.Text = "Текущий пользователь: " + _currentUserEmail +
                                        " (ID = " + _currentUserId + ", роль = " + _currentRole + ")";

            LoadLookups();
            LoadAllTables();
            SetDefaultSelections();
            ApplyCurrentUserBindingForTrader();
            ApplyRolePermissions();
        }

        private void ApplyRolePermissions()
        {
            bool isAdmin = string.Equals(_currentRole, "admin", StringComparison.OrdinalIgnoreCase);

            AddAssetButton.IsEnabled = isAdmin;
            UpdateAssetButton.IsEnabled = isAdmin;
            DeleteAssetButton.IsEnabled = isAdmin;

            AddPairButton.IsEnabled = isAdmin;
            UpdatePairButton.IsEnabled = isAdmin;
            DeletePairButton.IsEnabled = isAdmin;

            AddWalletButton.IsEnabled = isAdmin;
            UpdateWalletButton.IsEnabled = isAdmin;
            DeleteWalletButton.IsEnabled = isAdmin;

            if (isAdmin)
            {
                Title = "CryptoExchangeClient - режим администратора";
            }
            else
            {
                Title = "CryptoExchangeClient - режим пользователя";
            }
        }

        private long ResolveOperationUserId(object selectedValue)
        {
            bool isAdmin = string.Equals(_currentRole, "admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                return _currentUserId;
            }

            if (selectedValue == null)
            {
                throw new Exception("Пользователь не выбран.");
            }

            return Convert.ToInt64(selectedValue);
        }
        private void LoadLookups()
        {
            DataTable users = _lookupService.GetUsers();
            DataTable assets = _lookupService.GetAssets();
            DataTable pairs = _lookupService.GetTradingPairs();

            WalletUserComboBox.ItemsSource = users.DefaultView;
            OrderUserComboBox.ItemsSource = users.DefaultView;
            DepositUserComboBox.ItemsSource = users.DefaultView;
            WithdrawalUserComboBox.ItemsSource = users.DefaultView;
            WalletHistoryUserComboBox.ItemsSource = users.DefaultView;

            WalletAssetComboBox.ItemsSource = assets.DefaultView;
            PairBaseComboBox.ItemsSource = assets.DefaultView;
            PairQuoteComboBox.ItemsSource = assets.DefaultView;
            DepositAssetComboBox.ItemsSource = assets.DefaultView;
            WithdrawalAssetComboBox.ItemsSource = assets.DefaultView;
            WalletHistoryAssetComboBox.ItemsSource = assets.DefaultView;

            OrderPairComboBox.ItemsSource = pairs.DefaultView;
        }

        private void RefreshLookups()
        {
            LoadLookups();
        }

        private void SetDefaultSelections()
        {
            SelectComboItemByText(OrderSideComboBox, "buy");
            SelectComboItemByText(OrderStatusComboBox, "new");
            SelectComboItemByText(DepositStatusComboBox, "pending");
            SelectComboItemByText(WithdrawalStatusComboBox, "requested");
        }

        private void SelectComboItemByText(ComboBox comboBox, string text)
        {
            if (comboBox.Items.Count == 0)
            {
                return;
            }

            foreach (object item in comboBox.Items)
            {
                ComboBoxItem comboBoxItem = item as ComboBoxItem;
                if (comboBoxItem != null && Convert.ToString(comboBoxItem.Content) == text)
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private string GetSelectedComboBoxItemText(ComboBox comboBox)
        {
            ComboBoxItem item = comboBox.SelectedItem as ComboBoxItem;
            if (item == null)
            {
                return string.Empty;
            }

            return Convert.ToString(item.Content);
        }

        private void LoadAllTables()
        {
            LoadAssets();
            LoadPairs();
            LoadWallets();
            LoadOrders();
            LoadDeposits();
            LoadWithdrawals();
            LoadWalletHistory();
        }

        private void ApplyCurrentUserBindingForTrader()
        {
            bool isAdmin = string.Equals(_currentRole, "admin", StringComparison.OrdinalIgnoreCase);

            if (isAdmin)
            {
                return;
            }

            WalletUserComboBox.SelectedValue = _currentUserId;
            WalletUserComboBox.IsEnabled = false;

            OrderUserComboBox.SelectedValue = _currentUserId;
            OrderUserComboBox.IsEnabled = false;

            DepositUserComboBox.SelectedValue = _currentUserId;
            DepositUserComboBox.IsEnabled = false;

            WithdrawalUserComboBox.SelectedValue = _currentUserId;
            WithdrawalUserComboBox.IsEnabled = false;

            WalletHistoryUserComboBox.SelectedValue = _currentUserId;
            WalletHistoryUserComboBox.IsEnabled = false;
        }

        private bool TryParseDecimal(string text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
                   || decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        private long GetSelectedId(DataGrid dataGrid)
        {
            if (dataGrid.SelectedItem == null)
            {
                return 0;
            }

            DataRowView row = dataGrid.SelectedItem as DataRowView;
            if (row == null)
            {
                return 0;
            }

            return Convert.ToInt64(row["id"]);
        }

        // --------------------------------------------------------------------
        // АКТИВЫ
        // --------------------------------------------------------------------
        private void LoadAssets()
        {
            AssetsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT id, code, name FROM dbo.[assets] ORDER BY code;"
            ).DefaultView;
        }

        private void LoadAssetsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAssets();
        }

        private void AssetsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssetsDataGrid.SelectedItem == null)
            {
                return;
            }

            DataRowView row = AssetsDataGrid.SelectedItem as DataRowView;
            if (row == null)
            {
                return;
            }

            AssetIdTextBox.Text = row["id"].ToString();
            AssetCodeTextBox.Text = row["code"].ToString();
            AssetNameTextBox.Text = row["name"].ToString();
        }

        private void AddAssetButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AssetCodeTextBox.Text) || string.IsNullOrWhiteSpace(AssetNameTextBox.Text))
            {
                MessageBox.Show("Введите код и название актива.");
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "INSERT INTO dbo.[assets] (code, name) VALUES (@code, @name);",
                    new SqlParameter("@code", AssetCodeTextBox.Text.Trim()),
                    new SqlParameter("@name", AssetNameTextBox.Text.Trim())
                );

                LoadAssets();
                RefreshLookups();
                MessageBox.Show("Актив добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void UpdateAssetButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            if (!long.TryParse(AssetIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите актив.");
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "UPDATE dbo.[assets] SET code = @code, name = @name WHERE id = @id;",
                    new SqlParameter("@id", id),
                    new SqlParameter("@code", AssetCodeTextBox.Text.Trim()),
                    new SqlParameter("@name", AssetNameTextBox.Text.Trim())
                );

                LoadAssets();
                RefreshLookups();
                MessageBox.Show("Актив обновлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void DeleteAssetButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            if (!long.TryParse(AssetIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите актив.");
                return;
            }

            if (MessageBox.Show("Удалить актив?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "DELETE FROM dbo.[assets] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                LoadAssets();
                RefreshLookups();
                MessageBox.Show("Актив удален.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        // --------------------------------------------------------------------
        // ТОРГОВЫЕ ПАРЫ
        // --------------------------------------------------------------------
        private void LoadPairs()
        {
            PairsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT tp.id, tp.symbol, a1.code AS base_asset, a2.code AS quote_asset, tp.base_asset_id, tp.quote_asset_id " +
                "FROM dbo.[trading_pairs] tp " +
                "JOIN dbo.[assets] a1 ON a1.id = tp.base_asset_id " +
                "JOIN dbo.[assets] a2 ON a2.id = tp.quote_asset_id " +
                "ORDER BY tp.symbol;"
            ).DefaultView;
        }

        private void LoadPairsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPairs();
        }

        private void PairsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PairsDataGrid.SelectedItem == null)
            {
                return;
            }

            DataRowView row = PairsDataGrid.SelectedItem as DataRowView;
            if (row == null)
            {
                return;
            }

            PairIdTextBox.Text = row["id"].ToString();
            PairSymbolTextBox.Text = row["symbol"].ToString();
            PairBaseComboBox.SelectedValue = row["base_asset_id"];
            PairQuoteComboBox.SelectedValue = row["quote_asset_id"];
        }

        private void AddPairButton_Click(object sender, RoutedEventArgs e)
        {
            if (PairBaseComboBox.SelectedValue == null || PairQuoteComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите активы.");
                return;
            }

            if (Convert.ToInt64(PairBaseComboBox.SelectedValue) == Convert.ToInt64(PairQuoteComboBox.SelectedValue))
            {
                MessageBox.Show("Base и Quote активы не должны совпадать.");
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "INSERT INTO dbo.[trading_pairs] (symbol, base_asset_id, quote_asset_id) VALUES (@symbol, @base, @quote);",
                    new SqlParameter("@symbol", PairSymbolTextBox.Text.Trim()),
                    new SqlParameter("@base", Convert.ToInt64(PairBaseComboBox.SelectedValue)),
                    new SqlParameter("@quote", Convert.ToInt64(PairQuoteComboBox.SelectedValue))
                );

                LoadPairs();
                RefreshLookups();
                MessageBox.Show("Торговая пара добавлена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void UpdatePairButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            if (!long.TryParse(PairIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите торговую пару.");
                return;
            }

            if (PairBaseComboBox.SelectedValue == null || PairQuoteComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите активы.");
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "UPDATE dbo.[trading_pairs] SET symbol = @symbol, base_asset_id = @base, quote_asset_id = @quote WHERE id = @id;",
                    new SqlParameter("@id", id),
                    new SqlParameter("@symbol", PairSymbolTextBox.Text.Trim()),
                    new SqlParameter("@base", Convert.ToInt64(PairBaseComboBox.SelectedValue)),
                    new SqlParameter("@quote", Convert.ToInt64(PairQuoteComboBox.SelectedValue))
                );

                LoadPairs();
                RefreshLookups();
                MessageBox.Show("Торговая пара обновлена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void DeletePairButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            if (!long.TryParse(PairIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите торговую пару.");
                return;
            }

            if (MessageBox.Show("Удалить торговую пару?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "DELETE FROM dbo.[trading_pairs] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                LoadPairs();
                RefreshLookups();
                MessageBox.Show("Торговая пара удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        // --------------------------------------------------------------------
        // КОШЕЛЬКИ
        // --------------------------------------------------------------------
        private void LoadWallets()
        {
            WalletsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT w.id, u.email, a.code AS asset_code, w.user_id, w.asset_id, w.available_balance, w.locked_balance " +
                "FROM dbo.[wallets] w " +
                "JOIN dbo.[users] u ON u.id = w.user_id " +
                "JOIN dbo.[assets] a ON a.id = w.asset_id " +
                "ORDER BY u.email, a.code;"
            ).DefaultView;
        }

        private void LoadWalletsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWallets();
        }

        private void WalletsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WalletsDataGrid.SelectedItem == null)
            {
                return;
            }

            DataRowView row = WalletsDataGrid.SelectedItem as DataRowView;
            if (row == null)
            {
                return;
            }

            WalletIdTextBox.Text = row["id"].ToString();
            WalletUserComboBox.SelectedValue = row["user_id"];
            WalletAssetComboBox.SelectedValue = row["asset_id"];
            WalletAvailableTextBox.Text = row["available_balance"].ToString();
            WalletLockedTextBox.Text = row["locked_balance"].ToString();
        }

        private void AddWalletButton_Click(object sender, RoutedEventArgs e)
        {
            decimal available;
            decimal locked;

            if (WalletUserComboBox.SelectedValue == null || WalletAssetComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя и актив.");
                return;
            }

            if (!TryParseDecimal(WalletAvailableTextBox.Text, out available) || !TryParseDecimal(WalletLockedTextBox.Text, out locked))
            {
                MessageBox.Show("Введите корректные суммы.");
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "INSERT INTO dbo.[wallets] (user_id, asset_id, available_balance, locked_balance) VALUES (@user_id, @asset_id, @available, @locked);",
                    new SqlParameter("@user_id", Convert.ToInt64(WalletUserComboBox.SelectedValue)),
                    new SqlParameter("@asset_id", Convert.ToInt64(WalletAssetComboBox.SelectedValue)),
                    new SqlParameter("@available", available),
                    new SqlParameter("@locked", locked)
                );

                LoadWallets();
                MessageBox.Show("Кошелек добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void UpdateWalletButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            decimal available;
            decimal locked;

            if (!long.TryParse(WalletIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите кошелек.");
                return;
            }

            if (WalletUserComboBox.SelectedValue == null || WalletAssetComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя и актив.");
                return;
            }

            if (!TryParseDecimal(WalletAvailableTextBox.Text, out available) || !TryParseDecimal(WalletLockedTextBox.Text, out locked))
            {
                MessageBox.Show("Введите корректные суммы.");
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "UPDATE dbo.[wallets] SET user_id = @user_id, asset_id = @asset_id, available_balance = @available, locked_balance = @locked WHERE id = @id;",
                    new SqlParameter("@id", id),
                    new SqlParameter("@user_id", Convert.ToInt64(WalletUserComboBox.SelectedValue)),
                    new SqlParameter("@asset_id", Convert.ToInt64(WalletAssetComboBox.SelectedValue)),
                    new SqlParameter("@available", available),
                    new SqlParameter("@locked", locked)
                );

                LoadWallets();
                MessageBox.Show("Кошелек обновлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void DeleteWalletButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            if (!long.TryParse(WalletIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите кошелек.");
                return;
            }

            if (MessageBox.Show("Удалить кошелек?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "DELETE FROM dbo.[wallets] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                LoadWallets();
                MessageBox.Show("Кошелек удален.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        // --------------------------------------------------------------------
        // ОРДЕРА
        // --------------------------------------------------------------------
        private void LoadOrders()
        {
            OrdersDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT o.id, u.email, tp.symbol, o.user_id, o.pair_id, o.side, o.price, o.quantity, o.status, o.created_at " +
                "FROM dbo.[orders] o " +
                "JOIN dbo.[users] u ON u.id = o.user_id " +
                "JOIN dbo.[trading_pairs] tp ON tp.id = o.pair_id " +
                "ORDER BY o.created_at DESC;"
            ).DefaultView;
        }

        private void LoadOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem == null)
            {
                return;
            }

            DataRowView row = OrdersDataGrid.SelectedItem as DataRowView;
            if (row == null)
            {
                return;
            }

            OrderIdTextBox.Text = row["id"].ToString();
            OrderUserComboBox.SelectedValue = row["user_id"];
            OrderPairComboBox.SelectedValue = row["pair_id"];
            SelectComboItemByText(OrderSideComboBox, row["side"].ToString());
            OrderPriceTextBox.Text = row["price"].ToString();
            OrderQuantityTextBox.Text = row["quantity"].ToString();
            SelectComboItemByText(OrderStatusComboBox, row["status"].ToString());
        }

        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            decimal price;
            decimal quantity;

            if (OrderUserComboBox.SelectedValue == null || OrderPairComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя и торговую пару.");
                return;
            }

            if (!TryParseDecimal(OrderPriceTextBox.Text, out price) || !TryParseDecimal(OrderQuantityTextBox.Text, out quantity))
            {
                MessageBox.Show("Введите корректные цену и количество.");
                return;
            }

            string side = GetSelectedComboBoxItemText(OrderSideComboBox);
            string status = GetSelectedComboBoxItemText(OrderStatusComboBox);

            try
            {
                Db.ExecuteNonQuery(
                    "INSERT INTO dbo.[orders] (user_id, pair_id, side, price, quantity, status) VALUES (@user_id, @pair_id, @side, @price, @quantity, @status);",
                    new SqlParameter("@user_id", ResolveOperationUserId(OrderUserComboBox.SelectedValue)),
                    new SqlParameter("@pair_id", Convert.ToInt64(OrderPairComboBox.SelectedValue)),
                    new SqlParameter("@side", side),
                    new SqlParameter("@price", price),
                    new SqlParameter("@quantity", quantity),
                    new SqlParameter("@status", status)
                );

                LoadOrders();
                MessageBox.Show("Ордер добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void UpdateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            decimal price;
            decimal quantity;

            if (!long.TryParse(OrderIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите ордер.");
                return;
            }

            if (OrderUserComboBox.SelectedValue == null || OrderPairComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя и торговую пару.");
                return;
            }

            if (!TryParseDecimal(OrderPriceTextBox.Text, out price) || !TryParseDecimal(OrderQuantityTextBox.Text, out quantity))
            {
                MessageBox.Show("Введите корректные цену и количество.");
                return;
            }

            string newStatus = GetSelectedComboBoxItemText(OrderStatusComboBox);
            string side = GetSelectedComboBoxItemText(OrderSideComboBox);

            try
            {
                object oldStatusObj = Db.ExecuteScalar(
                    "SELECT status FROM dbo.[orders] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                string oldStatus = oldStatusObj == null ? string.Empty : Convert.ToString(oldStatusObj);

                Db.ExecuteNonQuery(
                    "UPDATE dbo.[orders] SET user_id = @user_id, pair_id = @pair_id, side = @side, price = @price, quantity = @quantity, status = @status WHERE id = @id;",
                    new SqlParameter("@id", id),
                    new SqlParameter("@user_id", ResolveOperationUserId(OrderUserComboBox.SelectedValue)),
                    new SqlParameter("@pair_id", Convert.ToInt64(OrderPairComboBox.SelectedValue)),
                    new SqlParameter("@side", side),
                    new SqlParameter("@price", price),
                    new SqlParameter("@quantity", quantity),
                    new SqlParameter("@status", newStatus)
                );

                if (oldStatus != newStatus)
                {
                    Db.ExecuteNonQuery(
                        "INSERT INTO dbo.[order_status_history] (order_id, old_status, new_status) VALUES (@order_id, @old_status, @new_status);",
                        new SqlParameter("@order_id", id),
                        new SqlParameter("@old_status", oldStatus),
                        new SqlParameter("@new_status", newStatus)
                    );
                }

                LoadOrders();
                MessageBox.Show("Ордер обновлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            if (!long.TryParse(OrderIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите ордер.");
                return;
            }

            if (MessageBox.Show("Удалить ордер?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "DELETE FROM dbo.[orders] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                LoadOrders();
                MessageBox.Show("Ордер удален.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        // --------------------------------------------------------------------
        // ПОПОЛНЕНИЯ
        // --------------------------------------------------------------------
        private void LoadDeposits()
        {
            DepositsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT d.id, u.email, a.code AS asset_code, d.user_id, d.asset_id, d.amount, d.tx_hash, d.status, d.created_at " +
                "FROM dbo.[deposits] d " +
                "JOIN dbo.[users] u ON u.id = d.user_id " +
                "JOIN dbo.[assets] a ON a.id = d.asset_id " +
                "ORDER BY d.created_at DESC;"
            ).DefaultView;
        }

        private void LoadDepositsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDeposits();
        }

        private void DepositsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepositsDataGrid.SelectedItem == null)
            {
                return;
            }

            DataRowView row = DepositsDataGrid.SelectedItem as DataRowView;
            if (row == null)
            {
                return;
            }

            DepositIdTextBox.Text = row["id"].ToString();
            DepositUserComboBox.SelectedValue = row["user_id"];
            DepositAssetComboBox.SelectedValue = row["asset_id"];
            DepositAmountTextBox.Text = row["amount"].ToString();
            DepositTxHashTextBox.Text = row["tx_hash"].ToString();
            SelectComboItemByText(DepositStatusComboBox, row["status"].ToString());
        }

        private void AddDepositButton_Click(object sender, RoutedEventArgs e)
        {
            decimal amount;

            if (DepositUserComboBox.SelectedValue == null || DepositAssetComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя и актив.");
                return;
            }

            if (!TryParseDecimal(DepositAmountTextBox.Text, out amount))
            {
                MessageBox.Show("Введите корректную сумму.");
                return;
            }

            long userId = ResolveOperationUserId(DepositUserComboBox.SelectedValue);
            long assetId = Convert.ToInt64(DepositAssetComboBox.SelectedValue);
            string status = GetSelectedComboBoxItemText(DepositStatusComboBox);
            string txHash = string.IsNullOrWhiteSpace(DepositTxHashTextBox.Text)
                ? null
                : DepositTxHashTextBox.Text.Trim();

            try
            {
                Db.ExecuteNonQuery(
                    "INSERT INTO dbo.[deposits] (user_id, asset_id, amount, tx_hash, status) " +
                    "VALUES (@user_id, @asset_id, @amount, @tx_hash, @status);",
                    new SqlParameter("@user_id", userId),
                    new SqlParameter("@asset_id", assetId),
                    new SqlParameter("@amount", amount),
                    new SqlParameter("@tx_hash", string.IsNullOrWhiteSpace(txHash) ? (object)DBNull.Value : txHash),
                    new SqlParameter("@status", status)
                );

                if (status == "confirmed")
                {
                    _walletAccountingService.ApplyConfirmedDeposit(
                        userId,
                        assetId,
                        amount,
                        "Пополнение баланса"
                    );
                }

                LoadDeposits();
                LoadWallets();
                MessageBox.Show("Пополнение добавлено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void UpdateDepositButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            decimal amount;

            if (!long.TryParse(DepositIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите пополнение.");
                return;
            }

            if (DepositUserComboBox.SelectedValue == null || DepositAssetComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя и актив.");
                return;
            }

            if (!TryParseDecimal(DepositAmountTextBox.Text, out amount))
            {
                MessageBox.Show("Введите корректную сумму.");
                return;
            }

            long userId = ResolveOperationUserId(DepositUserComboBox.SelectedValue);
            long assetId = Convert.ToInt64(DepositAssetComboBox.SelectedValue);
            string newStatus = GetSelectedComboBoxItemText(DepositStatusComboBox);
            string txHash = string.IsNullOrWhiteSpace(DepositTxHashTextBox.Text)
                ? null
                : DepositTxHashTextBox.Text.Trim();

            try
            {
                object oldStatusObj = Db.ExecuteScalar(
                    "SELECT status FROM dbo.[deposits] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                string oldStatus = oldStatusObj == null || oldStatusObj == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(oldStatusObj);

                Db.ExecuteNonQuery(
                    "UPDATE dbo.[deposits] " +
                    "SET user_id = @user_id, asset_id = @asset_id, amount = @amount, tx_hash = @tx_hash, status = @status " +
                    "WHERE id = @id;",
                    new SqlParameter("@id", id),
                    new SqlParameter("@user_id", userId),
                    new SqlParameter("@asset_id", assetId),
                    new SqlParameter("@amount", amount),
                    new SqlParameter("@tx_hash", string.IsNullOrWhiteSpace(txHash) ? (object)DBNull.Value : txHash),
                    new SqlParameter("@status", newStatus)
                );

                if (oldStatus != "confirmed" && newStatus == "confirmed")
                {
                    _walletAccountingService.ApplyConfirmedDeposit(
                        userId,
                        assetId,
                        amount,
                        "Подтвержденное пополнение"
                    );
                }

                LoadDeposits();
                LoadWallets();
                MessageBox.Show("Пополнение обновлено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void DeleteDepositButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            if (!long.TryParse(DepositIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите пополнение.");
                return;
            }

            if (MessageBox.Show("Удалить пополнение?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "DELETE FROM dbo.[deposits] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                LoadDeposits();
                MessageBox.Show("Пополнение удалено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        // --------------------------------------------------------------------
        // ВЫВОДЫ
        // --------------------------------------------------------------------
        private void LoadWithdrawals()
        {
            WithdrawalsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT w.id, u.email, a.code AS asset_code, w.user_id, w.asset_id, w.amount, w.address, w.fee_amount, w.status, w.created_at " +
                "FROM dbo.[withdrawals] w " +
                "JOIN dbo.[users] u ON u.id = w.user_id " +
                "JOIN dbo.[assets] a ON a.id = w.asset_id " +
                "ORDER BY w.created_at DESC;"
            ).DefaultView;
        }

        private void LoadWithdrawalsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWithdrawals();
        }

        private void WithdrawalsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WithdrawalsDataGrid.SelectedItem == null)
            {
                return;
            }

            DataRowView row = WithdrawalsDataGrid.SelectedItem as DataRowView;
            if (row == null)
            {
                return;
            }

            WithdrawalIdTextBox.Text = row["id"].ToString();
            WithdrawalUserComboBox.SelectedValue = row["user_id"];
            WithdrawalAssetComboBox.SelectedValue = row["asset_id"];
            WithdrawalAmountTextBox.Text = row["amount"].ToString();
            WithdrawalAddressTextBox.Text = row["address"].ToString();
            WithdrawalFeeTextBox.Text = row["fee_amount"].ToString();
            SelectComboItemByText(WithdrawalStatusComboBox, row["status"].ToString());
        }

        private void AddWithdrawalButton_Click(object sender, RoutedEventArgs e)
        {
            decimal amount;
            decimal fee;

            if (WithdrawalUserComboBox.SelectedValue == null || WithdrawalAssetComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя и актив.");
                return;
            }

            if (!TryParseDecimal(WithdrawalAmountTextBox.Text, out amount) || !TryParseDecimal(WithdrawalFeeTextBox.Text, out fee))
            {
                MessageBox.Show("Введите корректные сумму и комиссию.");
                return;
            }

            long userId = ResolveOperationUserId(WithdrawalUserComboBox.SelectedValue);
            long assetId = Convert.ToInt64(WithdrawalAssetComboBox.SelectedValue);
            string status = GetSelectedComboBoxItemText(WithdrawalStatusComboBox);
            string address = WithdrawalAddressTextBox.Text.Trim();

            try
            {
                Db.ExecuteNonQuery(
                    "INSERT INTO dbo.[withdrawals] (user_id, asset_id, amount, address, fee_amount, status) " +
                    "VALUES (@user_id, @asset_id, @amount, @address, @fee_amount, @status);",
                    new SqlParameter("@user_id", userId),
                    new SqlParameter("@asset_id", assetId),
                    new SqlParameter("@amount", amount),
                    new SqlParameter("@address", address),
                    new SqlParameter("@fee_amount", fee),
                    new SqlParameter("@status", status)
                );

                if (status == "sent")
                {
                    _walletAccountingService.ApplySentWithdrawal(
                        userId,
                        assetId,
                        amount,
                        fee,
                        "Вывод средств"
                    );
                }

                LoadWithdrawals();
                LoadWallets();
                MessageBox.Show("Вывод добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void UpdateWithdrawalButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            decimal amount;
            decimal fee;

            if (!long.TryParse(WithdrawalIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите вывод.");
                return;
            }

            if (WithdrawalUserComboBox.SelectedValue == null || WithdrawalAssetComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя и актив.");
                return;
            }

            if (!TryParseDecimal(WithdrawalAmountTextBox.Text, out amount) || !TryParseDecimal(WithdrawalFeeTextBox.Text, out fee))
            {
                MessageBox.Show("Введите корректные сумму и комиссию.");
                return;
            }

            long userId = ResolveOperationUserId(WithdrawalUserComboBox.SelectedValue); long assetId = Convert.ToInt64(WithdrawalAssetComboBox.SelectedValue);
            string newStatus = GetSelectedComboBoxItemText(WithdrawalStatusComboBox);
            string address = WithdrawalAddressTextBox.Text.Trim();

            try
            {
                object oldStatusObj = Db.ExecuteScalar(
                    "SELECT status FROM dbo.[withdrawals] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                string oldStatus = oldStatusObj == null || oldStatusObj == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(oldStatusObj);

                Db.ExecuteNonQuery(
                    "UPDATE dbo.[withdrawals] " +
                    "SET user_id = @user_id, asset_id = @asset_id, amount = @amount, address = @address, fee_amount = @fee_amount, status = @status " +
                    "WHERE id = @id;",
                    new SqlParameter("@id", id),
                    new SqlParameter("@user_id", userId),
                    new SqlParameter("@asset_id", assetId),
                    new SqlParameter("@amount", amount),
                    new SqlParameter("@address", address),
                    new SqlParameter("@fee_amount", fee),
                    new SqlParameter("@status", newStatus)
                );

                if (oldStatus != "sent" && newStatus == "sent")
                {
                    _walletAccountingService.ApplySentWithdrawal(
                        userId,
                        assetId,
                        amount,
                        fee,
                        "Подтвержденный вывод средств"
                    );
                }

                LoadWithdrawals();
                LoadWallets();
                MessageBox.Show("Вывод обновлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void DeleteWithdrawalButton_Click(object sender, RoutedEventArgs e)
        {
            long id;
            if (!long.TryParse(WithdrawalIdTextBox.Text, out id))
            {
                MessageBox.Show("Выберите вывод.");
                return;
            }

            if (MessageBox.Show("Удалить вывод?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Db.ExecuteNonQuery(
                    "DELETE FROM dbo.[withdrawals] WHERE id = @id;",
                    new SqlParameter("@id", id)
                );

                LoadWithdrawals();
                MessageBox.Show("Вывод удален.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        // --------------------------------------------------------------------
        // ОТЧЕТЫ
        // --------------------------------------------------------------------
        private void ReportWalletBalances_Click(object sender, RoutedEventArgs e)
        {
            ReportsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT * FROM dbo.vw_user_wallet_balances ORDER BY email, asset_code;"
            ).DefaultView;
        }

        private void ReportActiveOrders_Click(object sender, RoutedEventArgs e)
        {
            ReportsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT * FROM dbo.vw_active_orders ORDER BY created_at DESC;"
            ).DefaultView;
        }

        private void ReportDeposits_Click(object sender, RoutedEventArgs e)
        {
            ReportsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT * FROM dbo.vw_deposit_history ORDER BY created_at DESC;"
            ).DefaultView;
        }

        private void ReportWithdrawals_Click(object sender, RoutedEventArgs e)
        {
            ReportsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT * FROM dbo.vw_withdrawal_history ORDER BY created_at DESC;"
            ).DefaultView;
        }

        private void ReportTrades_Click(object sender, RoutedEventArgs e)
        {
            ReportsDataGrid.ItemsSource = Db.ExecuteDataTable(
                "SELECT * FROM dbo.vw_trade_history ORDER BY executed_at DESC;"
            ).DefaultView;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
        private void LoadWalletHistory()
        {
            string sql =
                "SELECT wt.id, u.email, a.code AS asset_code, wt.transaction_type, wt.amount, wt.note, wt.created_at " +
                "FROM dbo.[wallet_transactions] wt " +
                "JOIN dbo.[wallets] w ON w.id = wt.wallet_id " +
                "JOIN dbo.[users] u ON u.id = w.user_id " +
                "JOIN dbo.[assets] a ON a.id = w.asset_id " +
                "WHERE 1 = 1 ";

            System.Collections.Generic.List<SqlParameter> parameters = new System.Collections.Generic.List<SqlParameter>();

            bool isAdmin = string.Equals(_currentRole, "admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                sql += "AND u.id = @current_user_id ";
                parameters.Add(new SqlParameter("@current_user_id", _currentUserId));
            }
            else
            {
                if (WalletHistoryUserComboBox.SelectedValue != null)
                {
                    sql += "AND u.id = @user_id ";
                    parameters.Add(new SqlParameter("@user_id", Convert.ToInt64(WalletHistoryUserComboBox.SelectedValue)));
                }
            }

            if (WalletHistoryAssetComboBox.SelectedValue != null)
            {
                sql += "AND a.id = @asset_id ";
                parameters.Add(new SqlParameter("@asset_id", Convert.ToInt64(WalletHistoryAssetComboBox.SelectedValue)));
            }

            sql += "ORDER BY wt.created_at DESC;";

            WalletTransactionsDataGrid.ItemsSource = Db.ExecuteDataTable(
                sql,
                parameters.ToArray()
            ).DefaultView;
        }

        private void LoadWalletHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWalletHistory();
        }

        private void ClearWalletHistoryFilterButton_Click(object sender, RoutedEventArgs e)
        {
            WalletHistoryUserComboBox.SelectedIndex = -1;
            WalletHistoryAssetComboBox.SelectedIndex = -1;
            LoadWalletHistory();
        }

    }

}