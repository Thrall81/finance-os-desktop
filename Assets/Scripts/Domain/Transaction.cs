using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// A real, constated financial movement — as opposed to a <see cref="ForecastOccurrence"/>,
    /// which is only expected. See docs/03-Modele_de_donnees.md §9 and docs/06-Moteur_de_prevision.md §2.
    /// </summary>
    public sealed class Transaction
    {
        public int Id { get; private set; }
        public int AccountId { get; }
        public int? CategoryId { get; private set; }
        public int? CounterpartyId { get; private set; }
        public int? RecurringOperationId { get; private set; }
        public long AmountMinor { get; }
        public string Currency { get; }
        public DateTime OperationDate { get; private set; }
        public string OriginalLabel { get; }
        public string? NormalizedLabel { get; private set; }
        public TransactionSource Source { get; }
        public bool IsInternalTransfer { get; private set; }
        public bool IsExcludedFromBudget { get; private set; }
        public string? Notes { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }

        public Transaction(
            int accountId,
            long amountMinor,
            string currency,
            DateTime operationDate,
            string originalLabel,
            TransactionSource source,
            int? categoryId = null,
            int? counterpartyId = null,
            int? recurringOperationId = null,
            DateTime? today = null,
            DateTimeOffset? now = null)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("A transaction must belong to a persisted account.", nameof(accountId));
            }

            if (amountMinor == 0)
            {
                throw new ArgumentException("Transaction amount cannot be zero.", nameof(amountMinor));
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Transaction currency is required.", nameof(currency));
            }

            if (string.IsNullOrWhiteSpace(originalLabel))
            {
                throw new ArgumentException("Original label is required.", nameof(originalLabel));
            }

            // Manual entries represent something that already happened — a future movement
            // belongs to ForecastOccurrence instead. Imports are exempt: the bank's own date
            // is authoritative. See docs/01-Perimetre.md §2.6.
            if (source == TransactionSource.Manual && operationDate.Date > (today ?? DateTime.Now).Date)
            {
                throw new ArgumentException(
                    "A manually entered transaction cannot be dated in the future.", nameof(operationDate));
            }

            var timestamp = now ?? DateTimeOffset.Now;

            AccountId = accountId;
            AmountMinor = amountMinor;
            Currency = currency;
            OperationDate = operationDate;
            OriginalLabel = originalLabel;
            Source = source;
            CategoryId = categoryId;
            CounterpartyId = counterpartyId;
            RecurringOperationId = recurringOperationId;
            IsInternalTransfer = false;
            IsExcludedFromBudget = false;
            CreatedAt = timestamp;
            UpdatedAt = timestamp;
        }

        private Transaction(
            int id,
            int accountId,
            int? categoryId,
            int? counterpartyId,
            int? recurringOperationId,
            long amountMinor,
            string currency,
            DateTime operationDate,
            string originalLabel,
            string? normalizedLabel,
            TransactionSource source,
            bool isInternalTransfer,
            bool isExcludedFromBudget,
            string? notes,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            Id = id;
            AccountId = accountId;
            CategoryId = categoryId;
            CounterpartyId = counterpartyId;
            RecurringOperationId = recurringOperationId;
            AmountMinor = amountMinor;
            Currency = currency;
            OperationDate = operationDate;
            OriginalLabel = originalLabel;
            NormalizedLabel = normalizedLabel;
            Source = source;
            IsInternalTransfer = isInternalTransfer;
            IsExcludedFromBudget = isExcludedFromBudget;
            Notes = notes;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static Transaction FromStorage(
            int id,
            int accountId,
            int? categoryId,
            int? counterpartyId,
            int? recurringOperationId,
            long amountMinor,
            string currency,
            DateTime operationDate,
            string originalLabel,
            string? normalizedLabel,
            TransactionSource source,
            bool isInternalTransfer,
            bool isExcludedFromBudget,
            string? notes,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt) => new(
                id, accountId, categoryId, counterpartyId, recurringOperationId, amountMinor, currency,
                operationDate, originalLabel, normalizedLabel, source, isInternalTransfer, isExcludedFromBudget,
                notes, createdAt, updatedAt);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Transaction already has an id.");
            }

            Id = id;
        }

        public void AssignCategory(int? categoryId, DateTimeOffset? now = null)
        {
            CategoryId = categoryId;
            Touch(now);
        }

        public void AssignCounterparty(int? counterpartyId, DateTimeOffset? now = null)
        {
            CounterpartyId = counterpartyId;
            Touch(now);
        }

        public void Relabel(string normalizedLabel, DateTimeOffset? now = null)
        {
            NormalizedLabel = normalizedLabel;
            Touch(now);
        }

        public void UpdateNotes(string? notes, DateTimeOffset? now = null)
        {
            Notes = notes;
            Touch(now);
        }

        public void SetExcludedFromBudget(bool excluded, DateTimeOffset? now = null)
        {
            IsExcludedFromBudget = excluded;
            Touch(now);
        }

        public void MarkAsInternalTransfer(DateTimeOffset? now = null)
        {
            IsInternalTransfer = true;
            Touch(now);
        }

        public void UnmarkAsInternalTransfer(DateTimeOffset? now = null)
        {
            IsInternalTransfer = false;
            Touch(now);
        }

        private void Touch(DateTimeOffset? now) => UpdatedAt = now ?? DateTimeOffset.Now;
    }
}
