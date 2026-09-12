using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// A financial account or value holder. See docs/03-Modele_de_donnees.md §5.
    /// </summary>
    public sealed class Account
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string? InstitutionName { get; private set; }
        public AccountType Type { get; private set; }
        public string Currency { get; private set; }
        public long InitialBalanceMinor { get; private set; }
        public long OfficialBalanceMinor { get; private set; }
        public DateTime? OfficialBalanceDate { get; private set; }
        public LiquidityPolicy LiquidityPolicy { get; private set; }
        public bool IncludeInNetWorth { get; private set; }
        public string? Color { get; private set; }
        public bool IsArchived { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }

        public Account(
            string name,
            AccountType type,
            string currency,
            long initialBalanceMinor,
            LiquidityPolicy? liquidityPolicy = null,
            string? institutionName = null,
            string? color = null,
            bool includeInNetWorth = true,
            DateTimeOffset? now = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Account name is required.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Account currency is required.", nameof(currency));
            }

            var timestamp = now ?? DateTimeOffset.Now;

            Name = name;
            InstitutionName = institutionName;
            Type = type;
            Currency = currency;
            InitialBalanceMinor = initialBalanceMinor;
            OfficialBalanceMinor = initialBalanceMinor;
            OfficialBalanceDate = null;
            LiquidityPolicy = liquidityPolicy ?? DefaultLiquidityPolicyFor(type);
            IncludeInNetWorth = includeInNetWorth;
            Color = color;
            IsArchived = false;
            CreatedAt = timestamp;
            UpdatedAt = timestamp;
        }

        private Account(
            int id,
            string name,
            string? institutionName,
            AccountType type,
            string currency,
            long initialBalanceMinor,
            long officialBalanceMinor,
            DateTime? officialBalanceDate,
            LiquidityPolicy liquidityPolicy,
            bool includeInNetWorth,
            string? color,
            bool isArchived,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            Id = id;
            Name = name;
            InstitutionName = institutionName;
            Type = type;
            Currency = currency;
            InitialBalanceMinor = initialBalanceMinor;
            OfficialBalanceMinor = officialBalanceMinor;
            OfficialBalanceDate = officialBalanceDate;
            LiquidityPolicy = liquidityPolicy;
            IncludeInNetWorth = includeInNetWorth;
            Color = color;
            IsArchived = isArchived;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>Rehydrates an account from storage. Repositories only.</summary>
        public static Account FromStorage(
            int id,
            string name,
            string? institutionName,
            AccountType type,
            string currency,
            long initialBalanceMinor,
            long officialBalanceMinor,
            DateTime? officialBalanceDate,
            LiquidityPolicy liquidityPolicy,
            bool includeInNetWorth,
            string? color,
            bool isArchived,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt) => new(
                id, name, institutionName, type, currency, initialBalanceMinor, officialBalanceMinor,
                officialBalanceDate, liquidityPolicy, includeInNetWorth, color, isArchived, createdAt, updatedAt);

        /// <summary>The safe default applied when a type is chosen and no policy is set explicitly.
        /// See docs/01-Perimetre.md §2.2 and docs/07-Interface.md §9.</summary>
        public static LiquidityPolicy DefaultLiquidityPolicyFor(AccountType type) => type switch
        {
            AccountType.Current => LiquidityPolicy.Immediate,
            AccountType.Cash => LiquidityPolicy.Immediate,
            AccountType.Savings => LiquidityPolicy.Reserve,
            AccountType.Investment => LiquidityPolicy.Excluded,
            AccountType.Debt => LiquidityPolicy.Excluded,
            _ => LiquidityPolicy.Excluded,
        };

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Account already has an id.");
            }

            Id = id;
        }

        public void Rename(string name, DateTimeOffset? now = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Account name is required.", nameof(name));
            }

            Name = name;
            Touch(now);
        }

        public void SetLiquidityPolicy(LiquidityPolicy policy, DateTimeOffset? now = null)
        {
            LiquidityPolicy = policy;
            Touch(now);
        }

        public void SetIncludeInNetWorth(bool includeInNetWorth, DateTimeOffset? now = null)
        {
            IncludeInNetWorth = includeInNetWorth;
            Touch(now);
        }

        public void RecordOfficialBalance(long balanceMinor, DateTime balanceDate, DateTimeOffset? now = null)
        {
            OfficialBalanceMinor = balanceMinor;
            OfficialBalanceDate = balanceDate;
            Touch(now);
        }

        public void Archive(DateTimeOffset? now = null)
        {
            IsArchived = true;
            Touch(now);
        }

        public void Restore(DateTimeOffset? now = null)
        {
            IsArchived = false;
            Touch(now);
        }

        private void Touch(DateTimeOffset? now) => UpdatedAt = now ?? DateTimeOffset.Now;
    }
}
