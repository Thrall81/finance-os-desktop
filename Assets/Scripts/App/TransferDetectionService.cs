using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>
    /// Finds transaction pairs that look like an internal transfer entered as two separate,
    /// unrelated transactions — rather than through <see cref="InternalTransferService.CreateTransfer"/>,
    /// which links them from the start. Detection only ever proposes; nothing is persisted until
    /// the user explicitly confirms or rejects a candidate. See docs/01-Perimetre.md §2.6.
    /// </summary>
    public sealed class TransferDetectionService
    {
        // "dates proches" (§2.6) rather than "même jour" — a real transfer's two legs can post a
        // day or two apart on each side of the bank, so same-day-only would miss real pairs.
        private const int MaxDateDeltaDays = 3;

        private readonly TransactionRepository _transactions;
        private readonly TransferLinkRepository _transferLinks;

        public TransferDetectionService(TransactionRepository transactions, TransferLinkRepository transferLinks)
        {
            _transactions = transactions;
            _transferLinks = transferLinks;
        }

        /// <summary>Recomputed fresh on every call — nothing about a suggestion is stored until
        /// <see cref="Confirm"/> or <see cref="Reject"/> is called on it.</summary>
        public IReadOnlyList<TransferCandidate> DetectCandidates()
        {
            var linkedTransactionIds = new HashSet<int>();
            foreach (var link in _transferLinks.ListAll())
            {
                linkedTransactionIds.Add(link.OutgoingTransactionId);
                linkedTransactionIds.Add(link.IncomingTransactionId);
            }

            var eligible = _transactions.ListAll()
                .Where(t => !t.IsInternalTransfer && !linkedTransactionIds.Contains(t.Id))
                .OrderBy(t => t.OperationDate)
                .ToList();

            var candidates = new List<TransferCandidate>();
            var claimed = new HashSet<int>();

            for (var i = 0; i < eligible.Count; i++)
            {
                var a = eligible[i];
                if (claimed.Contains(a.Id))
                {
                    continue;
                }

                for (var j = i + 1; j < eligible.Count; j++)
                {
                    var b = eligible[j];
                    if ((b.OperationDate - a.OperationDate).TotalDays > MaxDateDeltaDays)
                    {
                        // eligible is date-sorted, so every later b is at least as far from a.
                        break;
                    }

                    if (claimed.Contains(b.Id) || a.AccountId == b.AccountId
                        || a.Currency != b.Currency || a.AmountMinor != -b.AmountMinor)
                    {
                        continue;
                    }

                    var (outgoing, incoming) = a.AmountMinor < 0 ? (a, b) : (b, a);
                    candidates.Add(new TransferCandidate(outgoing, incoming));
                    claimed.Add(a.Id);
                    claimed.Add(b.Id);
                    break;
                }
            }

            return candidates;
        }

        /// <summary>The user confirmed the pairing: both transactions are marked as an internal
        /// transfer and a <see cref="TransferLink"/> is persisted as Confirmed — same end state as
        /// <see cref="InternalTransferService.CreateTransfer"/>, just reached from two pre-existing
        /// transactions instead of creating new ones.</summary>
        public TransferLink Confirm(int outgoingTransactionId, int incomingTransactionId)
        {
            var outgoing = RequireTransaction(outgoingTransactionId);
            var incoming = RequireTransaction(incomingTransactionId);

            outgoing.MarkAsInternalTransfer();
            incoming.MarkAsInternalTransfer();
            _transactions.Update(outgoing);
            _transactions.Update(incoming);

            var link = new TransferLink(outgoingTransactionId, incomingTransactionId);
            link.Confirm();
            return _transferLinks.Insert(link);
        }

        /// <summary>The user says this pairing is wrong: the transactions are left untouched, but
        /// a Rejected link is persisted so this exact pair is never suggested again.</summary>
        public TransferLink Reject(int outgoingTransactionId, int incomingTransactionId)
        {
            var link = new TransferLink(outgoingTransactionId, incomingTransactionId);
            link.Reject();
            return _transferLinks.Insert(link);
        }

        private Transaction RequireTransaction(int transactionId) =>
            _transactions.FindById(transactionId) ?? throw new InvalidOperationException($"Transaction #{transactionId} not found.");
    }

    /// <summary>One suggested pairing, resolved to which leg is outgoing (negative amount) and
    /// which is incoming (positive amount) regardless of which transaction was created first.</summary>
    public sealed record TransferCandidate(Transaction Outgoing, Transaction Incoming);
}
