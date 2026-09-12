using System;
using System.Collections.Generic;
using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>Orchestrates manual transaction entry, correction and the label-memory
    /// categorization suggestion. See docs/01-Perimetre.md §2.4 and §2.6.</summary>
    public sealed class TransactionService
    {
        private readonly TransactionRepository _transactions;

        public TransactionService(TransactionRepository transactions) => _transactions = transactions;

        /// <summary>The category last used for this normalized label, if any — the UI pre-fills
        /// it as a suggestion, never silently. See docs/07-Interface.md §7.</summary>
        public int? SuggestCategoryForLabel(string normalizedLabel) =>
            _transactions.FindLastCategoryForLabel(normalizedLabel);

        public Transaction CreateManual(
            int accountId,
            long amountMinor,
            string currency,
            DateTime operationDate,
            string originalLabel,
            int? categoryId = null,
            int? counterpartyId = null,
            string? notes = null)
        {
            var transaction = new Transaction(
                accountId, amountMinor, currency, operationDate, originalLabel, TransactionSource.Manual,
                categoryId, counterpartyId);

            if (notes is not null)
            {
                transaction.UpdateNotes(notes);
            }

            return _transactions.Insert(transaction);
        }

        public Transaction? FindById(int transactionId) => _transactions.FindById(transactionId);

        public IReadOnlyList<Transaction> ListForAccount(int accountId) => _transactions.ListForAccount(accountId);

        public void AssignCategory(int transactionId, int? categoryId, string? normalizedLabel = null)
        {
            var transaction = RequireTransaction(transactionId);
            transaction.AssignCategory(categoryId);

            if (normalizedLabel is not null)
            {
                transaction.Relabel(normalizedLabel);
            }

            _transactions.Update(transaction);
        }

        public void AssignCounterparty(int transactionId, int? counterpartyId)
        {
            var transaction = RequireTransaction(transactionId);
            transaction.AssignCounterparty(counterpartyId);
            _transactions.Update(transaction);
        }

        public void UpdateNotes(int transactionId, string? notes)
        {
            var transaction = RequireTransaction(transactionId);
            transaction.UpdateNotes(notes);
            _transactions.Update(transaction);
        }

        public void SetExcludedFromBudget(int transactionId, bool excluded)
        {
            var transaction = RequireTransaction(transactionId);
            transaction.SetExcludedFromBudget(excluded);
            _transactions.Update(transaction);
        }

        /// <summary>Only ever a manual entry — an imported transaction is not deletable in the
        /// V1 (no import feature exists yet either). See docs/01-Perimetre.md §2.4.</summary>
        public void DeleteManual(int transactionId)
        {
            var transaction = RequireTransaction(transactionId);
            if (transaction.Source != TransactionSource.Manual)
            {
                throw new InvalidOperationException("Only a manually entered transaction can be deleted.");
            }

            _transactions.Delete(transactionId);
        }

        private Transaction RequireTransaction(int transactionId) =>
            _transactions.FindById(transactionId) ?? throw new InvalidOperationException($"Transaction #{transactionId} not found.");
    }
}
