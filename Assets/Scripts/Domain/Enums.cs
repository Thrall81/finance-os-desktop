namespace FinanceOS.Domain
{
    public enum AccountType
    {
        Current,
        Savings,
        Cash,
        Investment,
        Debt,
    }

    public enum LiquidityPolicy
    {
        Immediate,
        Reserve,
        Excluded,
    }

    public enum CategoryType
    {
        Expense,
        Income,
        Savings,
        Transfer,
    }

    public enum TransactionSource
    {
        Manual,
        CsvImport,
    }

    public enum TransferLinkStatus
    {
        Suggested,
        Confirmed,
        Rejected,
    }

    public enum RecurringOperationType
    {
        Income,
        Expense,
        SavingsTransfer,
        InternalTransfer,
    }

    public enum RecurringFrequency
    {
        Weekly,
        Monthly,
        Bimonthly,
        Quarterly,
        Semiannual,
        Yearly,
    }

    public enum ForecastOccurrenceStatus
    {
        Planned,
        Matched,
        Cancelled,
        Missed,
        Ignored,
    }

    public enum BudgetStatus
    {
        Draft,
        Active,
        Closed,
    }

    /// <summary>The whole app's color scheme — chosen manually in Paramètres, never inferred from
    /// the OS. A user-requested addition to Paramètres beyond docs/01-Perimetre.md §2.11's
    /// original list (devise/horizon/seuil/emplacement/export) — see ADR-135.</summary>
    public enum AppTheme
    {
        Light,
        Dark,
    }
}
