using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// Links the two transactions that together represent one internal transfer.
    /// See docs/03-Modele_de_donnees.md §9 (transfer_link) and docs/06-Moteur_de_prevision.md §19.
    /// </summary>
    public sealed class TransferLink
    {
        public int Id { get; private set; }
        public int OutgoingTransactionId { get; }
        public int IncomingTransactionId { get; }
        public TransferLinkStatus Status { get; private set; }
        public DateTimeOffset CreatedAt { get; }

        public TransferLink(int outgoingTransactionId, int incomingTransactionId, DateTimeOffset? now = null)
        {
            if (outgoingTransactionId <= 0 || incomingTransactionId <= 0)
            {
                throw new ArgumentException("A transfer link needs two persisted transactions.");
            }

            if (outgoingTransactionId == incomingTransactionId)
            {
                throw new ArgumentException("A transaction cannot be linked to itself.");
            }

            OutgoingTransactionId = outgoingTransactionId;
            IncomingTransactionId = incomingTransactionId;
            Status = TransferLinkStatus.Suggested;
            CreatedAt = now ?? DateTimeOffset.Now;
        }

        private TransferLink(int id, int outgoingTransactionId, int incomingTransactionId, TransferLinkStatus status, DateTimeOffset createdAt)
        {
            Id = id;
            OutgoingTransactionId = outgoingTransactionId;
            IncomingTransactionId = incomingTransactionId;
            Status = status;
            CreatedAt = createdAt;
        }

        public static TransferLink FromStorage(
            int id, int outgoingTransactionId, int incomingTransactionId, TransferLinkStatus status, DateTimeOffset createdAt)
            => new(id, outgoingTransactionId, incomingTransactionId, status, createdAt);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Transfer link already has an id.");
            }

            Id = id;
        }

        public void Confirm() => Status = TransferLinkStatus.Confirmed;

        public void Reject() => Status = TransferLinkStatus.Rejected;
    }
}
