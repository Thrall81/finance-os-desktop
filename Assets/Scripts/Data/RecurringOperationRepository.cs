using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class RecurringOperationRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? SourceAccountId { get; set; }
        public int? DestinationAccountId { get; set; }
        public int? CategoryId { get; set; }
        public int? CounterpartyId { get; set; }
        public long ExpectedAmountMinor { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public int IntervalValue { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string? EndDate { get; set; }
        public int? ExpectedDayOfMonth { get; set; }
        public int DateToleranceDays { get; set; }
        public long AmountToleranceMinor { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    /// <summary>Maps <see cref="RecurringOperation"/> to and from the `recurring_operation` table.</summary>
    public sealed class RecurringOperationRepository
    {
        private const string SelectColumns =
            @"SELECT id AS Id, name AS Name, type AS Type, source_account_id AS SourceAccountId,
                     destination_account_id AS DestinationAccountId, category_id AS CategoryId,
                     counterparty_id AS CounterpartyId, expected_amount_minor AS ExpectedAmountMinor,
                     frequency AS Frequency, interval_value AS IntervalValue, start_date AS StartDate,
                     end_date AS EndDate, expected_day_of_month AS ExpectedDayOfMonth,
                     date_tolerance_days AS DateToleranceDays, amount_tolerance_minor AS AmountToleranceMinor,
                     is_active AS IsActive, notes AS Notes, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM recurring_operation";

        private readonly SQLiteConnection _connection;

        public RecurringOperationRepository(SQLiteConnection connection) => _connection = connection;

        public RecurringOperation Insert(RecurringOperation operation)
        {
            _connection.Execute(
                @"INSERT INTO recurring_operation
                    (name, type, source_account_id, destination_account_id, category_id, counterparty_id,
                     expected_amount_minor, frequency, interval_value, start_date, end_date,
                     expected_day_of_month, date_tolerance_days, amount_tolerance_minor, is_active, notes,
                     created_at, updated_at)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                operation.Name,
                operation.Type.ToStorageString(),
                operation.SourceAccountId,
                operation.DestinationAccountId,
                operation.CategoryId,
                operation.CounterpartyId,
                operation.ExpectedAmountMinor,
                operation.Frequency.ToStorageString(),
                operation.IntervalValue,
                operation.StartDate.ToStorageString(),
                operation.EndDate?.ToStorageString(),
                operation.ExpectedDayOfMonth,
                operation.DateToleranceDays,
                operation.AmountToleranceMinor,
                operation.IsActive,
                operation.Notes,
                operation.CreatedAt.ToStorageString(),
                operation.UpdatedAt.ToStorageString());

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            operation.AssignId(id);
            return operation;
        }

        public void Update(RecurringOperation operation)
        {
            _connection.Execute(
                @"UPDATE recurring_operation SET
                    name = ?, category_id = ?, counterparty_id = ?, expected_amount_minor = ?, frequency = ?,
                    interval_value = ?, end_date = ?, expected_day_of_month = ?, date_tolerance_days = ?,
                    amount_tolerance_minor = ?, is_active = ?, notes = ?, updated_at = ?
                  WHERE id = ?",
                operation.Name,
                operation.CategoryId,
                operation.CounterpartyId,
                operation.ExpectedAmountMinor,
                operation.Frequency.ToStorageString(),
                operation.IntervalValue,
                operation.EndDate?.ToStorageString(),
                operation.ExpectedDayOfMonth,
                operation.DateToleranceDays,
                operation.AmountToleranceMinor,
                operation.IsActive,
                operation.Notes,
                operation.UpdatedAt.ToStorageString(),
                operation.Id);
        }

        public RecurringOperation? FindById(int id)
        {
            var row = _connection.Query<RecurringOperationRow>($"{SelectColumns} WHERE id = ?", id).FirstOrDefault();
            return row is null ? null : Map(row);
        }

        public IReadOnlyList<RecurringOperation> ListActive() =>
            _connection.Query<RecurringOperationRow>($"{SelectColumns} WHERE is_active = 1 ORDER BY name").Select(Map).ToList();

        public IReadOnlyList<RecurringOperation> ListAll() =>
            _connection.Query<RecurringOperationRow>($"{SelectColumns} ORDER BY name").Select(Map).ToList();

        private static RecurringOperation Map(RecurringOperationRow row) => RecurringOperation.FromStorage(
            row.Id,
            row.Name,
            StorageFormat.ParseRecurringOperationType(row.Type),
            row.SourceAccountId,
            row.DestinationAccountId,
            row.CategoryId,
            row.CounterpartyId,
            row.ExpectedAmountMinor,
            StorageFormat.ParseRecurringFrequency(row.Frequency),
            row.IntervalValue,
            StorageFormat.ParseDate(row.StartDate),
            row.EndDate is null ? null : StorageFormat.ParseDate(row.EndDate),
            row.ExpectedDayOfMonth,
            row.DateToleranceDays,
            row.AmountToleranceMinor,
            row.IsActive,
            row.Notes,
            StorageFormat.ParseTimestamp(row.CreatedAt),
            StorageFormat.ParseTimestamp(row.UpdatedAt));
    }
}
