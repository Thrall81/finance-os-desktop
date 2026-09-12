using System;
using System.Globalization;
using FinanceOS.Domain;

namespace FinanceOS.Data
{
    /// <summary>
    /// Translates between Domain types and the TEXT representations used in SQLite —
    /// lowercase snake_case for enums, ISO-8601 for dates (docs/03-Modele_de_donnees.md §2.2).
    /// The only place in the codebase that knows these string forms.
    /// </summary>
    internal static class StorageFormat
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string TimestampFormat = "yyyy-MM-ddTHH:mm:sszzz";

        public static string ToStorageString(this DateTime date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

        public static DateTime ParseDate(string value) =>
            DateTime.ParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);

        public static string ToStorageString(this DateTimeOffset timestamp) =>
            timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture);

        public static DateTimeOffset ParseTimestamp(string value) =>
            DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        public static string ToStorageString(this AccountType type) => type switch
        {
            AccountType.Current => "current",
            AccountType.Savings => "savings",
            AccountType.Cash => "cash",
            AccountType.Investment => "investment",
            AccountType.Debt => "debt",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        public static AccountType ParseAccountType(string value) => value switch
        {
            "current" => AccountType.Current,
            "savings" => AccountType.Savings,
            "cash" => AccountType.Cash,
            "investment" => AccountType.Investment,
            "debt" => AccountType.Debt,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown account type."),
        };

        public static string ToStorageString(this LiquidityPolicy policy) => policy switch
        {
            LiquidityPolicy.Immediate => "immediate",
            LiquidityPolicy.Reserve => "reserve",
            LiquidityPolicy.Excluded => "excluded",
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null),
        };

        public static LiquidityPolicy ParseLiquidityPolicy(string value) => value switch
        {
            "immediate" => LiquidityPolicy.Immediate,
            "reserve" => LiquidityPolicy.Reserve,
            "excluded" => LiquidityPolicy.Excluded,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown liquidity policy."),
        };

        public static string ToStorageString(this CategoryType type) => type switch
        {
            CategoryType.Expense => "expense",
            CategoryType.Income => "income",
            CategoryType.Savings => "savings",
            CategoryType.Transfer => "transfer",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        public static CategoryType ParseCategoryType(string value) => value switch
        {
            "expense" => CategoryType.Expense,
            "income" => CategoryType.Income,
            "savings" => CategoryType.Savings,
            "transfer" => CategoryType.Transfer,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown category type."),
        };

        public static string ToStorageString(this TransactionSource source) => source switch
        {
            TransactionSource.Manual => "manual",
            TransactionSource.CsvImport => "csv_import",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };

        public static TransactionSource ParseTransactionSource(string value) => value switch
        {
            "manual" => TransactionSource.Manual,
            "csv_import" => TransactionSource.CsvImport,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown transaction source."),
        };

        public static string ToStorageString(this TransferLinkStatus status) => status switch
        {
            TransferLinkStatus.Suggested => "suggested",
            TransferLinkStatus.Confirmed => "confirmed",
            TransferLinkStatus.Rejected => "rejected",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        public static TransferLinkStatus ParseTransferLinkStatus(string value) => value switch
        {
            "suggested" => TransferLinkStatus.Suggested,
            "confirmed" => TransferLinkStatus.Confirmed,
            "rejected" => TransferLinkStatus.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown transfer link status."),
        };

        public static string ToStorageString(this RecurringOperationType type) => type switch
        {
            RecurringOperationType.Income => "income",
            RecurringOperationType.Expense => "expense",
            RecurringOperationType.SavingsTransfer => "savings_transfer",
            RecurringOperationType.InternalTransfer => "internal_transfer",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        public static RecurringOperationType ParseRecurringOperationType(string value) => value switch
        {
            "income" => RecurringOperationType.Income,
            "expense" => RecurringOperationType.Expense,
            "savings_transfer" => RecurringOperationType.SavingsTransfer,
            "internal_transfer" => RecurringOperationType.InternalTransfer,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown recurring operation type."),
        };

        public static string ToStorageString(this RecurringFrequency frequency) => frequency switch
        {
            RecurringFrequency.Weekly => "weekly",
            RecurringFrequency.Monthly => "monthly",
            RecurringFrequency.Bimonthly => "bimonthly",
            RecurringFrequency.Quarterly => "quarterly",
            RecurringFrequency.Semiannual => "semiannual",
            RecurringFrequency.Yearly => "yearly",
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null),
        };

        public static RecurringFrequency ParseRecurringFrequency(string value) => value switch
        {
            "weekly" => RecurringFrequency.Weekly,
            "monthly" => RecurringFrequency.Monthly,
            "bimonthly" => RecurringFrequency.Bimonthly,
            "quarterly" => RecurringFrequency.Quarterly,
            "semiannual" => RecurringFrequency.Semiannual,
            "yearly" => RecurringFrequency.Yearly,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown recurring frequency."),
        };

        public static string ToStorageString(this ForecastOccurrenceStatus status) => status switch
        {
            ForecastOccurrenceStatus.Planned => "planned",
            ForecastOccurrenceStatus.Matched => "matched",
            ForecastOccurrenceStatus.Cancelled => "cancelled",
            ForecastOccurrenceStatus.Missed => "missed",
            ForecastOccurrenceStatus.Ignored => "ignored",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        public static ForecastOccurrenceStatus ParseForecastOccurrenceStatus(string value) => value switch
        {
            "planned" => ForecastOccurrenceStatus.Planned,
            "matched" => ForecastOccurrenceStatus.Matched,
            "cancelled" => ForecastOccurrenceStatus.Cancelled,
            "missed" => ForecastOccurrenceStatus.Missed,
            "ignored" => ForecastOccurrenceStatus.Ignored,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown occurrence status."),
        };

        public static string ToStorageString(this BudgetStatus status) => status switch
        {
            BudgetStatus.Draft => "draft",
            BudgetStatus.Active => "active",
            BudgetStatus.Closed => "closed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        public static BudgetStatus ParseBudgetStatus(string value) => value switch
        {
            "draft" => BudgetStatus.Draft,
            "active" => BudgetStatus.Active,
            "closed" => BudgetStatus.Closed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown budget status."),
        };
    }
}
