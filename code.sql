USE master;
GO

IF DB_ID(N'CryptoExchangeDB') IS NOT NULL
BEGIN
    ALTER DATABASE [CryptoExchangeDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [CryptoExchangeDB];
END;
GO

CREATE DATABASE [CryptoExchangeDB];
GO

USE [CryptoExchangeDB];
GO

-- 1. Пользователи
CREATE TABLE dbo.[users] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash NVARCHAR(MAX) NOT NULL,
    created_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_users_created_at DEFAULT SYSDATETIMEOFFSET()
);
GO

-- 2. Роли
CREATE TABLE dbo.[roles] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE,
    description NVARCHAR(255) NULL,
    created_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_roles_created_at DEFAULT SYSDATETIMEOFFSET()
);
GO

-- 3. Связь пользователей и ролей
CREATE TABLE dbo.[user_roles] (
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    assigned_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_user_roles_assigned_at DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT PK_user_roles PRIMARY KEY (user_id, role_id),

    CONSTRAINT FK_user_roles_user
        FOREIGN KEY (user_id) REFERENCES dbo.[users](id) ON DELETE CASCADE,
    CONSTRAINT FK_user_roles_role
        FOREIGN KEY (role_id) REFERENCES dbo.[roles](id) ON DELETE CASCADE
);
GO

-- 4. Активы
CREATE TABLE dbo.[assets] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code VARCHAR(20) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL
);
GO

-- 5. Торговые пары
CREATE TABLE dbo.[trading_pairs] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    symbol VARCHAR(30) NOT NULL UNIQUE,
    base_asset_id BIGINT NOT NULL,
    quote_asset_id BIGINT NOT NULL,

    CONSTRAINT FK_trading_pairs_base_asset
        FOREIGN KEY (base_asset_id) REFERENCES dbo.[assets](id),
    CONSTRAINT FK_trading_pairs_quote_asset
        FOREIGN KEY (quote_asset_id) REFERENCES dbo.[assets](id),

    CONSTRAINT CK_trading_pairs_assets_diff
        CHECK (base_asset_id <> quote_asset_id)
);
GO

-- 6. Кошельки
CREATE TABLE dbo.[wallets] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    asset_id BIGINT NOT NULL,
    available_balance DECIMAL(20,8) NOT NULL
        CONSTRAINT DF_wallets_available DEFAULT 0,
    locked_balance DECIMAL(20,8) NOT NULL
        CONSTRAINT DF_wallets_locked DEFAULT 0,

    CONSTRAINT FK_wallets_user
        FOREIGN KEY (user_id) REFERENCES dbo.[users](id) ON DELETE CASCADE,
    CONSTRAINT FK_wallets_asset
        FOREIGN KEY (asset_id) REFERENCES dbo.[assets](id),

    CONSTRAINT CK_wallets_available_nonneg
        CHECK (available_balance >= 0),
    CONSTRAINT CK_wallets_locked_nonneg
        CHECK (locked_balance >= 0),

    CONSTRAINT UQ_wallets_user_asset UNIQUE (user_id, asset_id)
);
GO

-- 7. История движений по кошельку
CREATE TABLE dbo.[wallet_transactions] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    wallet_id BIGINT NOT NULL,
    transaction_type VARCHAR(20) NOT NULL,
    amount DECIMAL(38,18) NOT NULL,
    note NVARCHAR(255) NULL,
    created_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_wallet_transactions_created_at DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT FK_wallet_transactions_wallet
        FOREIGN KEY (wallet_id) REFERENCES dbo.[wallets](id) ON DELETE CASCADE,

    CONSTRAINT CK_wallet_transactions_type
        CHECK (transaction_type IN ('deposit', 'withdrawal', 'trade_buy', 'trade_sell', 'lock', 'unlock', 'fee', 'adjustment')),
    CONSTRAINT CK_wallet_transactions_amount_nonzero
        CHECK (amount <> 0)
);
GO

