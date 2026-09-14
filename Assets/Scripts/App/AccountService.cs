using System;
using System.Collections.Generic;
using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>Orchestrates account creation, balance recording and lifecycle.
    /// See docs/01-Perimetre.md §2.2.</summary>
    public sealed class AccountService
    {
        private readonly AccountRepository _accounts;
        private readonly AccountBalanceSnapshotRepository _snapshots;

        public AccountService(AccountRepository accounts, AccountBalanceSnapshotRepository snapshots)
        {
            _accounts = accounts;
            _snapshots = snapshots;
        }

        public Account CreateAccount(
            string name,
            AccountType type,
            string currency,
            long initialBalanceMinor,
            string? institutionName = null,
            LiquidityPolicy? liquidityPolicy = null,
            string? color = null)
        {
            var account = new Account(name, type, currency, initialBalanceMinor, liquidityPolicy, institutionName, color);
            return _accounts.Insert(account);
        }

        public Account? FindById(int accountId) => _accounts.FindById(accountId);

        public IReadOnlyList<Account> ListActive() => _accounts.ListActive();

        public IReadOnlyList<Account> ListAll() => _accounts.ListAll();

        public void Rename(int accountId, string name)
        {
            var account = RequireAccount(accountId);
            account.Rename(name);
            _accounts.Update(account);
        }

        public void SetLiquidityPolicy(int accountId, LiquidityPolicy policy)
        {
            var account = RequireAccount(accountId);
            account.SetLiquidityPolicy(policy);
            _accounts.Update(account);
        }

        public void SetIncludeInNetWorth(int accountId, bool includeInNetWorth)
        {
            var account = RequireAccount(accountId);
            account.SetIncludeInNetWorth(includeInNetWorth);
            _accounts.Update(account);
        }

        /// <summary>Records a new officially observed balance as a permanent history entry —
        /// and, only if it is the most recent one known for this account, also moves the
        /// account's own forecast reference point forward (see Account.RecordOfficialBalance).
        /// See docs/03-Modele_de_donnees.md §8 and docs/01-Perimetre.md §2.2.</summary>
        public void RecordOfficialBalance(int accountId, long balanceMinor, DateTime balanceDate)
        {
            var account = RequireAccount(accountId);
            account.RecordOfficialBalance(balanceMinor, balanceDate);
            _snapshots.Insert(new AccountBalanceSnapshot(accountId, balanceMinor, balanceDate));
            _accounts.Update(account);
        }

        /// <summary>Every officially observed balance ever recorded for this account, most
        /// recent first — the "historique" half of docs/01-Perimetre.md §2.2.</summary>
        public IReadOnlyList<AccountBalanceSnapshot> ListBalanceHistory(int accountId) =>
            _snapshots.ListForAccount(accountId);

        public void Archive(int accountId)
        {
            var account = RequireAccount(accountId);
            account.Archive();
            _accounts.Update(account);
        }

        public void Restore(int accountId)
        {
            var account = RequireAccount(accountId);
            account.Restore();
            _accounts.Update(account);
        }

        private Account RequireAccount(int accountId) =>
            _accounts.FindById(accountId) ?? throw new InvalidOperationException($"Account #{accountId} not found.");
    }
}
