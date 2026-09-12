using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using FinanceOS.Forecast;
using UnityEditor;
using UnityEngine;

namespace FinanceOS.EditorTools
{
    /// <summary>
    /// Manual, batchmode-runnable proof that the forecast engine behaves correctly — pure
    /// in-memory Domain objects, no SQLite involved, exactly the isolation
    /// docs/06-Moteur_de_prevision.md §11 asks for. Ahead of the proper EditMode test suite.
    /// </summary>
    internal static class ForecastSmokeTest
    {
        [MenuItem("Finance OS/Run Forecast Smoke Test")]
        public static void Run()
        {
            CheckSimpleBalance();
            CheckIncomeAndExpenseSamePeriod();
            CheckMatchedOccurrenceNotDoubleCounted();
            CheckIdempotentGeneration();
            CheckManualAdjustmentSurvivesRegeneration();
            CheckEndOfFebruaryClamping();
            CheckLowestBalance();
            CheckTransferNeutralAcrossAccounts();
            CheckSimulationDoesNotMutateInputs();

            Debug.Log("[ForecastSmokeTest] OK — all forecast engine checks passed.");
        }

        private static void CheckSimpleBalance()
        {
            var account = MakeAccount("Compte courant", 100_000, new DateTime(2026, 9, 1));
            var occurrences = new[]
            {
                new ForecastOccurrence(account.Id, "Loyer", new DateTime(2026, 9, 10), -30_000),
            };

            var result = ForecastCalculator.Calculate(
                new ForecastRequest(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)),
                account, Array.Empty<Transaction>(), occurrences, new DateTime(2026, 9, 5));

            Check(result.ClosingBalanceMinor == 70_000, "simple balance: 100 000 - 30 000 = 70 000");
        }

        private static void CheckIncomeAndExpenseSamePeriod()
        {
            var account = MakeAccount("Compte courant", 50_000, new DateTime(2026, 9, 1));
            var occurrences = new[]
            {
                new ForecastOccurrence(account.Id, "Salaire", new DateTime(2026, 9, 28), 210_000),
                new ForecastOccurrence(account.Id, "Loyer", new DateTime(2026, 9, 5), -65_000),
            };

            var result = ForecastCalculator.Calculate(
                new ForecastRequest(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)),
                account, Array.Empty<Transaction>(), occurrences, new DateTime(2026, 9, 1));

