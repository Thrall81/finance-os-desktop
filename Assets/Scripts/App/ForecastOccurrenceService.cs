using System;
using System.Collections.Generic;
using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>
    /// Orchestrates the verification queue: an occurrence past its date waits here until the
    /// user resolves it. <see cref="ConfirmAsTransaction"/> is the one-click "C'est arrivé"
    /// action — it creates the real transaction(s) and matches the occurrence in one step.
    /// See docs/01-Perimetre.md §2.7 and docs/07-Interface.md §6.
    /// </summary>
    public sealed class ForecastOccurrenceService
    {
        private readonly ForecastOccurrenceRepository _occurrences;
        private readonly TransactionRepository _transactions;
        private readonly TransferLinkRepository _transferLinks;
        private readonly AccountRepository _accounts;

        public ForecastOccurrenceService(
            ForecastOccurrenceRepository occurrences,
            TransactionRepository transactions,
            TransferLinkRepository transferLinks,
            AccountRepository accounts)
        {
            _occurrences = occurrences;
            _transactions = transactions;
            _transferLinks = transferLinks;
            _accounts = accounts;
        }

        public ForecastOccurrence Create(
            int accountId,
            string label,
            DateTime expectedDate,
            long expectedAmountMinor,
            int? destinationAccountId = null,
            int? categoryId = null,
            int? counterpartyId = null,
            string? notes = null)
        {
            var occurrence = new ForecastOccurrence(
                accountId, label, expectedDate, expectedAmountMinor,
                destinationAccountId: destinationAccountId, categoryId: categoryId, counterpartyId: counterpartyId, notes: notes);

            return _occurrences.Insert(occurrence);
        }

        public IReadOnlyList<ForecastOccurrence> ListDueForVerification(DateTime today) =>
            _occurrences.ListDueForVerification(today);

        /// <summary>Creates the real transaction(s) matching the confirmed date/amount and marks
        /// the occurrence resolved — a transfer-shaped occurrence produces both legs and a
        /// confirmed <see cref="TransferLink"/>, exactly like a manually entered transfer.</summary>
        public Transaction ConfirmAsTransaction(int occurrenceId, DateTime actualDate, long actualAmountMinor)
        {
            var occurrence = RequireOccurrence(occurrenceId);
            var account = _accounts.FindById(occurrence.AccountId)
                ?? throw new InvalidOperationException($"Account #{occurrence.AccountId} not found.");

            var transaction = new Transaction(
                occurrence.AccountId, actualAmountMinor, account.Currency, actualDate, occurrence.Label,
                TransactionSource.Manual, occurrence.CategoryId, occurrence.CounterpartyId);
            _transactions.Insert(transaction);

            if (occurrence.DestinationAccountId is { } destinationAccountId)
            {
                var destinationAccount = _accounts.FindById(destinationAccountId)
                    ?? throw new InvalidOperationException($"Account #{destinationAccountId} not found.");

                var mirrored = new Transaction(
                    destinationAccountId, -actualAmountMinor, destinationAccount.Currency, actualDate, occurrence.Label,
                    TransactionSource.Manual);
                transaction.MarkAsInternalTransfer();
                mirrored.MarkAsInternalTransfer();
                _transactions.Insert(mirrored);

                var link = new TransferLink(transaction.Id, mirrored.Id);
                link.Confirm();
                _transferLinks.Insert(link);
            }

            occurrence.MarkAsMatched(transaction.Id);
            _occurrences.Update(occurrence);

            return transaction;
        }

        public void Cancel(int occurrenceId)
        {
            var occurrence = RequireOccurrence(occurrenceId);
            occurrence.Cancel();
            _occurrences.Update(occurrence);
        }

        public void Ignore(int occurrenceId)
        {
            var occurrence = RequireOccurrence(occurrenceId);
            occurrence.Ignore();
            _occurrences.Update(occurrence);
        }

        public void Restore(int occurrenceId)
        {
            var occurrence = RequireOccurrence(occurrenceId);
            occurrence.Restore();
            _occurrences.Update(occurrence);
        }

        private ForecastOccurrence RequireOccurrence(int occurrenceId) =>
            _occurrences.FindById(occurrenceId) ?? throw new InvalidOperationException($"Occurrence #{occurrenceId} not found.");
    }
}