-- 8. Ордера
CREATE TABLE dbo.[orders] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    pair_id BIGINT NOT NULL,
    side VARCHAR(4) NOT NULL,
    price DECIMAL(20,8) NOT NULL,
    quantity DECIMAL(20,8) NOT NULL,
    status VARCHAR(20) NOT NULL
        CONSTRAINT DF_orders_status DEFAULT 'new',
    created_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_orders_created_at DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT FK_orders_user
        FOREIGN KEY (user_id) REFERENCES dbo.[users](id),
    CONSTRAINT FK_orders_pair
        FOREIGN KEY (pair_id) REFERENCES dbo.[trading_pairs](id),

    CONSTRAINT CK_orders_side
        CHECK (side IN ('buy', 'sell')),
    CONSTRAINT CK_orders_price_positive
        CHECK (price > 0),
    CONSTRAINT CK_orders_quantity_positive
        CHECK (quantity > 0),
    CONSTRAINT CK_orders_status
        CHECK (status IN ('new', 'filled', 'canceled'))
);
GO

-- 9. История изменения статусов ордеров
CREATE TABLE dbo.[order_status_history] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id BIGINT NOT NULL,
    old_status VARCHAR(20) NULL,
    new_status VARCHAR(20) NOT NULL,
    changed_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_order_status_history_changed_at DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT FK_order_status_history_order
        FOREIGN KEY (order_id) REFERENCES dbo.[orders](id) ON DELETE CASCADE,

    CONSTRAINT CK_order_status_history_old_status
        CHECK (old_status IS NULL OR old_status IN ('new', 'filled', 'canceled')),
    CONSTRAINT CK_order_status_history_new_status
        CHECK (new_status IN ('new', 'filled', 'canceled'))
);
GO

-- 10. Сделки
CREATE TABLE dbo.[trades] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    pair_id BIGINT NOT NULL,
    buy_order_id BIGINT NOT NULL,
    sell_order_id BIGINT NOT NULL,
    price DECIMAL(20,8) NOT NULL,
    quantity DECIMAL(20,8) NOT NULL,
    executed_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_trades_executed_at DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT FK_trades_pair
        FOREIGN KEY (pair_id) REFERENCES dbo.[trading_pairs](id),
    CONSTRAINT FK_trades_buy_order
        FOREIGN KEY (buy_order_id) REFERENCES dbo.[orders](id),
    CONSTRAINT FK_trades_sell_order
        FOREIGN KEY (sell_order_id) REFERENCES dbo.[orders](id),

    CONSTRAINT CK_trades_orders_distinct
        CHECK (buy_order_id <> sell_order_id),
    CONSTRAINT CK_trades_price_positive
        CHECK (price > 0),
    CONSTRAINT CK_trades_quantity_positive
        CHECK (quantity > 0)
);
GO

-- 11. Пополнения
CREATE TABLE dbo.[deposits] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    asset_id BIGINT NOT NULL,
    amount DECIMAL(38,18) NOT NULL,
    tx_hash VARCHAR(255) NULL,
    status VARCHAR(20) NOT NULL
        CONSTRAINT DF_deposits_status DEFAULT 'pending',
    created_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_deposits_created_at DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT FK_deposits_user
        FOREIGN KEY (user_id) REFERENCES dbo.[users](id),
    CONSTRAINT FK_deposits_asset
        FOREIGN KEY (asset_id) REFERENCES dbo.[assets](id),

    CONSTRAINT CK_deposits_amount_positive
        CHECK (amount > 0),
    CONSTRAINT CK_deposits_status
        CHECK (status IN ('pending', 'confirmed', 'rejected'))
);
GO

-- 12. Выводы
CREATE TABLE dbo.[withdrawals] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    asset_id BIGINT NOT NULL,
    amount DECIMAL(38,18) NOT NULL,
    address VARCHAR(255) NOT NULL,
    fee_amount DECIMAL(38,18) NOT NULL
        CONSTRAINT DF_withdrawals_fee DEFAULT 0,
    status VARCHAR(20) NOT NULL
        CONSTRAINT DF_withdrawals_status DEFAULT 'requested',
    created_at DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_withdrawals_created_at DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT FK_withdrawals_user
        FOREIGN KEY (user_id) REFERENCES dbo.[users](id),
    CONSTRAINT FK_withdrawals_asset
        FOREIGN KEY (asset_id) REFERENCES dbo.[assets](id),

    CONSTRAINT CK_withdrawals_amount_positive
        CHECK (amount > 0),
    CONSTRAINT CK_withdrawals_fee_nonneg
        CHECK (fee_amount >= 0),
    CONSTRAINT CK_withdrawals_status
        CHECK (status IN ('requested', 'approved', 'sent', 'failed', 'canceled'))
);
GO

