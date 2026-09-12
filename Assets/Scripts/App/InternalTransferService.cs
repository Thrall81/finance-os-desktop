using System;
using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>
    /// Creates a real internal transfer as two linked transactions, confirmed immediately since
    /// the user initiated it directly (as opposed to the assisted detection of an already
    /// imported pair). See docs/01-Perimetre.md §2.6 and docs/06-Moteur_de_prevision.md §19.
    /// </summary>
    public sealed class InternalTransferService
    {
        private readonly TransactionRepository _transactions;
        private readonly TransferLinkRepository _transferLinks;

        public InternalTransferService(TransactionRepository transactions, TransferLinkRepository transferLinks)
        {
            _transactions = transactions;
            _transferLinks = transferLinks;
        }

        public TransferLink CreateTransfer(
            int sourceAccountId,
            int destinationAccountId,
            long amountMinor,
            string currency,
            DateTime operationDate,
            string label)
        {
            if (sourceAccountId == destinationAccountId)
            {
                throw new ArgumentException("Source and destination accounts must differ.", nameof(destinationAccountId));
            }

            var magnitude = Math.Abs(amountMinor);

            var outgoing = new Transaction(sourceAccountId, -magnitude, currency, operationDate, label, TransactionSource.Manual);
            var incoming = new Transaction(destinationAccountId, magnitude, currency, operationDate, label, TransactionSource.Manual);

            outgoing.MarkAsInternalTransfer();
            incoming.MarkAsInternalTransfer();

            _transactions.Insert(outgoing);
            _transactions.Insert(incoming);

            var link = new TransferLink(outgoing.Id, incoming.Id);
            link.Confirm();
            return _transferLinks.Insert(link);
        }
    }
}
