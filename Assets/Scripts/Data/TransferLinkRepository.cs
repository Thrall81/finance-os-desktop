using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class TransferLinkRow
    {
        public int Id { get; set; }
        public int OutgoingTransactionId { get; set; }
        public int IncomingTransactionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>Maps <see cref="TransferLink"/> to and from the `transfer_link` table.</summary>
    public sealed class TransferLinkRepository
    {
        private const string SelectColumns =
            @"SELECT id AS Id, outgoing_transaction_id AS OutgoingTransactionId,
                     incoming_transaction_id AS IncomingTransactionId, status AS Status, created_at AS CreatedAt
              FROM transfer_link";

        private readonly SQLiteConnection _connection;

        public TransferLinkRepository(SQLiteConnection connection) => _connection = connection;

        public TransferLink Insert(TransferLink link)
        {
            _connection.Execute(
                "INSERT INTO transfer_link (outgoing_transaction_id, incoming_transaction_id, status, created_at) VALUES (?, ?, ?, ?)",
                link.OutgoingTransactionId,
                link.IncomingTransactionId,
                link.Status.ToStorageString(),
                link.CreatedAt.ToStorageString());

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            link.AssignId(id);
            return link;
        }

        public void Update(TransferLink link) =>
            _connection.Execute("UPDATE transfer_link SET status = ? WHERE id = ?", link.Status.ToStorageString(), link.Id);

        public IReadOnlyList<TransferLink> ListAll() =>
            _connection.Query<TransferLinkRow>(SelectColumns).Select(Map).ToList();

        public TransferLink? FindById(int id)
        {
            var row = _connection.Query<TransferLinkRow>($"{SelectColumns} WHERE id = ?", id).FirstOrDefault();
            return row is null ? null : Map(row);
        }

        /// <summary>Whether a transaction already participates in a transfer link, on either side.</summary>
        public TransferLink? FindByTransactionId(int transactionId)
        {
            var row = _connection.Query<TransferLinkRow>(
                    $"{SelectColumns} WHERE outgoing_transaction_id = ? OR incoming_transaction_id = ?",
                    transactionId, transactionId)
                .FirstOrDefault();
            return row is null ? null : Map(row);
        }

        private static TransferLink Map(TransferLinkRow row) => TransferLink.FromStorage(
            row.Id, row.OutgoingTransactionId, row.IncomingTransactionId,
            StorageFormat.ParseTransferLinkStatus(row.Status), StorageFormat.ParseTimestamp(row.CreatedAt));
    }
}
