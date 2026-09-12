namespace FinanceOS.Data
{
    /// <summary>
    /// Ordered schema migrations, applied sequentially against PRAGMA user_version by
    /// <see cref="SchemaMigrator"/>. Each migration is a list of individual statements —
    /// sqlite-net-pcl's Execute only prepares one statement per call, so a migration is
    /// never a single multi-statement blob.
    /// Migration 0001 is the initial schema from docs/03-Modele_de_donnees.md §4.
    /// </summary>
    internal static class SchemaMigrations
    {
        // InitialSchema must be declared before OrderedMigrations: static field initializers
        // run in textual declaration order, not in order of use, so the reverse ordering
        // would leave OrderedMigrations[0] null.
        private static readonly string[] InitialSchema =
        {
            @"CREATE TABLE account (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                institution_name TEXT,
                type TEXT NOT NULL,
                currency TEXT NOT NULL DEFAULT 'EUR',
                initial_balance_minor INTEGER NOT NULL DEFAULT 0,
                official_balance_minor INTEGER NOT NULL DEFAULT 0,
                official_balance_date TEXT,
                liquidity_policy TEXT NOT NULL,
                include_in_net_worth INTEGER NOT NULL DEFAULT 1,
                color TEXT,
                is_archived INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            )",

            @"CREATE TABLE account_balance_snapshot (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_id INTEGER NOT NULL REFERENCES account(id) ON DELETE CASCADE,
                balance_minor INTEGER NOT NULL,
                balance_date TEXT NOT NULL,
                created_at TEXT NOT NULL
            )",

            @"CREATE INDEX idx_snapshot_account_date ON account_balance_snapshot(account_id, balance_date)",

            @"CREATE TABLE category (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                parent_id INTEGER REFERENCES category(id) ON DELETE RESTRICT,
                name TEXT NOT NULL,
                type TEXT NOT NULL,
                color TEXT,
                is_system INTEGER NOT NULL DEFAULT 0,
                is_archived INTEGER NOT NULL DEFAULT 0
            )",

            @"CREATE TABLE counterparty (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )",

            @"CREATE TABLE transaction_entry (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_id INTEGER NOT NULL REFERENCES account(id) ON DELETE CASCADE,
                category_id INTEGER REFERENCES category(id) ON DELETE SET NULL,
                counterparty_id INTEGER REFERENCES counterparty(id) ON DELETE SET NULL,
                recurring_operation_id INTEGER REFERENCES recurring_operation(id) ON DELETE SET NULL,
                amount_minor INTEGER NOT NULL,
                currency TEXT NOT NULL DEFAULT 'EUR',
                operation_date TEXT NOT NULL,
                original_label TEXT NOT NULL,
                normalized_label TEXT,
                source TEXT NOT NULL,
                is_internal_transfer INTEGER NOT NULL DEFAULT 0,
                is_excluded_from_budget INTEGER NOT NULL DEFAULT 0,
                notes TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            )",

            @"CREATE INDEX idx_transaction_account_date ON transaction_entry(account_id, operation_date)",

            @"CREATE INDEX idx_transaction_category_date ON transaction_entry(category_id, operation_date)",

            @"CREATE TABLE transfer_link (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                outgoing_transaction_id INTEGER NOT NULL REFERENCES transaction_entry(id) ON DELETE CASCADE,
                incoming_transaction_id INTEGER NOT NULL REFERENCES transaction_entry(id) ON DELETE CASCADE,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL
            )",

            @"CREATE TABLE recurring_operation (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                type TEXT NOT NULL,
                source_account_id INTEGER REFERENCES account(id) ON DELETE CASCADE,
                destination_account_id INTEGER REFERENCES account(id) ON DELETE CASCADE,
                category_id INTEGER REFERENCES category(id) ON DELETE SET NULL,
                counterparty_id INTEGER REFERENCES counterparty(id) ON DELETE SET NULL,
                expected_amount_minor INTEGER NOT NULL,
                frequency TEXT NOT NULL,
                interval_value INTEGER NOT NULL DEFAULT 1,
                start_date TEXT NOT NULL,
                end_date TEXT,
                expected_day_of_month INTEGER,
                date_tolerance_days INTEGER NOT NULL DEFAULT 3,
                amount_tolerance_minor INTEGER NOT NULL DEFAULT 0,
                is_active INTEGER NOT NULL DEFAULT 1,
                notes TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            )",

            @"CREATE TABLE forecast_occurrence (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                recurring_operation_id INTEGER REFERENCES recurring_operation(id) ON DELETE CASCADE,
                account_id INTEGER NOT NULL REFERENCES account(id) ON DELETE CASCADE,
                destination_account_id INTEGER REFERENCES account(id) ON DELETE CASCADE,
                category_id INTEGER REFERENCES category(id) ON DELETE SET NULL,
                counterparty_id INTEGER REFERENCES counterparty(id) ON DELETE SET NULL,
                label TEXT NOT NULL,
                expected_date TEXT NOT NULL,
                expected_amount_minor INTEGER NOT NULL,
                status TEXT NOT NULL,
                matched_transaction_id INTEGER REFERENCES transaction_entry(id) ON DELETE SET NULL,
                is_manually_adjusted INTEGER NOT NULL DEFAULT 0,
                notes TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            )",

            @"CREATE INDEX idx_occurrence_account_date ON forecast_occurrence(account_id, expected_date)",

            @"CREATE INDEX idx_occurrence_recurring ON forecast_occurrence(recurring_operation_id, expected_date)",

            @"CREATE UNIQUE INDEX uq_occurrence_logical_key
                ON forecast_occurrence(recurring_operation_id, expected_date)
                WHERE recurring_operation_id IS NOT NULL",

            @"CREATE TABLE budget (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                year INTEGER NOT NULL,
                month INTEGER NOT NULL,
                status TEXT NOT NULL DEFAULT 'active',
                UNIQUE(year, month)
            )",

            @"CREATE TABLE budget_allocation (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                budget_id INTEGER NOT NULL REFERENCES budget(id) ON DELETE CASCADE,
                category_id INTEGER NOT NULL REFERENCES category(id) ON DELETE CASCADE,
                planned_amount_minor INTEGER NOT NULL,
                UNIQUE(budget_id, category_id)
            )",

            @"CREATE TABLE app_setting (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            )",
        };

        public static readonly string[][] OrderedMigrations =
        {
            InitialSchema,
        };
    }
}
