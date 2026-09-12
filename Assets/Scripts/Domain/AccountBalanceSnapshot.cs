using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// A historical, immutable record of an officially observed account balance.
    /// See docs/03-Modele_de_donnees.md §8.
    /// </summary>
    public sealed class AccountBalanceSnapshot
    {
        public int Id { get; private set; }
        public int AccountId { get; }
        public long BalanceMinor { get; }
        public DateTime BalanceDate { get; }
        public DateTimeOffset CreatedAt { get; }

        public AccountBalanceSnapshot(int accountId, long balanceMinor, DateTime balanceDate, DateTimeOffset? now = null)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("A snapshot must belong to a persisted account.", nameof(accountId));
            }

            AccountId = accountId;
            BalanceMinor = balanceMinor;
            BalanceDate = balanceDate;
            CreatedAt = now ?? DateTimeOffset.Now;
        }

        private AccountBalanceSnapshot(int id, int accountId, long balanceMinor, DateTime balanceDate, DateTimeOffset createdAt)
        {
            Id = id;
            AccountId = accountId;
            BalanceMinor = balanceMinor;
            BalanceDate = balanceDate;
            CreatedAt = createdAt;
        }

        public static AccountBalanceSnapshot FromStorage(
            int id, int accountId, long balanceMinor, DateTime balanceDate, DateTimeOffset createdAt)
            => new(id, accountId, balanceMinor, balanceDate, createdAt);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Snapshot already has an id.");
            }

            Id = id;
        }
    }
}
