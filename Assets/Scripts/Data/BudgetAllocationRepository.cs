using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class BudgetAllocationRow
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }
        public int CategoryId { get; set; }
        public long PlannedAmountMinor { get; set; }
    }

    /// <summary>Maps <see cref="BudgetAllocation"/> to and from the `budget_allocation` table.</summary>
    public sealed class BudgetAllocationRepository
    {
        private const string SelectColumns =
            @"SELECT id AS Id, budget_id AS BudgetId, category_id AS CategoryId, planned_amount_minor AS PlannedAmountMinor
              FROM budget_allocation";

        private readonly SQLiteConnection _connection;

        public BudgetAllocationRepository(SQLiteConnection connection) => _connection = connection;

        public BudgetAllocation Insert(BudgetAllocation allocation)
        {
            _connection.Execute(
                "INSERT INTO budget_allocation (budget_id, category_id, planned_amount_minor) VALUES (?, ?, ?)",
                allocation.BudgetId, allocation.CategoryId, allocation.PlannedAmountMinor);

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            allocation.AssignId(id);
            return allocation;
        }

        public void Update(BudgetAllocation allocation) => _connection.Execute(
            "UPDATE budget_allocation SET planned_amount_minor = ? WHERE id = ?",
            allocation.PlannedAmountMinor, allocation.Id);

        public void Delete(int id) => _connection.Execute("DELETE FROM budget_allocation WHERE id = ?", id);

        public BudgetAllocation? FindByBudgetAndCategory(int budgetId, int categoryId)
        {
            var row = _connection.Query<BudgetAllocationRow>(
                    $"{SelectColumns} WHERE budget_id = ? AND category_id = ?", budgetId, categoryId)
                .FirstOrDefault();
            return row is null ? null : Map(row);
        }

        public IReadOnlyList<BudgetAllocation> ListForBudget(int budgetId) =>
            _connection.Query<BudgetAllocationRow>($"{SelectColumns} WHERE budget_id = ?", budgetId).Select(Map).ToList();

        private static BudgetAllocation Map(BudgetAllocationRow row) =>
            BudgetAllocation.FromStorage(row.Id, row.BudgetId, row.CategoryId, row.PlannedAmountMinor);
    }
}
