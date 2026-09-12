using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class CounterpartyRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Maps <see cref="Counterparty"/> to and from the `counterparty` table.</summary>
    public sealed class CounterpartyRepository
    {
        private const string SelectColumns = "SELECT id AS Id, name AS Name FROM counterparty";

        private readonly SQLiteConnection _connection;

        public CounterpartyRepository(SQLiteConnection connection) => _connection = connection;

        public Counterparty Insert(Counterparty counterparty)
        {
            _connection.Execute("INSERT INTO counterparty (name) VALUES (?)", counterparty.Name);
            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            counterparty.AssignId(id);
            return counterparty;
        }

        public void Update(Counterparty counterparty) =>
            _connection.Execute("UPDATE counterparty SET name = ? WHERE id = ?", counterparty.Name, counterparty.Id);

        public Counterparty? FindById(int id)
        {
            var row = _connection.Query<CounterpartyRow>($"{SelectColumns} WHERE id = ?", id).FirstOrDefault();
            return row is null ? null : Counterparty.FromStorage(row.Id, row.Name);
        }

        public IReadOnlyList<Counterparty> ListAll() =>
            _connection.Query<CounterpartyRow>($"{SelectColumns} ORDER BY name")
                .Select(row => Counterparty.FromStorage(row.Id, row.Name))
                .ToList();
    }
}
