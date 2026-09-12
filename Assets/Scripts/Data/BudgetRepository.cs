using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class BudgetRow
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>Maps <see cref="Budget"/> to and from the `budget` table.</summary>
    public sealed class BudgetRepository
    {
        private const string SelectColumns = "SELECT id AS Id, year AS Year, month AS Month, status AS Status FROM budget";

        private readonly SQLiteConnection _connection;

        public BudgetRepository(SQLiteConnection connection) => _connection = connection;

        public Budget Insert(Budget budget)
        {
            _connection.Execute(
                "INSERT INTO budget (year, month, status) VALUES (?, ?, ?)",
                budget.Year, budget.Month, budget.Status.ToStorageString());

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            budget.AssignId(id);
            return budget;
        }

        public void Update(Budget budget) =>
            _connection.Execute("UPDATE budget SET status = ? WHERE id = ?", budget.Status.ToStorageString(), budget.Id);

        public Budget? FindById(int id)
        {
            var row = _connection.Query<BudgetRow>($"{SelectColumns} WHERE id = ?", id).FirstOrDefault();
            return row is null ? null : Map(row);
        }

        public Budget? FindByYearMonth(int year, int month)
        {
            var row = _connection.Query<BudgetRow>($"{SelectColumns} WHERE year = ? AND month = ?", year, month).FirstOrDefault();
            return row is null ? null : Map(row);
        }

        public IReadOnlyList<Budget> ListAll() =>
            _connection.Query<BudgetRow>($"{SelectColumns} ORDER BY year DESC, month DESC").Select(Map).ToList();

        private static Budget Map(BudgetRow row) => Budget.FromStorage(row.Id, row.Year, row.Month, StorageFormat.ParseBudgetStatus(row.Status));
    }
}
