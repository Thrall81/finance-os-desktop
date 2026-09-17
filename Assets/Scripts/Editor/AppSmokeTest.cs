using System;
using System.IO;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;
using UnityEditor;
using UnityEngine;

namespace FinanceOS.EditorTools
{
    /// <summary>
    /// Manual, batchmode-runnable proof that the App layer correctly orchestrates Domain, Data
    /// and Forecast together through AppContainer — end to end, the way UI eventually will.
    /// Ahead of the proper EditMode test suite.
    /// </summary>
    internal static class AppSmokeTest
    {
        [MenuItem("Finance OS/Run App Smoke Test")]
        public static void Run()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"financeos-app-smoke-{Guid.NewGuid():N}.db");

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
                Debug.LogWarning($"[AppSmokeTest] Could not delete temp file (harmless): {path}");
            }
        }

        private static void RunAgainst(string tempPath)
        {
            using var app = new AppContainer(tempPath);

            app.Categories.SeedDefaultCategoriesIfEmpty();
            Check(app.Categories.ListActive().Count == 10, "ten default categories seeded");
            app.Categories.SeedDefaultCategoriesIfEmpty();
            Check(app.Categories.ListActive().Count == 10, "seeding twice does not duplicate");

            var housing = app.Categories.ListActive().First(c => c.Name == "Logement");
            var current = app.Accounts.CreateAccount("Compte courant", AccountType.Current, "EUR", 150_000);
            var savings = app.Accounts.CreateAccount("Livret A", AccountType.Savings, "EUR", 300_000);
            app.Accounts.RecordOfficialBalance(current.Id, 150_000, new DateTime(2026, 9, 1));
            app.Accounts.RecordOfficialBalance(savings.Id, 300_000, new DateTime(2026, 9, 1));

            // Dated in August, strictly before the account's balance reference date of Sept 1st,
            // so it seeds the label-memory suggestion below without also landing in the
            // September forecast window and double-counting rent alongside the occurrence
            // confirmed later.
            var firstRent = app.Transactions.CreateManual(
                current.Id, -65_000, "EUR", new DateTime(2026, 8, 5), "PRLV SEPA PROPRIETAIRE", categoryId: housing.Id);
            app.Transactions.AssignCategory(firstRent.Id, housing.Id, normalizedLabel: "Loyer");

            var suggested = app.Transactions.SuggestCategoryForLabel("Loyer");
            Check(suggested == housing.Id, "label-memory suggests the previously used category");

            var rent = app.RecurringOperations.Create(
                "Loyer", RecurringOperationType.Expense, 65_000, RecurringFrequency.Monthly,
                new DateTime(2026, 1, 5), sourceAccountId: current.Id, categoryId: housing.Id, expectedDayOfMonth: 5);

            var generated = app.RecurringOperations.GenerateOccurrences(rent.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30));
            Check(generated.Select(o => o.ExpectedDate).SequenceEqual(new[]
            {
                new DateTime(2026, 9, 5), new DateTime(2026, 10, 5), new DateTime(2026, 11, 5),
            }), "three monthly occurrences generated");

            var regenerated = app.RecurringOperations.GenerateOccurrences(rent.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30));
            Check(regenerated.Count == 0, "regenerating the same window is a no-op");

            var due = app.ForecastOccurrences.ListDueForVerification(new DateTime(2026, 9, 13));
            Check(due.Count == 1 && due[0].ExpectedDate == new DateTime(2026, 9, 5), "September occurrence is due for verification");

            var confirmedTransaction = app.ForecastOccurrences.ConfirmAsTransaction(due[0].Id, new DateTime(2026, 9, 5), -65_000);
            Check(confirmedTransaction.Id != 0, "confirming the occurrence created a real transaction");
            Check(app.ForecastOccurrences.ListDueForVerification(new DateTime(2026, 9, 13)).Count == 0,
                "confirmed occurrence leaves the verification queue");

            var transferLink = app.InternalTransfers.CreateTransfer(
                current.Id, savings.Id, 20_000, "EUR", new DateTime(2026, 9, 10), "Virement épargne");
            Check(transferLink.Status == TransferLinkStatus.Confirmed, "internal transfer is confirmed immediately");

            var forecast = app.Forecast.GetForecast(current.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), new DateTime(2026, 9, 13));
            // 150 000 - 65 000 (September rent, now a real confirmed transaction) - 20 000 (transfer out).
            // October and November's occurrences fall outside this window and are not counted.
            Check(forecast.ClosingBalanceMinor == 65_000, "forecast reflects the real rent payment and the transfer out (150000-65000-20000)");

            var simulated = app.Forecast.Simulate(
                current.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), new DateTime(2026, 9, 13),
                "Achat ordinateur", new DateTime(2026, 9, 20), -85_000);
            Check(simulated.ClosingBalanceMinor == -20_000, "simulation reflects the extra purchase without persisting it");
            Check(app.Forecast.GetForecast(current.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), new DateTime(2026, 9, 13))
                .ClosingBalanceMinor == 65_000, "simulation left no trace in the real forecast");
            Check(simulated.ClosingBalanceMinor - forecast.ClosingBalanceMinor == -85_000,
                "the avant/après delta (ForecastsController's simulation result) equals exactly the simulated amount");

            var budget = app.Budget.GetOrCreate(2026, 9);
            app.Budget.UpsertAllocation(budget.Id, housing.Id, 70_000);
            var summary = app.Budget.GetSummary(budget.Id).Single();
            Check(summary.PlannedAmountMinor == 70_000, "budget planned amount");
            Check(summary.ActualAmountMinor == 65_000, "budget actual amount excludes the internal transfer, includes the rent");
            Check(summary.RemainingAmountMinor == 5_000, "budget remaining = 70000 - 65000");
            Check(summary.AllocationId != 0, "the allocation's own id is carried through the summary, not just the category id");

            var revenus = app.Categories.ListActive().First(c => c.Name == "Revenus");
            var epargne = app.Categories.ListActive().First(c => c.Name == "Épargne");
            app.Transactions.CreateManual(current.Id, 220_000, "EUR", new DateTime(2026, 9, 2), "Salaire", categoryId: revenus.Id);
            app.Transactions.CreateManual(current.Id, -30_000, "EUR", new DateTime(2026, 9, 3), "Virement Livret A", categoryId: epargne.Id);

            var overview = app.Budget.GetOverview(2026, 9);
            Check(overview.IncomeMinor == 220_000, "income sums transactions categorized as Income within the month");
            Check(overview.SavingsMinor == 30_000, "savings sums transactions categorized as Savings within the month (magnitude)");
            // ADR-147: reste à vivre = net recurring operations for the month (here, just the
            // confirmed September rent occurrence, -65 000 — the only recurring-operation-sourced
            // occurrence whose ExpectedDate falls in September) minus the month's total budgeted
            // amount across every category regardless of type (here, just housing's 70 000).
            Check(overview.RemainingToLiveMinor == -135_000,
                "reste à vivre = net recurring operations (-65 000, the confirmed rent occurrence) minus total budgeted (70 000)");
            Check(Math.Abs(overview.SavingsRatePercent - 13.6363636) < 0.01, "savings rate = savings / income * 100");

            // ADR-147: with no budget created for October, reste à vivre still returns a real
            // figure (not a placeholder) — just the net recurring operations alone, nothing to
            // deduct. October's rent occurrence (generated earlier, still 'planned') is the only
            // recurring-operation-sourced occurrence in that month.
            var overviewWithoutBudget = app.Budget.GetOverview(2026, 10);
            Check(overviewWithoutBudget.RemainingToLiveMinor == -65_000,
                "reste à vivre falls back to net recurring operations alone when no budget exists for the month");

            // The "Virement Livret A" transaction just above (line ~116) is exactly the real-world
            // case this feature targets: one leg of a transfer entered manually, with nothing ever
            // linking it to its counterpart. Adding that counterpart here (after every assertion
            // above that depends on current/savings totals, so it can't disturb them) gives
            // TransferDetectionService a genuine unlinked pair to find.
            var transferCounterpart = app.Transactions.CreateManual(
                savings.Id, 30_000, "EUR", new DateTime(2026, 9, 4), "Dépôt depuis compte courant");

            var candidates = app.TransferDetection.DetectCandidates();
            Check(candidates.Count == 1, "exactly one unlinked transfer-shaped pair is detected");
            Check(candidates[0].Outgoing.AccountId == current.Id && candidates[0].Incoming.AccountId == savings.Id,
                "the negative leg resolves as outgoing and the positive leg as incoming, regardless of creation order");
            Check(app.TransferDetection.DetectCandidates().Count == 1, "detection is a pure read — re-running it neither consumes nor duplicates the candidate");

            var farApartOutgoing = app.Transactions.CreateManual(current.Id, -8_000, "EUR", new DateTime(2026, 9, 6), "Retrait");
            var farApartIncoming = app.Transactions.CreateManual(savings.Id, 8_000, "EUR", new DateTime(2026, 9, 11), "Dépôt tardif");
            Check(app.TransferDetection.DetectCandidates().Count == 1, "a pair more than three days apart is not suggested");
            app.Transactions.DeleteManual(farApartOutgoing.Id);
            app.Transactions.DeleteManual(farApartIncoming.Id);

            var sameAccountOutgoing = app.Transactions.CreateManual(current.Id, -4_000, "EUR", new DateTime(2026, 9, 6), "Dépense A");
            var sameAccountIncoming = app.Transactions.CreateManual(current.Id, 4_000, "EUR", new DateTime(2026, 9, 6), "Remboursement");
            Check(app.TransferDetection.DetectCandidates().Count == 1, "a same-account opposite pair is not suggested — it isn't a transfer between accounts");
            app.Transactions.DeleteManual(sameAccountOutgoing.Id);
            app.Transactions.DeleteManual(sameAccountIncoming.Id);

            var rejectedLink = app.TransferDetection.Reject(candidates[0].Outgoing.Id, candidates[0].Incoming.Id);
            Check(rejectedLink.Status == TransferLinkStatus.Rejected, "rejecting a suggestion persists it as Rejected");
            Check(app.TransferDetection.DetectCandidates().Count == 0, "a rejected pair is never suggested again");
            Check(app.Transactions.FindById(transferCounterpart.Id)!.IsInternalTransfer == false,
                "rejecting a suggestion leaves the transactions themselves untouched");

            var thirdOutgoing = app.Transactions.CreateManual(current.Id, -5_000, "EUR", new DateTime(2026, 9, 6), "Retrait");
            var thirdIncoming = app.Transactions.CreateManual(savings.Id, 5_000, "EUR", new DateTime(2026, 9, 7), "Dépôt");
            Check(app.TransferDetection.DetectCandidates().Count == 1, "a new, distinct pair is detected independently of the earlier rejected one");

            var confirmedLink = app.TransferDetection.Confirm(thirdOutgoing.Id, thirdIncoming.Id);
            Check(confirmedLink.Status == TransferLinkStatus.Confirmed, "confirming a suggestion persists it as Confirmed");
            Check(app.Transactions.FindById(thirdOutgoing.Id)!.IsInternalTransfer, "confirming marks the outgoing leg as an internal transfer");
            Check(app.Transactions.FindById(thirdIncoming.Id)!.IsInternalTransfer, "confirming marks the incoming leg as an internal transfer");
            Check(app.TransferDetection.DetectCandidates().Count == 0, "a confirmed pair no longer appears as a candidate, same as InternalTransfers.CreateTransfer's pair never did");

            // A one-off occurrence's whole point is having no RecurringOperationId — dated in
            // December, well outside every date this file's later MarkStaleAsMissed lookups
            // filter by exact ExpectedDate, so it can't be mistaken for the October/November rent.
            var oneOffOccurrence = app.ForecastOccurrences.Create(
                current.Id, "Prime exceptionnelle", new DateTime(2026, 12, 15), 50_000, categoryId: revenus.Id, counterpartyId: null);
            Check(oneOffOccurrence.RecurringOperationId is null, "a one-off occurrence has no recurring operation behind it");
            Check(oneOffOccurrence.CategoryId == revenus.Id, "category is carried through Create");
            var decemberOccurrences = app.ForecastOccurrences.ListForAccount(current.Id, new DateTime(2026, 12, 1), new DateTime(2026, 12, 31));
            Check(decemberOccurrences.Count == 1 && decemberOccurrences[0].Id == oneOffOccurrence.Id, "the one-off occurrence appears when listing its account for its period");

            // A dedicated recurring operation, not `rent` above — UpdateOperation deletes and
            // regenerates this operation's own pending occurrences, which would otherwise
            // entangle with the October/November missed-occurrence assertions already made
            // against `rent` earlier in this file.
            var wrongAccountOperation = app.RecurringOperations.Create(
                "Test compte erroné", RecurringOperationType.Expense, 1_000, RecurringFrequency.Monthly,
                new DateTime(2026, 9, 1), sourceAccountId: savings.Id, expectedDayOfMonth: 1);
            app.RecurringOperations.GenerateOccurrences(wrongAccountOperation.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30));

            var onWrongAccount = app.ForecastOccurrences.ListForAccount(savings.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30))
                .Where(o => o.RecurringOperationId == wrongAccountOperation.Id).ToList();
            Check(onWrongAccount.Count == 3, "three monthly occurrences generated on the (wrong) account first");

            app.RecurringOperations.UpdateOperation(
                wrongAccountOperation.Id, RecurringOperationType.Expense, current.Id, null,
                1_000, RecurringFrequency.Monthly, wrongAccountOperation.StartDate, 1, categoryId: null, counterpartyId: null);
            Check(app.RecurringOperations.FindById(wrongAccountOperation.Id)!.SourceAccountId == current.Id,
                "the corrected account is persisted on the recurring operation itself");

            var stillOnWrongAccount = app.ForecastOccurrences.ListForAccount(savings.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30))
                .Where(o => o.RecurringOperationId == wrongAccountOperation.Id).ToList();
            Check(stillOnWrongAccount.Count == 0, "UpdateOperation wipes the old pending occurrences, not just the operation's own fields");

            app.RecurringOperations.GenerateOccurrences(wrongAccountOperation.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30));
            var regeneratedOnRightAccount = app.ForecastOccurrences.ListForAccount(current.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30))
                .Where(o => o.RecurringOperationId == wrongAccountOperation.Id).ToList();
            Check(regeneratedOnRightAccount.Count == 3, "regenerating after the fix produces fresh occurrences on the corrected account");

            var invalidAccountChangeBlocked = false;
            try
            {
                app.RecurringOperations.UpdateOperation(
                    wrongAccountOperation.Id, RecurringOperationType.Expense, null, null,
                    1_000, RecurringFrequency.Monthly, wrongAccountOperation.StartDate, 1, categoryId: null, counterpartyId: null);
            }
            catch (ArgumentException)
            {
                invalidAccountChangeBlocked = true;
            }

            Check(invalidAccountChangeBlocked, "an expense still requires a source account after the change — same validation as creation");

            // Category, counterparty and frequency are editable too (ADR-140) — each is baked
            // into an already-generated occurrence, so changing any of them must also wipe and
            // regenerate pending occurrences, same as the account correction above.
            var testCounterparty = app.Counterparties.FindOrCreateByName("Bailleur Test");
            app.RecurringOperations.UpdateOperation(
                wrongAccountOperation.Id, RecurringOperationType.Expense, current.Id, null,
                1_000, RecurringFrequency.Quarterly, wrongAccountOperation.StartDate, 1, categoryId: housing.Id, counterpartyId: testCounterparty.Id);

            var afterScheduleChange = app.RecurringOperations.FindById(wrongAccountOperation.Id)!;
            Check(afterScheduleChange.Frequency == RecurringFrequency.Quarterly, "frequency change is persisted");
            Check(afterScheduleChange.CategoryId == housing.Id, "category change is persisted");
            Check(afterScheduleChange.CounterpartyId == testCounterparty.Id, "counterparty change is persisted");

            var stillOnOldSchedule = app.ForecastOccurrences.ListForAccount(current.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30))
                .Where(o => o.RecurringOperationId == wrongAccountOperation.Id).ToList();
            Check(stillOnOldSchedule.Count == 0, "changing the schedule/category/counterparty wipes pending occurrences too, not just accounts");

            var quarterlyOccurrences = app.RecurringOperations.GenerateOccurrences(wrongAccountOperation.Id, new DateTime(2026, 9, 1), new DateTime(2026, 11, 30));
            Check(quarterlyOccurrences.Count == 1, "quarterly frequency produces only the September occurrence over the same Sept-Nov window that produced three monthly ones");
            Check(quarterlyOccurrences[0].CategoryId == housing.Id, "a regenerated occurrence carries the operation's new category, not the old (null) one");
            Check(quarterlyOccurrences[0].CounterpartyId == testCounterparty.Id, "a regenerated occurrence carries the operation's new counterparty");

            // Type change: Dépense → Revenu flips the stored amount sign and moves the account
            // from source to destination — exercises the repository's own `type` column, which
            // never appeared in its UPDATE statement before this pass (a latent bug, harmless
            // only because Type used to be immutable — same shape as ADR-137's original
            // account-columns gap). Bundled with a real start-date change too, since start date
            // was immutable until this same pass — exercises the repository's `start_date` column,
            // which never appeared in the UPDATE statement either (identical latent-bug shape).
            var newStartDate = new DateTime(2026, 10, 15);
            app.RecurringOperations.UpdateOperation(
                wrongAccountOperation.Id, RecurringOperationType.Income, null, current.Id,
                1_000, RecurringFrequency.Quarterly, newStartDate, 1, categoryId: housing.Id, counterpartyId: testCounterparty.Id);

            var afterTypeChange = app.RecurringOperations.FindById(wrongAccountOperation.Id)!;
            Check(afterTypeChange.Type == RecurringOperationType.Income, "type change is actually persisted by the repository, not silently dropped");
            Check(afterTypeChange.ExpectedAmountMinor == 1_000, "an income's expected amount is stored positive — the sign flipped by the type change");
            Check(afterTypeChange.DestinationAccountId == current.Id, "the account moved from source to destination to match the new type");
            Check(afterTypeChange.StartDate == newStartDate, "start date change is actually persisted by the repository, not silently dropped");

            var settings = app.Settings.Get();
            Check(settings.ForecastHorizonDays == 90, "default forecast horizon");
            app.Settings.UpdateForecastHorizon(60);
            Check(app.Settings.Get().ForecastHorizonDays == 60, "forecast horizon persisted through the service");

            Check(settings.Theme == AppTheme.Light, "theme defaults to light");
            app.Settings.UpdateTheme(AppTheme.Dark);
            Check(app.Settings.Get().Theme == AppTheme.Dark, "theme persisted through the service");

            // October's and November's rent occurrences (generated earlier, never confirmed) are
            // still "planned" at this point in the test — a good, untouched pair to exercise the
            // missed-threshold transition without disturbing any assertion above.
            app.ForecastOccurrences.MarkStaleAsMissed(new DateTime(2026, 11, 1), 15);

            var octoberOccurrence = app.ForecastOccurrences
                .ListForAccount(current.Id, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))
                .First(o => o.ExpectedDate == new DateTime(2026, 10, 5));
            Check(octoberOccurrence.Status == ForecastOccurrenceStatus.Missed, "October's unconfirmed occurrence transitions to Missed past the threshold");

            var novemberOccurrence = app.ForecastOccurrences
                .ListForAccount(current.Id, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))
                .First(o => o.ExpectedDate == new DateTime(2026, 11, 5));
            Check(novemberOccurrence.Status == ForecastOccurrenceStatus.Planned, "November's occurrence is not yet past the threshold");

            app.ForecastOccurrences.MarkStaleAsMissed(new DateTime(2026, 11, 1), 15);
            var afterSecondRun = app.ForecastOccurrences
                .ListForAccount(current.Id, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))
                .First(o => o.ExpectedDate == new DateTime(2026, 10, 5));
            Check(afterSecondRun.Status == ForecastOccurrenceStatus.Missed, "running MarkStaleAsMissed again is idempotent");

            Check(app.ForecastOccurrences.ListDueForVerification(new DateTime(2026, 11, 1)).Any(o => o.Id == octoberOccurrence.Id),
                "a missed occurrence still appears in the verification queue rather than disappearing");

            // Balance history on `savings`, not `current` — nothing above depends on savings's
            // balance, so this can freely move its forecast anchor without disturbing any
            // already-asserted forecast/budget figure computed against `current`.
            app.Accounts.RecordOfficialBalance(savings.Id, 305_000, new DateTime(2026, 9, 10));
            var afterSecondObservation = app.Accounts.FindById(savings.Id)!;
            Check(afterSecondObservation.OfficialBalanceMinor == 305_000 && afterSecondObservation.OfficialBalanceDate == new DateTime(2026, 9, 10),
                "a later observation moves the account's forecast anchor forward");

            app.Accounts.RecordOfficialBalance(savings.Id, 299_000, new DateTime(2026, 9, 5));
            var afterBackfill = app.Accounts.FindById(savings.Id)!;
            Check(afterBackfill.OfficialBalanceMinor == 305_000 && afterBackfill.OfficialBalanceDate == new DateTime(2026, 9, 10),
                "a backfilled, earlier observation does not pull the forecast anchor backward");

            var balanceHistory = app.Accounts.ListBalanceHistory(savings.Id);
            Check(balanceHistory.Count == 3, "every recorded observation is kept as history, including the backfilled one");
            Check(balanceHistory[0].BalanceDate == new DateTime(2026, 9, 10), "history is ordered most-recent-first");

            var futureBalanceRejected = false;
            try
            {
                app.Accounts.RecordOfficialBalance(savings.Id, 300_000, new DateTime(2026, 12, 31));
            }
            catch (ArgumentException)
            {
                futureBalanceRejected = true;
            }

            Check(futureBalanceRejected, "recording a future-dated official balance is rejected");
            Check(app.Accounts.ListBalanceHistory(savings.Id).Count == 3, "a rejected future balance is not recorded as history either");

            var backupPath = app.Backup.CreateBackup(new DateTimeOffset(2026, 9, 13, 10, 0, 0, TimeSpan.Zero));
            Check(File.Exists(backupPath), "backup creates a real file");
            Check(new FileInfo(backupPath).Length == new FileInfo(tempPath).Length, "backup is a byte-for-byte copy of the live database file");
            Directory.Delete(Path.GetDirectoryName(backupPath)!, recursive: true);

            Debug.Log($"[AppSmokeTest] OK — every App-layer service round-trips correctly through AppContainer. File: {tempPath}");
        }

        private static void Check(bool condition, string what)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"App smoke test assertion failed: {what}.");
            }
        }
    }
}
