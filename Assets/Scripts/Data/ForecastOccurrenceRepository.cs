using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class ForecastOccurrenceRow
    {
        public int Id { get; set; }
        public int? RecurringOperationId { get; set; }
        public int AccountId { get; set; }
        public int? DestinationAccountId { get; set; }
        public int? CategoryId { get; set; }
        public int? CounterpartyId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string ExpectedDate { get; set; } = string.Empty;
        public long ExpectedAmountMinor { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? MatchedTransactionId { get; set; }
        public bool IsManuallyAdjusted { get; set; }
        public string? Notes { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    /// <summary>Maps <see cref="ForecastOccurrence"/> to and from the `forecast_occurrence` table.</summary>
    public sealed class ForecastOccurrenceRepository
    {
        private const string SelectColumns =
            @"SELECT id AS Id, recurring_operation_id AS RecurringOperationId, account_id AS AccountId,
                     destination_account_id AS DestinationAccountId, category_id AS CategoryId,
                     counterparty_id AS CounterpartyId, label AS Label, expected_date AS ExpectedDate,
                     expected_amount_minor AS ExpectedAmountMinor, status AS Status,
                     matched_transaction_id AS MatchedTransactionId, is_manually_adjusted AS IsManuallyAdjusted,
                     notes AS Notes, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM forecast_occurrence";

        private readonly SQLiteConnection _connection;

        public ForecastOccurrenceRepository(SQLiteConnection connection) => _connection = connection;

        public ForecastOccurrence Insert(ForecastOccurrence occurrence)
        {
            _connection.Execute(
                @"INSERT INTO forecast_occurrence
                    (recurring_operation_id, account_id, destination_account_id, category_id, counterparty_id,
                     label, expected_date, expected_amount_minor, status, matched_transaction_id,
                     is_manually_adjusted, notes, created_at, updated_at)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                occurrence.RecurringOperationId,
                occurrence.AccountId,
                occurrence.DestinationAccountId,
                occurrence.CategoryId,
                occurrence.CounterpartyId,
                occurrence.Label,
                occurrence.ExpectedDate.ToStorageString(),
                occurrence.ExpectedAmountMinor,
                occurrence.Status.ToStorageString(),
                occurrence.MatchedTransactionId,
                occurrence.IsManuallyAdjusted,
                occurrence.Notes,
                occurrence.CreatedAt.ToStorageString(),
                occurrence.UpdatedAt.ToStorageString());

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            occurrence.AssignId(id);
            return occurrence;
        }

        public void Update(ForecastOccurrence occurrence)
        {
            _connection.Execute(
                @"UPDATE forecast_occurrence SET
                    category_id = ?, counterparty_id = ?, label = ?, expected_date = ?, expected_amount_minor = ?,
                    status = ?, matched_transaction_id = ?, is_manually_adjusted = ?, notes = ?, updated_at = ?
                  WHERE id = ?",
                occurrence.CategoryId,
                occurrence.CounterpartyId,
                occurrence.Label,
                occurrence.ExpectedDate.ToStorageString(),
                occurrence.ExpectedAmountMinor,
                occurrence.Status.ToStorageString(),
                occurrence.MatchedTransactionId,
                occurrence.IsManuallyAdjusted,
                occurrence.Notes,
                occurrence.UpdatedAt.ToStorageString(),
                occurrence.Id);
        }

        public ForecastOccurrence? FindById(int id)
        {
            var row = _connection.Query<ForecastOccurrenceRow>($"{SelectColumns} WHERE id = ?", id).FirstOrDefault();
            return row is null ? null : Map(row);
        }

        public IReadOnlyList<ForecastOccurrence> ListForAccountSince(int accountId, DateTime sinceDate) =>
            _connection.Query<ForecastOccurrenceRow>(
                    $"{SelectColumns} WHERE account_id = ? AND expected_date >= ? ORDER BY expected_date",
                    accountId, sinceDate.ToStorageString())
                .Select(Map)
                .ToList();

        /// <summary>Occurrences whose date has arrived or passed without being resolved — the
        /// dashboard's "file de vérification". See docs/07-Interface.md §6.</summary>
        public IReadOnlyList<ForecastOccurrence> ListDueForVerification(DateTime today) =>
            _connection.Query<ForecastOccurrenceRow>(
                    $"{SelectColumns} WHERE status = 'planned' AND expected_date <= ? ORDER BY expected_date",
                    today.ToStorageString())
                .Select(Map)
                .ToList();

        /// <summary>Existing occurrences for one recurring operation in a date range — used by the
        /// (future) generator to stay idempotent. See docs/06-Moteur_de_prevision.md §5.</summary>
        public IReadOnlyList<ForecastOccurrence> ListForRecurringOperation(int recurringOperationId, DateTime from, DateTime to) =>
            _connection.Query<ForecastOccurrenceRow>(
                    $"{SelectColumns} WHERE recurring_operation_id = ? AND expected_date BETWEEN ? AND ? ORDER BY expected_date",
                    recurringOperationId, from.ToStorageString(), to.ToStorageString())
                .Select(Map)
                .ToList();

        private static ForecastOccurrence Map(ForecastOccurrenceRow row) => ForecastOccurrence.FromStorage(
            row.Id,
            row.RecurringOperationId,
            row.AccountId,
            row.DestinationAccountId,
            row.CategoryId,
            row.CounterpartyId,
            row.Label,
            StorageFormat.ParseDate(row.ExpectedDate),
            row.ExpectedAmountMinor,
            StorageFormat.ParseForecastOccurrenceStatus(row.Status),
            row.MatchedTransactionId,
            row.IsManuallyAdjusted,
            row.Notes,
            StorageFormat.ParseTimestamp(row.CreatedAt),
            StorageFormat.ParseTimestamp(row.UpdatedAt));
    }
}
