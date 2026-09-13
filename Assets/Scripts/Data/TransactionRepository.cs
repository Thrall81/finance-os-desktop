using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class TransactionRow
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? CategoryId { get; set; }
        public int? CounterpartyId { get; set; }
        public int? RecurringOperationId { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string OperationDate { get; set; } = string.Empty;
        public string OriginalLabel { get; set; } = string.Empty;
        public string? NormalizedLabel { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool IsInternalTransfer { get; set; }
        public bool IsExcludedFromBudget { get; set; }
        public string? Notes { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    /// <summary>Maps <see cref="Transaction"/> to and from the `transaction_entry` table.</summary>
    public sealed class TransactionRepository
    {
        private const string SelectColumns =
            @"SELECT id AS Id, account_id AS AccountId, category_id AS CategoryId, counterparty_id AS CounterpartyId,
                     recurring_operation_id AS RecurringOperationId, amount_minor AS AmountMinor, currency AS Currency,
                     operation_date AS OperationDate, original_label AS OriginalLabel, normalized_label AS NormalizedLabel,
                     source AS Source, is_internal_transfer AS IsInternalTransfer,
                     is_excluded_from_budget AS IsExcludedFromBudget, notes AS Notes,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM transaction_entry";

        private readonly SQLiteConnection _connection;

        public TransactionRepository(SQLiteConnection connection) => _connection = connection;

        public Transaction Insert(Transaction transaction)
        {
            _connection.Execute(
                @"INSERT INTO transaction_entry
                    (account_id, category_id, counterparty_id, recurring_operation_id, amount_minor, currency,
                     operation_date, original_label, normalized_label, source, is_internal_transfer,
                     is_excluded_from_budget, notes, created_at, updated_at)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                transaction.AccountId,
                transaction.CategoryId,
                transaction.CounterpartyId,
                transaction.RecurringOperationId,
                transaction.AmountMinor,
                transaction.Currency,
                transaction.OperationDate.ToStorageString(),
                transaction.OriginalLabel,
                transaction.NormalizedLabel,
                transaction.Source.ToStorageString(),
                transaction.IsInternalTransfer,
                transaction.IsExcludedFromBudget,
                transaction.Notes,
                transaction.CreatedAt.ToStorageString(),
                transaction.UpdatedAt.ToStorageString());

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            transaction.AssignId(id);
            return transaction;
        }

        public void Update(Transaction transaction)
        {
            _connection.Execute(
                @"UPDATE transaction_entry SET
                    category_id = ?, counterparty_id = ?, recurring_operation_id = ?, normalized_label = ?,
                    is_internal_transfer = ?, is_excluded_from_budget = ?, notes = ?, updated_at = ?
                  WHERE id = ?",
                transaction.CategoryId,
                transaction.CounterpartyId,
                transaction.RecurringOperationId,
                transaction.NormalizedLabel,
                transaction.IsInternalTransfer,
                transaction.IsExcludedFromBudget,
                transaction.Notes,
                transaction.UpdatedAt.ToStorageString(),
                transaction.Id);
        }

        public void Delete(int id) => _connection.Execute("DELETE FROM transaction_entry WHERE id = ?", id);

        public Transaction? FindById(int id)
        {
            var row = _connection.Query<TransactionRow>($"{SelectColumns} WHERE id = ?", id).FirstOrDefault();
            return row is null ? null : Map(row);
        }

        public IReadOnlyList<Transaction> ListAll() =>
            _connection.Query<TransactionRow>($"{SelectColumns} ORDER BY operation_date DESC, id DESC").Select(Map).ToList();

        public IReadOnlyList<Transaction> ListForAccount(int accountId) =>
            _connection.Query<TransactionRow>(
                    $"{SelectColumns} WHERE account_id = ? ORDER BY operation_date DESC", accountId)
                .Select(Map)
                .ToList();

        /// <summary>Transactions on or after a date — the forecast engine's "réel postérieur au
        /// solde de référence". See docs/06-Moteur_de_prevision.md §12.</summary>
        public IReadOnlyList<Transaction> ListForAccountSince(int accountId, DateTime sinceDate) =>
            _connection.Query<TransactionRow>(
                    $"{SelectColumns} WHERE account_id = ? AND operation_date >= ? ORDER BY operation_date",
                    accountId, sinceDate.ToStorageString())
                .Select(Map)
                .ToList();

        /// <summary>The category last used for this exact normalized label — the whole
        /// mechanism behind "catégorisation assistée". See docs/03-Modele_de_donnees.md §6bis.</summary>
        public int? FindLastCategoryForLabel(string normalizedLabel) => _connection.ExecuteScalar<int?>(
            "SELECT category_id FROM transaction_entry WHERE normalized_label = ? ORDER BY operation_date DESC LIMIT 1",
            normalizedLabel);

        /// <summary>Transactions across every account within a period — a budget is user-wide,
        /// not account-scoped. See docs/03-Modele_de_donnees.md §18.</summary>
        public IReadOnlyList<Transaction> ListForPeriod(DateTime from, DateTime to) =>
            _connection.Query<TransactionRow>(
                    $"{SelectColumns} WHERE operation_date BETWEEN ? AND ? ORDER BY operation_date",
                    from.ToStorageString(), to.ToStorageString())
                .Select(Map)
                .ToList();

        private static Transaction Map(TransactionRow row) => Transaction.FromStorage(
            row.Id,
            row.AccountId,
            row.CategoryId,
            row.CounterpartyId,
            row.RecurringOperationId,
            row.AmountMinor,
            row.Currency,
            StorageFormat.ParseDate(row.OperationDate),
            row.OriginalLabel,
            row.NormalizedLabel,
            StorageFormat.ParseTransactionSource(row.Source),
            row.IsInternalTransfer,
            row.IsExcludedFromBudget,
            row.Notes,
            StorageFormat.ParseTimestamp(row.CreatedAt),
            StorageFormat.ParseTimestamp(row.UpdatedAt));
    }
}
