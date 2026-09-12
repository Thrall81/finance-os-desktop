using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// An expected financial movement recurring at a regular frequency.
    /// See docs/03-Modele_de_donnees.md §15.
    /// </summary>
    public sealed class RecurringOperation
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public RecurringOperationType Type { get; }
        public int? SourceAccountId { get; }
        public int? DestinationAccountId { get; }
        public int? CategoryId { get; private set; }
        public int? CounterpartyId { get; private set; }
        public long ExpectedAmountMinor { get; private set; }
        public RecurringFrequency Frequency { get; private set; }
        public int IntervalValue { get; private set; }
        public DateTime StartDate { get; }
        public DateTime? EndDate { get; private set; }
        public int? ExpectedDayOfMonth { get; private set; }
        public int DateToleranceDays { get; private set; }
        public long AmountToleranceMinor { get; private set; }
        public bool IsActive { get; private set; }
        public string? Notes { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }

        public RecurringOperation(
            string name,
            RecurringOperationType type,
            long expectedAmountMinor,
            RecurringFrequency frequency,
            DateTime startDate,
            int? sourceAccountId = null,
            int? destinationAccountId = null,
            int? categoryId = null,
            int? counterpartyId = null,
            int intervalValue = 1,
            DateTime? endDate = null,
            int? expectedDayOfMonth = null,
            int dateToleranceDays = 3,
            long amountToleranceMinor = 0,
            string? notes = null,
            DateTimeOffset? now = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Recurring operation name is required.", nameof(name));
            }

            if (endDate is { } end && end < startDate)
            {
                throw new ArgumentException("End date cannot precede start date.", nameof(endDate));
            }

            switch (type)
            {
                case RecurringOperationType.Expense when sourceAccountId is null:
                    throw new ArgumentException("An expense requires a source account.", nameof(sourceAccountId));
                case RecurringOperationType.Income when destinationAccountId is null:
                    throw new ArgumentException("An income requires a destination account.", nameof(destinationAccountId));
                case RecurringOperationType.SavingsTransfer when sourceAccountId is null || destinationAccountId is null:
                case RecurringOperationType.InternalTransfer when sourceAccountId is null || destinationAccountId is null:
                    throw new ArgumentException("A transfer requires both a source and a destination account.");
            }

            var timestamp = now ?? DateTimeOffset.Now;

            Name = name;
            Type = type;
            SourceAccountId = sourceAccountId;
            DestinationAccountId = destinationAccountId;
            CategoryId = categoryId;
            CounterpartyId = counterpartyId;
            ExpectedAmountMinor = expectedAmountMinor;
            Frequency = frequency;
            IntervalValue = intervalValue;
            StartDate = startDate;
            EndDate = endDate;
            ExpectedDayOfMonth = expectedDayOfMonth;
            DateToleranceDays = dateToleranceDays;
            AmountToleranceMinor = amountToleranceMinor;
            IsActive = true;
            Notes = notes;
            CreatedAt = timestamp;
            UpdatedAt = timestamp;
        }

        private RecurringOperation(
            int id,
            string name,
            RecurringOperationType type,
            int? sourceAccountId,
            int? destinationAccountId,
            int? categoryId,
            int? counterpartyId,
            long expectedAmountMinor,
            RecurringFrequency frequency,
            int intervalValue,
            DateTime startDate,
            DateTime? endDate,
            int? expectedDayOfMonth,
            int dateToleranceDays,
            long amountToleranceMinor,
            bool isActive,
            string? notes,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            Id = id;
            Name = name;
            Type = type;
            SourceAccountId = sourceAccountId;
            DestinationAccountId = destinationAccountId;
            CategoryId = categoryId;
            CounterpartyId = counterpartyId;
            ExpectedAmountMinor = expectedAmountMinor;
            Frequency = frequency;
            IntervalValue = intervalValue;
            StartDate = startDate;
            EndDate = endDate;
            ExpectedDayOfMonth = expectedDayOfMonth;
            DateToleranceDays = dateToleranceDays;
            AmountToleranceMinor = amountToleranceMinor;
            IsActive = isActive;
            Notes = notes;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static RecurringOperation FromStorage(
            int id,
            string name,
            RecurringOperationType type,
            int? sourceAccountId,
            int? destinationAccountId,
            int? categoryId,
            int? counterpartyId,
            long expectedAmountMinor,
            RecurringFrequency frequency,
            int intervalValue,
            DateTime startDate,
            DateTime? endDate,
            int? expectedDayOfMonth,
            int dateToleranceDays,
            long amountToleranceMinor,
            bool isActive,
            string? notes,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt) => new(
                id, name, type, sourceAccountId, destinationAccountId, categoryId, counterpartyId,
                expectedAmountMinor, frequency, intervalValue, startDate, endDate, expectedDayOfMonth,
                dateToleranceDays, amountToleranceMinor, isActive, notes, createdAt, updatedAt);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Recurring operation already has an id.");
            }

            Id = id;
        }

        public void Rename(string name, DateTimeOffset? now = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Recurring operation name is required.", nameof(name));
            }

            Name = name;
            Touch(now);
        }

        public void UpdateExpectedAmount(long expectedAmountMinor, DateTimeOffset? now = null)
        {
            ExpectedAmountMinor = expectedAmountMinor;
            Touch(now);
        }

        public void UpdateTolerances(int dateToleranceDays, long amountToleranceMinor, DateTimeOffset? now = null)
        {
            DateToleranceDays = dateToleranceDays;
            AmountToleranceMinor = amountToleranceMinor;
            Touch(now);
        }

        public void Suspend(DateTimeOffset? now = null)
        {
            IsActive = false;
            Touch(now);
        }

        public void Resume(DateTimeOffset? now = null)
        {
            IsActive = true;
            Touch(now);
        }

        private void Touch(DateTimeOffset? now) => UpdatedAt = now ?? DateTimeOffset.Now;
    }
}
