using System;
using System.IO;
using FinanceOS.Data;
using FinanceOS.Domain;
using SQLite;
using UnityEditor;
using UnityEngine;

namespace FinanceOS.EditorTools
{
    /// <summary>
    /// Manual, batchmode-runnable proof that the vendored SQLite stack and the repositories
    /// built on top of it actually work end to end — ahead of the proper EditMode test suite,
    /// which is deferred until Assets/Tests gets its own asmdef.
    /// </summary>
    internal static class DatabaseSmokeTest
    {
        [MenuItem("Finance OS/Run Database Smoke Test")]
        public static void Run()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"financeos-smoke-{Guid.NewGuid():N}.db");

            // Deliberately not wrapped in try/finally: a cleanup failure must never mask a
            // real exception from RunAgainst. Leftover temp files are harmless and rare.
            RunAgainst(tempPath);
            TryDeleteQuietly(tempPath);
        }

        private static void TryDeleteQuietly(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                Debug.LogWarning($"[DatabaseSmokeTest] Could not delete temp file (harmless): {path}");
            }
        }

        private static void RunAgainst(string tempPath)
        {
            using var database = new AppDatabase(tempPath);
            var connection = database.Connection;

            CheckAccountRepository(connection, out var accountId);
            CheckCategoryRepository(connection, out var categoryId);
            CheckTransactionAndRecurringAndOccurrence(connection, accountId, categoryId);
            CheckBudget(connection, categoryId);
            CheckAppSettings(connection);

            var schemaVersion = connection.ExecuteScalar<int>("PRAGMA user_version");
            Debug.Log($"[DatabaseSmokeTest] OK — schema version {schemaVersion}, all repositories round-trip correctly. File: {tempPath}");
        }

        private static void CheckAccountRepository(SQLiteConnection connection, out int accountId)
        {
            var accounts = new AccountRepository(connection);

            var account = new Account(
                name: "Compte courant",
                type: AccountType.Current,
                currency: "EUR",
                initialBalanceMinor: 150_000);
            account.RecordOfficialBalance(154_230, new DateTime(2026, 9, 12));
            accounts.Insert(account);
            Check(account.Id != 0, "account insert assigns id");

            var reloaded = accounts.FindById(account.Id) ?? throw new InvalidOperationException("account not found after insert");
            Check(reloaded.Name == "Compte courant", "account name");
            Check(reloaded.Type == AccountType.Current, "account type");
            Check(reloaded.OfficialBalanceMinor == 154_230, "account official balance");
            Check(reloaded.OfficialBalanceDate == new DateTime(2026, 9, 12), "account official balance date");
            Check(reloaded.LiquidityPolicy == LiquidityPolicy.Immediate, "account default liquidity policy");

            reloaded.Rename("Compte courant principal");
            reloaded.Archive();
            accounts.Update(reloaded);

            Check(accounts.FindById(reloaded.Id)!.IsArchived, "account archived flag persisted");
            Check(accounts.ListActive().Count == 0, "active accounts excludes archived");
            Check(accounts.ListAll().Count == 1, "full account list still includes archived");

            accountId = reloaded.Id;
        }

        private static void CheckCategoryRepository(SQLiteConnection connection, out int categoryId)
        {
            var categories = new CategoryRepository(connection);

            var category = new Category("Alimentation", CategoryType.Expense);
            categories.Insert(category);
            Check(category.Id != 0, "category insert assigns id");

            var sub = new Category("Courses", CategoryType.Expense, parentId: category.Id);
            categories.Insert(sub);

            var reloadedSub = categories.FindById(sub.Id)!;
            Check(reloadedSub.ParentId == category.Id, "sub-category parent id");
            Check(categories.ListActive().Count == 2, "two active categories");

            categoryId = category.Id;
        }

        private static void CheckTransactionAndRecurringAndOccurrence(SQLiteConnection connection, int accountId, int categoryId)
        {
            var transactions = new TransactionRepository(connection);
            var recurringOperations = new RecurringOperationRepository(connection);
            var occurrences = new ForecastOccurrenceRepository(connection);

            var rent = new RecurringOperation(
                name: "Loyer",
                type: RecurringOperationType.Expense,
                expectedAmountMinor: -65_000,
                frequency: RecurringFrequency.Monthly,
                startDate: new DateTime(2026, 1, 5),
                sourceAccountId: accountId,
                categoryId: categoryId,
                expectedDayOfMonth: 5);
            recurringOperations.Insert(rent);
            Check(recurringOperations.FindById(rent.Id)!.ExpectedAmountMinor == -65_000, "recurring operation amount");

            var pastDue = new ForecastOccurrence(
                accountId: accountId,
                label: "Loyer",
                expectedDate: new DateTime(2026, 9, 5),
                expectedAmountMinor: -65_000,
                recurringOperationId: rent.Id,
                categoryId: categoryId);
            occurrences.Insert(pastDue);

            var future = new ForecastOccurrence(
                accountId: accountId,
                label: "Loyer",
                expectedDate: new DateTime(2026, 10, 5),
                expectedAmountMinor: -65_000,
                recurringOperationId: rent.Id,
                categoryId: categoryId);
            occurrences.Insert(future);

            var due = occurrences.ListDueForVerification(new DateTime(2026, 9, 13));
            Check(due.Count == 1 && due[0].Id == pastDue.Id, "verification queue contains only the past-due occurrence");

            var transaction = new Transaction(
                accountId: accountId,
                amountMinor: -65_000,
                currency: "EUR",
                operationDate: new DateTime(2026, 9, 5),
                originalLabel: "PRLV SEPA PROPRIETAIRE",
                source: TransactionSource.Manual,
                categoryId: categoryId);
            transactions.Insert(transaction);

            due[0].MarkAsMatched(transaction.Id);
            occurrences.Update(due[0]);

            var afterMatch = occurrences.FindById(pastDue.Id)!;
            Check(afterMatch.Status == ForecastOccurrenceStatus.Matched, "occurrence marked matched");
            Check(afterMatch.MatchedTransactionId == transaction.Id, "occurrence records matched transaction id");

            var stillDue = occurrences.ListDueForVerification(new DateTime(2026, 9, 13));
            Check(stillDue.Count == 0, "matched occurrence leaves the verification queue");

            var sinceStart = transactions.ListForAccountSince(accountId, new DateTime(2026, 9, 1));
            Check(sinceStart.Count == 1 && sinceStart[0].OriginalLabel == "PRLV SEPA PROPRIETAIRE", "transaction original label preserved");
        }

        private static void CheckBudget(SQLiteConnection connection, int categoryId)
        {
            var budgets = new BudgetRepository(connection);
            var allocations = new BudgetAllocationRepository(connection);

            var budget = new Budget(2026, 9);
            budgets.Insert(budget);
            Check(budgets.FindByYearMonth(2026, 9)!.Id == budget.Id, "budget lookup by year/month");

            var allocation = new BudgetAllocation(budget.Id, categoryId, 35_000);
            allocations.Insert(allocation);

            allocation.UpdatePlannedAmount(40_000);
            allocations.Update(allocation);

            var reloaded = allocations.FindByBudgetAndCategory(budget.Id, categoryId)!;
            Check(reloaded.PlannedAmountMinor == 40_000, "budget allocation updated amount");
            Check(allocations.ListForBudget(budget.Id).Count == 1, "one allocation for budget");
        }

        private static void CheckAppSettings(SQLiteConnection connection)
        {
            var settingsRepository = new AppSettingsRepository(connection);

            var defaults = settingsRepository.Load();
            Check(defaults.DefaultCurrency == "EUR", "default currency default");
            Check(defaults.ForecastHorizonDays == 90, "forecast horizon default");
            Check(defaults.MissedThresholdDays == 15, "missed threshold default");
            Check(defaults.Theme == AppTheme.Light, "theme defaults to light");

            defaults.UpdateForecastHorizon(60);
            defaults.SetDefaultCurrentAccount(1);
            defaults.UpdateMissedThreshold(21);
            defaults.SetTheme(AppTheme.Dark);
            settingsRepository.Save(defaults);

            var reloaded = settingsRepository.Load();
            Check(reloaded.ForecastHorizonDays == 60, "forecast horizon persisted");
            Check(reloaded.DefaultCurrentAccountId == 1, "default current account persisted");
            Check(reloaded.MissedThresholdDays == 21, "missed threshold persisted");
            Check(reloaded.Theme == AppTheme.Dark, "theme persisted");
        }

        private static void Check(bool condition, string what)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Smoke test assertion failed: {what}.");
            }
        }
    }
}