            Check(result.ClosingBalanceMinor == 195_000, "income + expense same month");
            Check(result.ExpectedIncomeMinor == 210_000, "expected income excludes nothing here");
            Check(result.ExpectedExpensesMinor == -65_000, "expected expenses");
        }

        private static void CheckMatchedOccurrenceNotDoubleCounted()
        {
            var account = MakeAccount("Compte courant", 100_000, new DateTime(2026, 9, 1));

            var occurrence = new ForecastOccurrence(account.Id, "EDF", new DateTime(2026, 9, 8), -9_736);
            var transaction = new Transaction(
                account.Id, -9_736, "EUR", new DateTime(2026, 9, 9), "PRLV SEPA EDF", TransactionSource.Manual);
            occurrence.MarkAsMatched(transaction.Id);

            var result = ForecastCalculator.Calculate(
                new ForecastRequest(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)),
                account, new[] { transaction }, new[] { occurrence }, new DateTime(2026, 9, 20));

            Check(result.ClosingBalanceMinor == 90_264, "matched occurrence must not apply twice (100000 - 9736)");
        }

        private static void CheckIdempotentGeneration()
        {
            var rent = new RecurringOperation(
                "Loyer", RecurringOperationType.Expense, 65_000, RecurringFrequency.Monthly,
                new DateTime(2026, 1, 5), sourceAccountId: 1, expectedDayOfMonth: 5);

            var horizonStart = new DateTime(2026, 8, 1);
            var horizonEnd = new DateTime(2026, 10, 31);

            var firstRun = ForecastOccurrenceGenerator.GenerateMissingOccurrences(
                rent, horizonStart, horizonEnd, Array.Empty<DateTime>());
            Check(firstRun.Select(o => o.ExpectedDate).SequenceEqual(new[]
            {
                new DateTime(2026, 8, 5), new DateTime(2026, 9, 5), new DateTime(2026, 10, 5),
            }), "first generation produces exactly the three monthly dates");

            var existingDates = firstRun.Select(o => o.ExpectedDate).ToList();
            var secondRun = ForecastOccurrenceGenerator.GenerateMissingOccurrences(rent, horizonStart, horizonEnd, existingDates);
            Check(secondRun.Count == 0, "regenerating with the same existing dates produces nothing new");
        }

        private static void CheckManualAdjustmentSurvivesRegeneration()
        {
            var rent = new RecurringOperation(
                "Loyer", RecurringOperationType.Expense, 65_000, RecurringFrequency.Monthly,
                new DateTime(2026, 1, 5), sourceAccountId: 1, expectedDayOfMonth: 5);

            var adjusted = new ForecastOccurrence(1, "Loyer", new DateTime(2026, 9, 5), -70_000, recurringOperationId: rent.Id);
            adjusted.AdjustManually(expectedAmountMinor: -70_000);

            var regenerated = ForecastOccurrenceGenerator.GenerateMissingOccurrences(
                rent, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), new[] { adjusted.ExpectedDate });

            Check(regenerated.Count == 0, "a date already present (manually adjusted or not) is never regenerated");
        }

        private static void CheckEndOfFebruaryClamping()
        {
            var operation = new RecurringOperation(
                "Assurance", RecurringOperationType.Expense, 5_000, RecurringFrequency.Monthly,
                new DateTime(2026, 1, 31), sourceAccountId: 1, expectedDayOfMonth: 31);

            var occurrences = ForecastOccurrenceGenerator.GenerateMissingOccurrences(
                operation, new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), Array.Empty<DateTime>());

            Check(occurrences.Count == 1 && occurrences[0].ExpectedDate == new DateTime(2026, 2, 28),
                "day 31 clamps to February 28th in a non-leap year");
        }

        private static void CheckLowestBalance()
        {
            var account = MakeAccount("Compte courant", 150_000, new DateTime(2026, 9, 1));
            var occurrences = new[]
            {
                new ForecastOccurrence(account.Id, "Loyer", new DateTime(2026, 9, 5), -65_000),
                new ForecastOccurrence(account.Id, "Assurance annuelle", new DateTime(2026, 9, 27), -85_360),
                new ForecastOccurrence(account.Id, "Salaire", new DateTime(2026, 9, 29), 210_000),
            };

            var result = ForecastCalculator.Calculate(
                new ForecastRequest(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)),
                account, Array.Empty<Transaction>(), occurrences, new DateTime(2026, 9, 13));

            Check(result.LowestBalanceMinor == -360, "lowest point is 150000 - 65000 - 85360 = -360");
            Check(result.LowestBalanceDate == new DateTime(2026, 9, 27), "lowest point falls on the insurance date");
        }

        private static void CheckTransferNeutralAcrossAccounts()
        {
            var current = MakeAccount("Compte courant", 100_000, new DateTime(2026, 9, 1));
            var savings = MakeAccount("Livret A", 300_000, new DateTime(2026, 9, 1));

            var transferOut = new ForecastOccurrence(
                current.Id, "Virement épargne", new DateTime(2026, 9, 10), -20_000, destinationAccountId: savings.Id);

            var currentResult = ForecastCalculator.Calculate(
                new ForecastRequest(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)),
                current, Array.Empty<Transaction>(), new[] { transferOut }, new DateTime(2026, 9, 1));
            var savingsResult = ForecastCalculator.Calculate(
                new ForecastRequest(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)),
                savings, Array.Empty<Transaction>(), new[] { transferOut }, new DateTime(2026, 9, 1));

            Check(currentResult.ClosingBalanceMinor == 80_000, "source account loses the transfer amount");
            Check(savingsResult.ClosingBalanceMinor == 320_000, "destination account gains the mirrored amount");
            Check(currentResult.ExpectedExpensesMinor == 0 && savingsResult.ExpectedIncomeMinor == 0,
                "a transfer counts as neither income nor expense on either side");

            var combinedPatrimony = (currentResult.OpeningBalanceMinor + savingsResult.OpeningBalanceMinor)
                == (currentResult.ClosingBalanceMinor + savingsResult.ClosingBalanceMinor);
            Check(combinedPatrimony, "consolidated patrimony is unchanged by an internal transfer");
        }

        private static void CheckSimulationDoesNotMutateInputs()
        {
            var account = MakeAccount("Compte courant", 100_000, new DateTime(2026, 9, 1));
            IReadOnlyList<ForecastOccurrence> baseline = new[]
            {
                new ForecastOccurrence(account.Id, "Loyer", new DateTime(2026, 9, 5), -65_000),
            };

            var baselineResult = ForecastCalculator.Calculate(
                new ForecastRequest(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)),
                account, Array.Empty<Transaction>(), baseline, new DateTime(2026, 9, 1));

            // A simulation is nothing more than an extra in-memory occurrence passed alongside
            // the real ones — never persisted, never mutating `baseline`.
            var simulated = baseline.Append(new ForecastOccurrence(account.Id, "Ordinateur", new DateTime(2026, 9, 20), -85_000)).ToList();
            var simulatedResult = ForecastCalculator.Calculate(
                new ForecastRequest(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)),
                account, Array.Empty<Transaction>(), simulated, new DateTime(2026, 9, 1));

            Check(baseline.Count == 1, "the original occurrence list is untouched by simulation");
            Check(baselineResult.ClosingBalanceMinor == 35_000, "baseline unaffected by the simulated purchase");
            Check(simulatedResult.ClosingBalanceMinor == -50_000, "simulated result reflects the extra purchase");
        }

        private static int _nextAccountId = 1;

        private static Account MakeAccount(string name, long officialBalanceMinor, DateTime officialBalanceDate)
        {
            var account = new Account(name, AccountType.Current, "EUR", officialBalanceMinor);
            account.AssignId(_nextAccountId++);
            account.RecordOfficialBalance(officialBalanceMinor, officialBalanceDate);
            return account;
        }

        private static void Check(bool condition, string what)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Forecast smoke test assertion failed: {what}.");
            }
        }
    }
}
