using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class AccountRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? InstitutionName { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public long InitialBalanceMinor { get; set; }
        public long OfficialBalanceMinor { get; set; }
        public string? OfficialBalanceDate { get; set; }
        public string LiquidityPolicy { get; set; } = string.Empty;
        public bool IncludeInNetWorth { get; set; }
        public string? Color { get; set; }
        public bool IsArchived { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    /// <summary>Maps <see cref="Account"/> to and from the `account` table. See docs/02-Architecture.md §3.2.</summary>
    public sealed class AccountRepository
    {
        private const string SelectColumns =
            @"SELECT id AS Id, name AS Name, institution_name AS InstitutionName, type AS Type,
                     currency AS Currency, initial_balance_minor AS InitialBalanceMinor,
                     official_balance_minor AS OfficialBalanceMinor, official_balance_date AS OfficialBalanceDate,
                     liquidity_policy AS LiquidityPolicy, include_in_net_worth AS IncludeInNetWorth,
                     color AS Color, is_archived AS IsArchived, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM account";

        private readonly SQLiteConnection _connection;

        public AccountRepository(SQLiteConnection connection) => _connection = connection;

        public Account Insert(Account account)
        {
            _connection.Execute(
                @"INSERT INTO account
                    (name, institution_name, type, currency, initial_balance_minor, official_balance_minor,
                     official_balance_date, liquidity_policy, include_in_net_worth, color, is_archived,
                     created_at, updated_at)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                account.Name,
                account.InstitutionName,
                account.Type.ToStorageString(),
                account.Currency,
                account.InitialBalanceMinor,
                account.OfficialBalanceMinor,
                account.OfficialBalanceDate?.ToStorageString(),
                account.LiquidityPolicy.ToStorageString(),
                account.IncludeInNetWorth,
                account.Color,
                account.IsArchived,
                account.CreatedAt.ToStorageString(),
                account.UpdatedAt.ToStorageString());

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            account.AssignId(id);
            return account;
        }

        public void Update(Account account)
        {
            _connection.Execute(
                @"UPDATE account SET
                    name = ?, institution_name = ?, type = ?, currency = ?, initial_balance_minor = ?,
                    official_balance_minor = ?, official_balance_date = ?, liquidity_policy = ?,
                    include_in_net_worth = ?, color = ?, is_archived = ?, updated_at = ?
                  WHERE id = ?",
                account.Name,
                account.InstitutionName,
                account.Type.ToStorageString(),
                account.Currency,
                account.InitialBalanceMinor,
                account.OfficialBalanceMinor,
                account.OfficialBalanceDate?.ToStorageString(),
                account.LiquidityPolicy.ToStorageString(),
                account.IncludeInNetWorth,
                account.Color,
                account.IsArchived,
                account.UpdatedAt.ToStorageString(),
                account.Id);
        }

        public Account? FindById(int id)
        {
            var row = _connection.Query<AccountRow>($"{SelectColumns} WHERE id = ?", id).FirstOrDefault();
            return row is null ? null : Map(row);
        }

        public IReadOnlyList<Account> ListActive() =>
            _connection.Query<AccountRow>($"{SelectColumns} WHERE is_archived = 0 ORDER BY name").Select(Map).ToList();

        public IReadOnlyList<Account> ListAll() =>
            _connection.Query<AccountRow>($"{SelectColumns} ORDER BY name").Select(Map).ToList();

        private static Account Map(AccountRow row) => Account.FromStorage(
            row.Id,
            row.Name,
            row.InstitutionName,
            StorageFormat.ParseAccountType(row.Type),
            row.Currency,
            row.InitialBalanceMinor,
            row.OfficialBalanceMinor,
            row.OfficialBalanceDate is null ? null : StorageFormat.ParseDate(row.OfficialBalanceDate),
            StorageFormat.ParseLiquidityPolicy(row.LiquidityPolicy),
            row.IncludeInNetWorth,
            row.Color,
            row.IsArchived,
            StorageFormat.ParseTimestamp(row.CreatedAt),
            StorageFormat.ParseTimestamp(row.UpdatedAt));
    }
}
