using System;
using System.IO;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;
using FinanceOS.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.EditorTools
{
    /// <summary>
    /// Manual, batchmode-runnable proof for the UI layer. Cannot verify visual layout or styling
    /// (no rendering happens in batchmode) — but UXML structure, and the data-binding logic that
    /// turns a DashboardViewModel into actual Label text, need no rendering pass at all and are
    /// fully checked here. See docs/07-Interface.md.
    /// </summary>
    internal static class UISmokeTest
    {
        private const string DashboardUxmlPath = "Assets/UI/UXML/Dashboard.uxml";

        [MenuItem("Finance OS/Run UI Smoke Test")]
        public static void Run()
        {
            CheckMoneyFormat();
            CheckDateFormat();

            var tempPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-{Guid.NewGuid():N}.db");
            RunAgainstDatabase(tempPath);
            TryDeleteQuietly(tempPath);

            Debug.Log("[UISmokeTest] OK — formatting, view model and UXML data-binding all correct.");
        }

        private static void CheckMoneyFormat()
        {
            // MoneyFormat deliberately groups thousands with a non-breaking space (U+00A0);
            // normalize to a plain space here so the comparison isn't hostage to which
            // whitespace variant got typed into this literal — digit grouping is what's under
            // test, not the exact codepoint.
            Check(Normalize(MoneyFormat.Format(154_230)) == "1 542,30 €", "positive amount grouping");
            Check(MoneyFormat.Format(-8_235) == "−82,35 €", "negative amount uses the real minus sign");
            Check(Normalize(MoneyFormat.Format(2_100_00, forceSign: true)) == "+2 100,00 €", "forced sign on a positive amount");
            Check(MoneyFormat.Format(0) == "0,00 €", "zero amount");
            Check(MoneyFormat.Format(500) == "5,00 €", "sub-thousand amount has no separator");
        }

        private static string Normalize(string value) => value.Replace(" ", " ");

        private static void CheckDateFormat()
        {
            Check(DateFormat.Short(new DateTime(2026, 8, 27)) == "27 août", "short date");
            Check(DateFormat.Long(new DateTime(2026, 8, 27)) == "27 août 2026", "long date");
            Check(DateFormat.RelativeToToday(new DateTime(2026, 9, 8), new DateTime(2026, 9, 13)) == "il y a 5 jours", "relative past date");
            Check(DateFormat.RelativeToToday(new DateTime(2026, 9, 13), new DateTime(2026, 9, 13)) == "aujourd'hui", "relative today");
        }

        private static void RunAgainstDatabase(string tempPath)
        {
            using var app = new AppContainer(tempPath);

            app.Categories.SeedDefaultCategoriesIfEmpty();
            var housing = app.Categories.ListActive().First(c => c.Name == "Logement");
            var account = app.Accounts.CreateAccount("Compte courant", AccountType.Current, "EUR", 174_860);
            app.Accounts.RecordOfficialBalance(account.Id, 174_860, new DateTime(2026, 9, 13));

            var rent = app.RecurringOperations.Create(
                "Loyer", RecurringOperationType.Expense, 65_000, RecurringFrequency.Monthly,
                new DateTime(2026, 9, 5), sourceAccountId: account.Id, categoryId: housing.Id, expectedDayOfMonth: 5);
            app.RecurringOperations.GenerateOccurrences(rent.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

            var today = new DateTime(2026, 9, 13);
            var viewModel = DashboardViewModelBuilder.Build(app, today);
            Check(viewModel is not null, "view model is built when an account exists");
            Check(viewModel!.VerificationQueue.Count == 1, "September's rent occurrence is due for verification");
            Check(Normalize(viewModel.AvailableBalanceText) == "1 748,60 €", "available balance formatted correctly");

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DashboardUxmlPath);
            if (visualTree == null)
            {
                throw new FileNotFoundException($"Dashboard UXML not found at {DashboardUxmlPath}");
            }

            var root = visualTree.Instantiate();
            var controller = new DashboardController(root);
            controller.Render(viewModel);

            Check(root.Q<Label>("account-name-label").text == "Compte courant", "account name bound");
            Check(Normalize(root.Q<Label>("kpi-available-value").text) == "1 748,60 €", "available balance bound");
            Check(root.Q<Label>("verification-count").text == "1", "verification count bound");
            Check(root.Q<Label>("verification-empty").style.display == DisplayStyle.None, "empty-state hidden when queue is non-empty");

            var verificationList = root.Q<VisualElement>("verification-list");
            Check(verificationList.childCount == 1, "one verification row rendered");

            var noAccountPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-empty-{Guid.NewGuid():N}.db");
            using (var noAccountApp = new AppContainer(noAccountPath))
            {
                Check(DashboardViewModelBuilder.Build(noAccountApp, today) is null, "no account yields a null view model, not a crash");
            }

            TryDeleteQuietly(noAccountPath);
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
                Debug.LogWarning($"[UISmokeTest] Could not delete temp file (harmless): {path}");
            }
        }

        private static void Check(bool condition, string what)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"UI smoke test assertion failed: {what}.");
            }
        }
    }
}
