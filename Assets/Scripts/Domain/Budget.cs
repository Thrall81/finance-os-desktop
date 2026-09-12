using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// A user's budget for one calendar month. See docs/03-Modele_de_donnees.md §18.
    /// </summary>
    public sealed class Budget
    {
        public int Id { get; private set; }
        public int Year { get; }
        public int Month { get; }
        public BudgetStatus Status { get; private set; }

        public Budget(int year, int month)
        {
            if (month is < 1 or > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
            }

            Year = year;
            Month = month;
            Status = BudgetStatus.Active;
        }

        private Budget(int id, int year, int month, BudgetStatus status)
        {
            Id = id;
            Year = year;
            Month = month;
            Status = status;
        }

        public static Budget FromStorage(int id, int year, int month, BudgetStatus status)
            => new(id, year, month, status);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Budget already has an id.");
            }

            Id = id;
        }

        public void Activate() => Status = BudgetStatus.Active;

        public void Close() => Status = BudgetStatus.Closed;

        public void Reopen() => Status = BudgetStatus.Active;
    }
}
