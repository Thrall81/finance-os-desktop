using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// An expected future movement — from a recurring operation, or standalone for a one-off
    /// planned expense/income. See docs/03-Modele_de_donnees.md §16 and docs/06-Moteur_de_prevision.md §4.4.
    /// </summary>
    public sealed class ForecastOccurrence
    {
        public int Id { get; private set; }
        public int? RecurringOperationId { get; }
        public int AccountId { get; }
        public int? DestinationAccountId { get; }
        public int? CategoryId { get; private set; }
        public int? CounterpartyId { get; private set; }
        public string Label { get; private set; }
        public DateTime ExpectedDate { get; private set; }
        public long ExpectedAmountMinor { get; private set; }
        public ForecastOccurrenceStatus Status { get; private set; }
        public int? MatchedTransactionId { get; private set; }
        public bool IsManuallyAdjusted { get; private set; }
        public string? Notes { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }

        public ForecastOccurrence(
            int accountId,
            string label,
            DateTime expectedDate,
            long expectedAmountMinor,
            int? recurringOperationId = null,
            int? destinationAccountId = null,
            int? categoryId = null,
            int? counterpartyId = null,
            string? notes = null,
            DateTimeOffset? now = null)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("An occurrence must belong to a persisted account.", nameof(accountId));
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Occurrence label is required.", nameof(label));
            }

            var timestamp = now ?? DateTimeOffset.Now;

            AccountId = accountId;
            Label = label;
            ExpectedDate = expectedDate;
            ExpectedAmountMinor = expectedAmountMinor;
            RecurringOperationId = recurringOperationId;
            DestinationAccountId = destinationAccountId;
            CategoryId = categoryId;
            CounterpartyId = counterpartyId;
            Status = ForecastOccurrenceStatus.Planned;
            IsManuallyAdjusted = false;
            Notes = notes;
            CreatedAt = timestamp;
            UpdatedAt = timestamp;
        }

        private ForecastOccurrence(
            int id,
            int? recurringOperationId,
            int accountId,
            int? destinationAccountId,
            int? categoryId,
            int? counterpartyId,
            string label,
            DateTime expectedDate,
            long expectedAmountMinor,
            ForecastOccurrenceStatus status,
            int? matchedTransactionId,
            bool isManuallyAdjusted,
            string? notes,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            Id = id;
            RecurringOperationId = recurringOperationId;
            AccountId = accountId;
            DestinationAccountId = destinationAccountId;
            CategoryId = categoryId;
            CounterpartyId = counterpartyId;
            Label = label;
            ExpectedDate = expectedDate;
            ExpectedAmountMinor = expectedAmountMinor;
            Status = status;
            MatchedTransactionId = matchedTransactionId;
            IsManuallyAdjusted = isManuallyAdjusted;
            Notes = notes;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static ForecastOccurrence FromStorage(
            int id,
            int? recurringOperationId,
            int accountId,
            int? destinationAccountId,
            int? categoryId,
            int? counterpartyId,
            string label,
            DateTime expectedDate,
            long expectedAmountMinor,
            ForecastOccurrenceStatus status,
            int? matchedTransactionId,
            bool isManuallyAdjusted,
            string? notes,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt) => new(
                id, recurringOperationId, accountId, destinationAccountId, categoryId, counterpartyId, label,
                expectedDate, expectedAmountMinor, status, matchedTransactionId, isManuallyAdjusted, notes,
                createdAt, updatedAt);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Occurrence already has an id.");
            }

            Id = id;
        }

        /// <summary>The one-click "C'est arrivé" action from the verification queue.
        /// See docs/07-Interface.md §6.</summary>
        public void MarkAsMatched(int transactionId, DateTimeOffset? now = null)
        {
            EnsurePending();
            Status = ForecastOccurrenceStatus.Matched;
            MatchedTransactionId = transactionId;
            Touch(now);
        }

        public void Cancel(DateTimeOffset? now = null)
        {
            EnsurePending();
            Status = ForecastOccurrenceStatus.Cancelled;
            Touch(now);
        }

        public void Ignore(DateTimeOffset? now = null)
        {
            EnsurePending();
            Status = ForecastOccurrenceStatus.Ignored;
            Touch(now);
        }

        public void MarkAsMissed(DateTimeOffset? now = null)
        {
            EnsurePending();
            Status = ForecastOccurrenceStatus.Missed;
            Touch(now);
        }

        public void Restore(DateTimeOffset? now = null)
        {
            Status = ForecastOccurrenceStatus.Planned;
            MatchedTransactionId = null;
            Touch(now);
        }

        public void AdjustManually(DateTime? expectedDate = null, long? expectedAmountMinor = null, DateTimeOffset? now = null)
        {
            if (expectedDate is { } date)
            {
                ExpectedDate = date;
            }

            if (expectedAmountMinor is { } amount)
            {
                ExpectedAmountMinor = amount;
            }

            IsManuallyAdjusted = true;
            Touch(now);
        }

        private void EnsurePending()
        {
            if (Status is ForecastOccurrenceStatus.Matched or ForecastOccurrenceStatus.Cancelled)
            {
                throw new InvalidOperationException($"Occurrence is already {Status} and cannot transition further.");
            }
        }

        private void Touch(DateTimeOffset? now) => UpdatedAt = now ?? DateTimeOffset.Now;
    }
}
