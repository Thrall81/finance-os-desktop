using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class AccountBalanceSnapshotRow
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public long BalanceMinor { get; set; }
        public string BalanceDate { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>Maps <see cref="AccountBalanceSnapshot"/> to and from `account_balance_snapshot`.
    /// Snapshots are immutable once recorded — no Update.</summary>
    public sealed class AccountBalanceSnapshotRepository
    {
        private const string SelectColumns =
            @"SELECT id AS Id, account_id AS AccountId, balance_minor AS BalanceMinor,
                     balance_date AS BalanceDate, created_at AS CreatedAt
              FROM account_balance_snapshot";

        private readonly SQLiteConnection _connection;

        public AccountBalanceSnapshotRepository(SQLiteConnection connection) => _connection = connection;

        public AccountBalanceSnapshot Insert(AccountBalanceSnapshot snapshot)
        {
            _connection.Execute(
                "INSERT INTO account_balance_snapshot (account_id, balance_minor, balance_date, created_at) VALUES (?, ?, ?, ?)",
                snapshot.AccountId,
                snapshot.BalanceMinor,
                snapshot.BalanceDate.ToStorageString(),
                snapshot.CreatedAt.ToStorageString());

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            snapshot.AssignId(id);
            return snapshot;
        }

        public IReadOnlyList<AccountBalanceSnapshot> ListForAccount(int accountId) =>
            _connection.Query<AccountBalanceSnapshotRow>(
                    $"{SelectColumns} WHERE account_id = ? ORDER BY balance_date DESC", accountId)
                .Select(Map)
                .ToList();

        public AccountBalanceSnapshot? FindLatestForAccount(int accountId) =>
            _connection.Query<AccountBalanceSnapshotRow>(
                    $"{SelectColumns} WHERE account_id = ? ORDER BY balance_date DESC LIMIT 1", accountId)
                .Select(Map)
                .FirstOrDefault();

        private static AccountBalanceSnapshot Map(AccountBalanceSnapshotRow row) => AccountBalanceSnapshot.FromStorage(
            row.Id, row.AccountId, row.BalanceMinor, StorageFormat.ParseDate(row.BalanceDate), StorageFormat.ParseTimestamp(row.CreatedAt));
    }
}
