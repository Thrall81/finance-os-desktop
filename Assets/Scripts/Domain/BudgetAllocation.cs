using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// The planned amount for one category within one budget. See docs/03-Modele_de_donnees.md §19.
    /// </summary>
    public sealed class BudgetAllocation
    {
        public int Id { get; private set; }
        public int BudgetId { get; }
        public int CategoryId { get; }
        public long PlannedAmountMinor { get; private set; }

        public BudgetAllocation(int budgetId, int categoryId, long plannedAmountMinor)
        {
            if (budgetId <= 0)
            {
                throw new ArgumentException("An allocation must belong to a persisted budget.", nameof(budgetId));
            }

            if (categoryId <= 0)
            {
                throw new ArgumentException("An allocation must reference a persisted category.", nameof(categoryId));
            }

            if (plannedAmountMinor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(plannedAmountMinor), plannedAmountMinor, "Planned amount cannot be negative.");
            }

            BudgetId = budgetId;
            CategoryId = categoryId;
            PlannedAmountMinor = plannedAmountMinor;
        }

        private BudgetAllocation(int id, int budgetId, int categoryId, long plannedAmountMinor)
        {
            Id = id;
            BudgetId = budgetId;
            CategoryId = categoryId;
            PlannedAmountMinor = plannedAmountMinor;
        }

        public static BudgetAllocation FromStorage(int id, int budgetId, int categoryId, long plannedAmountMinor)
            => new(id, budgetId, categoryId, plannedAmountMinor);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Allocation already has an id.");
            }

            Id = id;
        }

        public void UpdatePlannedAmount(long plannedAmountMinor)
        {
            if (plannedAmountMinor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(plannedAmountMinor), plannedAmountMinor, "Planned amount cannot be negative.");
            }

            PlannedAmountMinor = plannedAmountMinor;
        }
    }
}