------------------------------------------------------------
-- Тестовые данные
------------------------------------------------------------

INSERT INTO dbo.[users] (email, password_hash)
VALUES
('user1@example.com', 'hash1'),
('user2@example.com', 'hash2');
GO

INSERT INTO dbo.[roles] (role_name, description)
VALUES
('admin', 'Администратор системы'),
('trader', 'Обычный пользователь биржи');
GO

INSERT INTO dbo.[user_roles] (user_id, role_id)
SELECT u.id, r.id
FROM dbo.[users] u
JOIN dbo.[roles] r ON r.role_name = 'trader';
GO

INSERT INTO dbo.[assets] (code, name)
VALUES
('BTC', 'Bitcoin'),
('ETH', 'Ethereum'),
('USDT', 'Tether');
GO

INSERT INTO dbo.[trading_pairs] (symbol, base_asset_id, quote_asset_id)
SELECT 'BTCUSDT', a1.id, a2.id
FROM dbo.[assets] a1
CROSS JOIN dbo.[assets] a2
WHERE a1.code = 'BTC' AND a2.code = 'USDT';

INSERT INTO dbo.[trading_pairs] (symbol, base_asset_id, quote_asset_id)
SELECT 'ETHUSDT', a1.id, a2.id
FROM dbo.[assets] a1
CROSS JOIN dbo.[assets] a2
WHERE a1.code = 'ETH' AND a2.code = 'USDT';
GO

INSERT INTO dbo.[wallets] (user_id, asset_id, available_balance, locked_balance)
SELECT u.id, a.id, 1000, 0
FROM dbo.[users] u
JOIN dbo.[assets] a ON a.code = 'USDT'
WHERE u.email = 'user1@example.com';

INSERT INTO dbo.[wallets] (user_id, asset_id, available_balance, locked_balance)
SELECT u.id, a.id, 0.5, 0
FROM dbo.[users] u
JOIN dbo.[assets] a ON a.code = 'BTC'
WHERE u.email = 'user2@example.com';
GO

INSERT INTO dbo.[wallet_transactions] (wallet_id, transaction_type, amount, note)
SELECT w.id, 'deposit', CAST(w.available_balance AS DECIMAL(38,18)), N'Начальное пополнение'
FROM dbo.[wallets] w
WHERE w.available_balance > 0;
GO

INSERT INTO dbo.[deposits] (user_id, asset_id, amount, tx_hash, status)
SELECT u.id, a.id, CAST(1000 AS DECIMAL(38,18)), 'txhash_demo_001', 'confirmed'
FROM dbo.[users] u
JOIN dbo.[assets] a ON a.code = 'USDT'
WHERE u.email = 'user1@example.com';

INSERT INTO dbo.[deposits] (user_id, asset_id, amount, tx_hash, status)
SELECT u.id, a.id, CAST(0.5 AS DECIMAL(38,18)), 'txhash_demo_002', 'confirmed'
FROM dbo.[users] u
JOIN dbo.[assets] a ON a.code = 'BTC'
WHERE u.email = 'user2@example.com';
GO

INSERT INTO dbo.[withdrawals] (user_id, asset_id, amount, address, fee_amount, status)
SELECT u.id, a.id, CAST(0.1 AS DECIMAL(38,18)), 'bc1qexampleaddress123', CAST(0.0005 AS DECIMAL(38,18)), 'requested'
FROM dbo.[users] u
JOIN dbo.[assets] a ON a.code = 'BTC'
WHERE u.email = 'user2@example.com';
GO