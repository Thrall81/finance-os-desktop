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
        private const string AccountsUxmlPath = "Assets/UI/UXML/Accounts.uxml";
        private const string TransactionsUxmlPath = "Assets/UI/UXML/Transactions.uxml";
        private const string RecurringOperationsUxmlPath = "Assets/UI/UXML/RecurringOperations.uxml";
        private const string ShellUxmlPath = "Assets/UI/UXML/Shell.uxml";

        [MenuItem("Finance OS/Run UI Smoke Test")]
        public static void Run()
        {
            CheckMoneyFormat();
            CheckMoneyParse();
            CheckDateFormat();

            var tempPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-{Guid.NewGuid():N}.db");
            RunAgainstDatabase(tempPath);
            TryDeleteQuietly(tempPath);

            CheckShell();

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

        private static void CheckMoneyParse()
        {
            Check(MoneyFormat.TryParseEurosToMinor("1234,56", out var a) && a == 123_456, "comma decimal parses");
            Check(MoneyFormat.TryParseEurosToMinor("1234.56", out var b) && b == 123_456, "dot decimal parses");
            Check(MoneyFormat.TryParseEurosToMinor("-12", out var d) && d == -1_200, "negative amount parses");
            Check(MoneyFormat.TryParseEurosToMinor("0", out var e) && e == 0, "zero parses");
            Check(!MoneyFormat.TryParseEurosToMinor("", out _), "empty text is rejected");
            Check(!MoneyFormat.TryParseEurosToMinor("abc", out _), "non-numeric text is rejected");
        }

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

            var savings = app.Accounts.CreateAccount("Ancien Livret", AccountType.Savings, "EUR", 20_000);
            app.Accounts.Archive(savings.Id);

            var accountsViewModel = AccountsViewModelBuilder.Build(app.Accounts);
            Check(accountsViewModel.Accounts.Count == 2, "both accounts appear in the accounts view model");
            Check(accountsViewModel.Accounts[0].Name == "Compte courant", "active account sorts before archived ones");
            Check(accountsViewModel.Accounts[0].TypeText == "Courant", "account type text mapped");
            Check(accountsViewModel.Accounts[0].LiquidityPolicyText == "Immédiat", "liquidity policy text mapped");
            Check(accountsViewModel.Accounts[1].Name == "Ancien Livret", "archived account sorts last");
            Check(accountsViewModel.Accounts[1].IsArchived, "archived flag carried through");

            var accountsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AccountsUxmlPath);
            if (accountsTree == null)
            {
                throw new FileNotFoundException($"Accounts UXML not found at {AccountsUxmlPath}");
            }

            var accountsRoot = accountsTree.Instantiate();
            var accountsController = new AccountsController(accountsRoot, app.Accounts);

            var accountsList = accountsRoot.Q<VisualElement>("accounts-list");
            Check(accountsList.childCount == 2, "accounts controller renders one row per account on construction");
            Check(accountsRoot.Q<Label>("accounts-empty").style.display == DisplayStyle.None, "empty-state hidden when accounts exist");

            var firstRow = accountsList.Children().ElementAt(0);
            Check(firstRow.Q<Label>(className: "account-row-name").text == "Compte courant", "first row shows the active account name");
            Check(Normalize(firstRow.Q<Label>(className: "account-row-balance").text) == "1 748,60 €", "first row shows the formatted balance");

            var secondRow = accountsList.Children().ElementAt(1);
            Check(secondRow.ClassListContains("account-row-archived"), "archived account row carries the archived style class");

            accountsController.Refresh();
            Check(accountsList.childCount == 2, "refresh re-renders the same two rows without duplication");

            var groceries = app.Categories.ListActive().First(c => c.Name == "Alimentation");
            var firstGroceries = app.Transactions.CreateManual(
                account.Id, -4_250, "EUR", new DateTime(2026, 8, 10), "  Carrefour  ", categoryId: groceries.Id);
            Check(firstGroceries.NormalizedLabel == "carrefour", "CreateManual auto-normalizes the label for future suggestion matching");

            var suggestedCategory = app.Transactions.SuggestCategoryForLabel("CARREFOUR");
            Check(suggestedCategory == groceries.Id, "suggestion matches regardless of case and surrounding whitespace");

            Check(DateFormat.ForInput(new DateTime(2026, 9, 13)) == "13/09/2026", "date formatted for input fields");
            Check(DateFormat.TryParseInput("13/09/2026", out var parsedDate) && parsedDate == new DateTime(2026, 9, 13), "input date parses back");
            Check(!DateFormat.TryParseInput("31/02/2026", out _), "invalid calendar date is rejected");
            Check(!DateFormat.TryParseInput("not a date", out _), "garbage text is rejected");

            var transactionsViewModel = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions, accountFilter: null);
            Check(transactionsViewModel.Transactions.Count == 1, "one transaction appears in the unfiltered view model");
            Check(transactionsViewModel.Transactions[0].CategoryText == "Alimentation", "category resolved by name");
            Check(transactionsViewModel.Transactions[0].AccountName == "Compte courant", "account resolved by name");
            Check(Normalize(transactionsViewModel.Transactions[0].AmountText) == "−42,50 €", "amount formatted with its sign");

            var filteredOnOtherAccount = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions, accountFilter: savings.Id);
            Check(filteredOnOtherAccount.Transactions.Count == 0, "filtering by an unrelated account yields an empty list");

            var transactionsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TransactionsUxmlPath);
            if (transactionsTree == null)
            {
                throw new FileNotFoundException($"Transactions UXML not found at {TransactionsUxmlPath}");
            }

            var transactionsRoot = transactionsTree.Instantiate();
            var transactionsController = new TransactionsController(
                transactionsRoot, app.Accounts, app.Categories, app.Counterparties, app.Transactions, app.InternalTransfers);

            var listView = transactionsRoot.Q<MultiColumnListView>("transactions-list-view");
            Check(listView.itemsSource.Count == 1, "transactions controller renders the one seeded transaction");
            Check(listView.columns.Count == 5, "five table columns configured");
            Check(transactionsRoot.Q<Label>("transactions-empty").style.display == DisplayStyle.None, "empty-state hidden when transactions exist");

            transactionsController.Refresh();
            Check(listView.itemsSource.Count == 1, "refresh re-renders without duplication");

            var salary = app.RecurringOperations.Create(
                "Salaire", RecurringOperationType.Income, 210_000, RecurringFrequency.Monthly,
                new DateTime(2026, 1, 1), destinationAccountId: account.Id);

            var newSavings = app.Accounts.CreateAccount("Livret Perso", AccountType.Savings, "EUR", 0);
            app.RecurringOperations.Create(
                "Épargne mensuelle", RecurringOperationType.SavingsTransfer, 20_000, RecurringFrequency.Monthly,
                new DateTime(2026, 1, 1), sourceAccountId: account.Id, destinationAccountId: newSavings.Id);

            var recurringViewModel = RecurringOperationsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.RecurringOperations);
            Check(recurringViewModel.Operations.Count == 3, "three recurring operations appear in the view model");

            var rentRow = recurringViewModel.Operations.First(o => o.Name == "Loyer");
            Check(rentRow.TypeText == "Dépense", "expense type text mapped");
            Check(rentRow.AccountText == "Compte courant", "expense shows its source account");
            Check(rentRow.FrequencyText == "Mensuelle", "frequency text mapped");
            Check(rentRow.CategoryText == "Logement", "category resolved by name");
            Check(rentRow.IsActive, "newly created operation is active");

            var salaryRow = recurringViewModel.Operations.First(o => o.Name == "Salaire");
            Check(salaryRow.AccountText == "Compte courant", "income shows its destination account");

            var transferRow = recurringViewModel.Operations.First(o => o.Name == "Épargne mensuelle");
            Check(transferRow.AccountText == "Compte courant → Livret Perso", "transfer shows source then destination");

            app.RecurringOperations.GenerateUpcomingOccurrences(today, 90);
            var followUp = app.RecurringOperations.GenerateOccurrences(rent.Id, rent.StartDate, today.AddDays(90));
            Check(followUp.Count == 0, "GenerateUpcomingOccurrences already covered the horizon for an active operation — idempotent");

            app.RecurringOperations.Suspend(salary.Id);
            var afterSuspend = RecurringOperationsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.RecurringOperations);
            Check(!afterSuspend.Operations.First(o => o.Name == "Salaire").IsActive, "suspended operation reflects inactive state");
            Check(afterSuspend.Operations.Last().Name == "Salaire", "suspended operations sort after active ones");

            var recurringOperationsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RecurringOperationsUxmlPath);
            if (recurringOperationsTree == null)
            {
                throw new FileNotFoundException($"RecurringOperations UXML not found at {RecurringOperationsUxmlPath}");
            }

            var recurringOperationsRoot = recurringOperationsTree.Instantiate();
            var recurringOperationsController = new RecurringOperationsController(
                recurringOperationsRoot, app.Accounts, app.Categories, app.Counterparties, app.RecurringOperations, app.Settings);

            var operationsListView = recurringOperationsRoot.Q<MultiColumnListView>("operations-list-view");
            Check(operationsListView.itemsSource.Count == 3, "controller renders all three recurring operations");
            Check(operationsListView.columns.Count == 6, "six table columns configured");
            Check(recurringOperationsRoot.Q<Label>("operations-empty").style.display == DisplayStyle.None, "empty-state hidden when operations exist");

            recurringOperationsController.Refresh();
            Check(operationsListView.itemsSource.Count == 3, "refresh re-renders without duplication");

            var noAccountPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-empty-{Guid.NewGuid():N}.db");
            using (var noAccountApp = new AppContainer(noAccountPath))
            {
                Check(DashboardViewModelBuilder.Build(noAccountApp, today) is null, "no account yields a null view model, not a crash");

                var emptyAccountsRoot = accountsTree.Instantiate();
                _ = new AccountsController(emptyAccountsRoot, noAccountApp.Accounts);
                Check(emptyAccountsRoot.Q<VisualElement>("accounts-list").childCount == 0, "no accounts renders an empty list");
                Check(emptyAccountsRoot.Q<Label>("accounts-empty").style.display == DisplayStyle.Flex, "empty-state shown when no accounts exist");

                var emptyTransactionsRoot = transactionsTree.Instantiate();
                _ = new TransactionsController(
                    emptyTransactionsRoot, noAccountApp.Accounts, noAccountApp.Categories, noAccountApp.Counterparties,
                    noAccountApp.Transactions, noAccountApp.InternalTransfers);
                Check(emptyTransactionsRoot.Q<Label>("transactions-empty").style.display == DisplayStyle.Flex, "empty-state shown when no transactions exist");
                Check(!emptyTransactionsRoot.Q<Button>("new-transaction-button").enabledSelf, "new-transaction button disabled with no account to post against");

                var emptyRecurringOperationsRoot = recurringOperationsTree.Instantiate();
                _ = new RecurringOperationsController(
                    emptyRecurringOperationsRoot, noAccountApp.Accounts, noAccountApp.Categories, noAccountApp.Counterparties,
                    noAccountApp.RecurringOperations, noAccountApp.Settings);
                Check(emptyRecurringOperationsRoot.Q<Label>("operations-empty").style.display == DisplayStyle.Flex, "empty-state shown when no operations exist");
                Check(!emptyRecurringOperationsRoot.Q<Button>("new-operation-button").enabledSelf, "new-operation button disabled with no account to post against");
            }

            TryDeleteQuietly(noAccountPath);
        }

        private static void CheckShell()
        {
            var shellTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
            if (shellTree == null)
            {
                throw new FileNotFoundException($"Shell UXML not found at {ShellUxmlPath}");
            }

            var root = shellTree.Instantiate();
            var dashboardCalls = 0;
            var accountsCalls = 0;
            var transactionsCalls = 0;
            var recurringOperationsCalls = 0;
            var shell = new ShellController(
                root, () => dashboardCalls++, () => accountsCalls++, () => transactionsCalls++, () => recurringOperationsCalls++);

            var content = new VisualElement();
            shell.SetContent(content);
            Check(root.Q<VisualElement>("content-area").childCount == 1, "shell content area receives the screen's content");

            shell.SetActive(ShellScreen.Accounts);
            Check(root.Q<Button>("nav-accounts").ClassListContains("nav-item-active"), "accounts nav item marked active");
            Check(!root.Q<Button>("nav-dashboard").ClassListContains("nav-item-active"), "dashboard nav item not active");
            Check(!root.Q<Button>("nav-transactions").ClassListContains("nav-item-active"), "transactions nav item not active");
            Check(!root.Q<Button>("nav-recurring-operations").ClassListContains("nav-item-active"), "recurring operations nav item not active");

            shell.SetActive(ShellScreen.Transactions);
            Check(root.Q<Button>("nav-transactions").ClassListContains("nav-item-active"), "transactions nav item marked active");
            Check(!root.Q<Button>("nav-accounts").ClassListContains("nav-item-active"), "accounts nav item not active");

            shell.SetActive(ShellScreen.RecurringOperations);
            Check(root.Q<Button>("nav-recurring-operations").ClassListContains("nav-item-active"), "recurring operations nav item marked active");
            Check(!root.Q<Button>("nav-transactions").ClassListContains("nav-item-active"), "transactions nav item not active");

            shell.SetActive(ShellScreen.Dashboard);
            Check(root.Q<Button>("nav-dashboard").ClassListContains("nav-item-active"), "dashboard nav item marked active");
            Check(!root.Q<Button>("nav-accounts").ClassListContains("nav-item-active"), "accounts nav item not active");
            Check(!root.Q<Button>("nav-transactions").ClassListContains("nav-item-active"), "transactions nav item not active");
            Check(!root.Q<Button>("nav-recurring-operations").ClassListContains("nav-item-active"), "recurring operations nav item not active");

            using (var clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = root.Q<Button>("nav-accounts");
                root.Q<Button>("nav-accounts").SendEvent(clickEvent);
            }

            Debug.Log($"[UISmokeTest] Shell nav click delivery: dashboard={dashboardCalls}, accounts={accountsCalls}, transactions={transactionsCalls}, recurringOperations={recurringOperationsCalls} (best-effort without an attached panel).");
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
