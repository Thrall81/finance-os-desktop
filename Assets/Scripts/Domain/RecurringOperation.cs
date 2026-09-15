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
        public RecurringOperationType Type { get; private set; }
        public int? SourceAccountId { get; private set; }
        public int? DestinationAccountId { get; private set; }
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

            ValidateAccounts(type, sourceAccountId, destinationAccountId);

            var timestamp = now ?? DateTimeOffset.Now;

            Name = name;
            Type = type;
            SourceAccountId = sourceAccountId;
            DestinationAccountId = destinationAccountId;
            CategoryId = categoryId;
            CounterpartyId = counterpartyId;
            ExpectedAmountMinor = NormalizeAmountSign(type, expectedAmountMinor);
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
            ExpectedAmountMinor = NormalizeAmountSign(Type, expectedAmountMinor);
            Touch(now);
        }

        /// <summary>The caller may pass either sign (a form typically lets the user enter a plain
        /// magnitude) — the type alone decides the stored sign: positive for income, negative for
        /// an expense or a transfer (which always reduces the source account).
        /// See docs/03-Modele_de_donnees.md §2.4.</summary>
        private static long NormalizeAmountSign(RecurringOperationType type, long amountMinor)
        {
            var magnitude = Math.Abs(amountMinor);
            return type == RecurringOperationType.Income ? magnitude : -magnitude;
        }

        /// <summary>Corrects the type and/or account(s) chosen at creation in one atomic,
        /// validated step — e.g. a wrong source account (the original ADR-137 mistake) or a wrong
        /// Dépense/Revenu selection. Validating the final (type, accounts) combination together,
        /// rather than as two separate calls, avoids a transient state where the old accounts
        /// don't satisfy the new type (or vice versa). Also re-normalizes the amount's sign for
        /// the new type, since Dépense/Revenu store opposite signs for the same typed magnitude.
        /// Callers must also clear and regenerate this operation's still-pending occurrences
        /// (RecurringOperationService.UpdateOperation) — the ones already generated have the old
        /// type/accounts/amount-sign baked in and won't update just because these fields did.</summary>
        public void ChangeType(RecurringOperationType type, int? sourceAccountId, int? destinationAccountId, DateTimeOffset? now = null)
        {
            ValidateAccounts(type, sourceAccountId, destinationAccountId);
            Type = type;
            SourceAccountId = sourceAccountId;
            DestinationAccountId = destinationAccountId;
            ExpectedAmountMinor = NormalizeAmountSign(type, ExpectedAmountMinor);
            Touch(now);
        }

        /// <summary>Corrects the frequency and/or day-of-month chosen at creation. Interval value
        /// stays untouched — no screen exposes editing it, same restraint as start date (below).
        /// Callers must also clear and regenerate this operation's still-pending occurrences
        /// (RecurringOperationService.UpdateOperation) — their dates were computed from the old
        /// schedule.</summary>
        public void ChangeSchedule(RecurringFrequency frequency, int? expectedDayOfMonth, DateTimeOffset? now = null)
        {
            Frequency = frequency;
            ExpectedDayOfMonth = expectedDayOfMonth;
            Touch(now);
        }

        /// <summary>Reassigns the category, or clears it (null). Callers must also clear and
        /// regenerate this operation's still-pending occurrences (RecurringOperationService.
        /// UpdateOperation) — each already-generated occurrence carries its own copy of the
        /// category, taken from this operation at generation time.</summary>
        public void AssignCategory(int? categoryId, DateTimeOffset? now = null)
        {
            CategoryId = categoryId;
            Touch(now);
        }

        /// <summary>Reassigns the counterparty (tiers), or clears it (null). Same "already-
        /// generated occurrences carry their own copy" caveat as <see cref="AssignCategory"/>.</summary>
        public void AssignCounterparty(int? counterpartyId, DateTimeOffset? now = null)
        {
            CounterpartyId = counterpartyId;
            Touch(now);
        }

        private static void ValidateAccounts(RecurringOperationType type, int? sourceAccountId, int? destinationAccountId)
        {
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
