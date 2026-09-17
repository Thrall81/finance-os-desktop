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
        private const string ForecastsUxmlPath = "Assets/UI/UXML/Forecasts.uxml";
        private const string BudgetsUxmlPath = "Assets/UI/UXML/Budgets.uxml";
        private const string CategoriesUxmlPath = "Assets/UI/UXML/Categories.uxml";
        private const string SettingsUxmlPath = "Assets/UI/UXML/Settings.uxml";
        private const string OnboardingUxmlPath = "Assets/UI/UXML/Onboarding.uxml";
        private const string ShellUxmlPath = "Assets/UI/UXML/Shell.uxml";

        [MenuItem("Finance OS/Run UI Smoke Test")]
        public static void Run()
        {
            CheckMoneyFormat();
            CheckMoneyParse();
            CheckDateFormat();
            CheckNumericInputFilter();

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

        // ADR-141: the actual keystroke NumericInputFilter would normally run from can't be
        // simulated in batchmode (no live panel, same limitation as every other interaction —
        // e.g. ADR-113) — Sanitize is public specifically so its logic is still directly testable.
        private static void CheckNumericInputFilter()
        {
            Check(NumericInputFilter.Sanitize("123abc", allowDecimalSeparator: false, allowNegative: false) == "123",
                "letters are stripped from an integer field");
            Check(NumericInputFilter.Sanitize("12,50", allowDecimalSeparator: true, allowNegative: false) == "12,50",
                "comma decimal separator is kept on a money field");
            Check(NumericInputFilter.Sanitize("12.50.30", allowDecimalSeparator: true, allowNegative: false) == "12.5030",
                "a second decimal separator is dropped, not just the extra digits");
            Check(NumericInputFilter.Sanitize("1 234,56", allowDecimalSeparator: true, allowNegative: false) == "1234,56",
                "spaces (thousands grouping) are stripped, matching what MoneyFormat.TryParseEurosToMinor already tolerates");
            Check(NumericInputFilter.Sanitize("-45", allowDecimalSeparator: false, allowNegative: true) == "-45",
                "a leading minus is kept when negative values are allowed");
            Check(NumericInputFilter.Sanitize("4-5", allowDecimalSeparator: false, allowNegative: true) == "45",
                "a minus anywhere but the very start is dropped, not just tolerated");
            Check(NumericInputFilter.Sanitize("-45", allowDecimalSeparator: false, allowNegative: false) == "45",
                "a leading minus is dropped entirely on a field that never expects a negative value");
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
            Check(viewModel.ChartSeries.Count > 0, "chart series is populated when an account exists");
            Check(viewModel.RemainingToLiveText == "—", "reste à vivre falls back to a placeholder with no budget created yet for this month");
            Check(viewModel.BudgetSummary.Count == 0, "budget summary is empty with no budget created yet for this month");
            Check(viewModel.Alerts.Count == 0, "no alerts yet — no budget to be over, and the balance is well above the low-balance threshold");
            Check(viewModel.UpcomingOperations.Count == 0, "no upcoming operations yet — the only occurrence so far (September's rent) is already overdue, not upcoming");

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
            Check(root.Q<Label>("kpi-remaining-value").text == "—", "reste à vivre placeholder bound");
            Check(root.Q<Label>("verification-count").text == "1", "verification count bound");
            Check(root.Q<Label>("verification-empty").style.display == DisplayStyle.None, "empty-state hidden when queue is non-empty");

            var verificationList = root.Q<VisualElement>("verification-list");
            Check(verificationList.childCount == 1, "one verification row rendered");

            var dashboardChart = root.Q<LineChartElement>();
            Check(dashboardChart!.Points.Count == viewModel.ChartSeries.Count, "dashboard chart element receives the full chart series");
            Check(dashboardChart.style.flexGrow.value == 1f, "dashboard chart element grows to fill its fixed-height container");
            Check(dashboardChart.Q<Label>(className: "chart-tooltip") is not null, "cash-flow chart's hover tooltip label exists");
            Check(dashboardChart.Q<Label>(className: "chart-tooltip")!.style.display == DisplayStyle.None, "cash-flow chart's hover tooltip is hidden by default");
            Check(LineChartElement.FindNearestPointIndex(pointCount: 0, elementWidth: 300, sidePadding: 6, localX: 100) is null, "no nearest point with zero points");
            Check(LineChartElement.FindNearestPointIndex(pointCount: 1, elementWidth: 300, sidePadding: 6, localX: 100) is null, "no nearest point with a single point either — nothing is drawn below two points");
            Check(LineChartElement.FindNearestPointIndex(pointCount: 5, elementWidth: 300, sidePadding: 6, localX: 6) == 0, "leftmost pointer position resolves to the first point");
            Check(LineChartElement.FindNearestPointIndex(pointCount: 5, elementWidth: 300, sidePadding: 6, localX: 294) == 4, "rightmost pointer position resolves to the last point");
            Check(LineChartElement.FindNearestPointIndex(pointCount: 5, elementWidth: 300, sidePadding: 6, localX: 150) == 2, "middle pointer position resolves to the middle point");

            Check(!dashboardChart.DarkTheme, "dashboard chart defaults to light theme when isDarkTheme is omitted");
            var darkRoot = visualTree.Instantiate();
            var darkController = new DashboardController(darkRoot, isDarkTheme: true);
            darkController.Render(viewModel);
            Check(darkRoot.Q<LineChartElement>()!.DarkTheme, "isDarkTheme: true reaches the cash-flow chart");
            Check(darkRoot.Q<ExpenseDonutElement>()!.DarkTheme, "isDarkTheme: true reaches the expense donut too");

            Check(viewModel.ExpenseBreakdown.Count == 0, "no September expense transactions exist yet at this point in the scenario");
            Check(root.Q<Label>("donut-empty").style.display == DisplayStyle.Flex, "donut empty-state shown with no expenses this month");
            Check(root.Q<VisualElement>("donut-row").style.display == DisplayStyle.None, "donut row hidden with no expenses this month");
            Check(root.Q<ExpenseDonutElement>()!.Slices.Count == 0, "donut chart has no slices with no expenses — constructing/rendering it did not throw");

            Check(root.Q<Label>("upcoming-empty").style.display == DisplayStyle.Flex, "upcoming-operations empty-state shown with nothing upcoming yet");
            Check(root.Q<VisualElement>("upcoming-list").childCount == 0, "no upcoming-operations rows rendered yet");

            Check(root.Q<Label>("budget-summary-empty").style.display == DisplayStyle.Flex, "budget-summary empty-state shown with no budget created yet");
            Check(root.Q<VisualElement>("budget-summary-list").childCount == 0, "no budget-summary rows rendered yet");

            Check(root.Q<Label>("alerts-empty").style.display == DisplayStyle.Flex, "alerts empty-state shown with nothing to alert on yet");
            Check(root.Q<VisualElement>("alerts-list").childCount == 0, "no alert rows rendered yet");

            // Isolated fixture (own temp database) rather than reusing `app`/`account`: by the time
            // enough recurring operations exist later in this scenario to have real upcoming
            // occurrences, several of them (Loyer, Salaire, Épargne mensuelle) would all land in
            // the same few future months, making exact order/count fragile to assert against. One
            // recurring (Attendue) + one one-off (Estimée) occurrence here instead, fully controlled.
            var upcomingTestPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-upcoming-{Guid.NewGuid():N}.db");
            using (var upcomingApp = new AppContainer(upcomingTestPath))
            {
                upcomingApp.Categories.SeedDefaultCategoriesIfEmpty();
                var upcomingAccount = upcomingApp.Accounts.CreateAccount("Compte test", AccountType.Current, "EUR", 100_000);
                upcomingApp.Accounts.RecordOfficialBalance(upcomingAccount.Id, 100_000, today);

                var subscription = upcomingApp.RecurringOperations.Create(
                    "Abonnement", RecurringOperationType.Expense, 1_000, RecurringFrequency.Monthly,
                    new DateTime(2026, 9, 1), sourceAccountId: upcomingAccount.Id, expectedDayOfMonth: 20);
                upcomingApp.RecurringOperations.GenerateOccurrences(subscription.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));
                upcomingApp.ForecastOccurrences.Create(upcomingAccount.Id, "Remboursement ami", new DateTime(2026, 9, 25), 5_000);

                var upcomingViewModel = DashboardViewModelBuilder.Build(upcomingApp, today);
                Check(upcomingViewModel!.UpcomingOperations.Count == 2, "both the recurring and the one-off future occurrence appear");
                Check(upcomingViewModel.UpcomingOperations[0].Label == "Abonnement", "sorted ascending — the 20th comes before the 25th");
                Check(upcomingViewModel.UpcomingOperations[0].CertaintyText == "Attendue", "tied to a recurring operation");
                Check(Normalize(upcomingViewModel.UpcomingOperations[0].AmountText) == "−10,00 €", "expense amount formatted with its sign");
                Check(upcomingViewModel.UpcomingOperations[1].Label == "Remboursement ami", "the one-off occurrence sorts second");
                Check(upcomingViewModel.UpcomingOperations[1].CertaintyText == "Estimée", "no recurring operation behind a manually created occurrence");
                Check(Normalize(upcomingViewModel.UpcomingOperations[1].AmountText) == "+50,00 €", "a positive amount is force-signed, matching the mockup");
            }

            TryDeleteQuietly(upcomingTestPath);

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
            var accountsController = new AccountsController(accountsRoot, app.Accounts, app.Settings);

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

            // Isolated fixture: recording a new official balance moves the account's forecast
            // anchor (Account.RecordOfficialBalance) — doing this on the shared `account` fixture
            // would disturb the many forecast/budget assertions it feeds for the rest of this
            // file, same reasoning as the other isolated blocks in this suite.
            var balanceHistoryPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-balance-history-{Guid.NewGuid():N}.db");
            using (var balanceHistoryApp = new AppContainer(balanceHistoryPath))
            {
                var balanceHistoryAccount = balanceHistoryApp.Accounts.CreateAccount("Compte test", AccountType.Current, "EUR", 100_000);

                var balanceHistoryTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AccountsUxmlPath);
                var balanceHistoryRoot = balanceHistoryTree.Instantiate();
                var balanceHistoryController = new AccountsController(balanceHistoryRoot, balanceHistoryApp.Accounts, balanceHistoryApp.Settings);

                Check(balanceHistoryRoot.Q<VisualElement>("balance-history-section").style.display == DisplayStyle.None,
                    "balance-history section hidden before any account is opened for edit");

                var balanceHistoryRowViewModel = AccountsViewModelBuilder.Build(balanceHistoryApp.Accounts).Accounts.Single();
                balanceHistoryController.OpenEditForm(balanceHistoryRowViewModel);
                Check(balanceHistoryRoot.Q<VisualElement>("balance-history-section").style.display == DisplayStyle.Flex,
                    "balance-history section shown once an account is opened for edit");
                Check(balanceHistoryRoot.Q<Label>("balance-history-empty").style.display == DisplayStyle.Flex,
                    "balance-history empty-state shown with no recorded balance yet");
                Check(balanceHistoryRoot.Q<VisualElement>("balance-history-list").childCount == 0, "no balance-history rows rendered yet");

                // balance-history-date is a Button (opens the App UI DatePicker, ADR-145) rather
                // than a TextField now — its click can't be simulated without a live panel here,
                // same limitation as every other button in this project, so this only checks that
                // OpenEditForm seeded it with today's date rather than the placeholder, and lets
                // SubmitBalanceHistory use that default rather than trying to pick a specific date.
                Check(balanceHistoryRoot.Q<Button>("balance-history-date").text != "jj/mm/aaaa",
                    "the balance-history date trigger shows today's date, not the placeholder, once the edit form opens");
                var balanceHistoryAmountField = balanceHistoryRoot.Q<TextField>("balance-history-amount");
                balanceHistoryAmountField.SetValueWithoutNotify("1050,00");
                balanceHistoryController.SubmitBalanceHistory();

                Check(balanceHistoryRoot.Q<Label>("balance-history-empty").style.display == DisplayStyle.None,
                    "balance-history empty-state hidden once a balance is recorded");
                Check(balanceHistoryRoot.Q<VisualElement>("balance-history-list").childCount == 1, "one balance-history row rendered after submit");
                Check(balanceHistoryApp.Accounts.FindById(balanceHistoryAccount.Id)!.OfficialBalanceMinor == 105_000,
                    "the submitted balance became the account's new official balance");

                balanceHistoryAmountField.SetValueWithoutNotify("not a number");
                balanceHistoryController.SubmitBalanceHistory();
                Check(balanceHistoryRoot.Q<Label>("balance-history-error").style.display == DisplayStyle.Flex,
                    "an invalid amount shows the balance-history form's own error label");
                Check(balanceHistoryRoot.Q<VisualElement>("balance-history-list").childCount == 1, "a rejected submission does not add a row");
            }

            TryDeleteQuietly(balanceHistoryPath);

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
                app.Accounts, app.Categories, app.Counterparties, app.Transactions, new TransactionFilter());
            Check(transactionsViewModel.Transactions.Count == 1, "one transaction appears in the unfiltered view model");
            Check(transactionsViewModel.Transactions[0].CategoryText == "Alimentation", "category resolved by name");
            Check(transactionsViewModel.Transactions[0].AccountName == "Compte courant", "account resolved by name");
            Check(Normalize(transactionsViewModel.Transactions[0].AmountText) == "−42,50 €", "amount formatted with its sign");

            var filteredOnOtherAccount = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions, new TransactionFilter(AccountId: savings.Id));
            Check(filteredOnOtherAccount.Transactions.Count == 0, "filtering by an unrelated account yields an empty list");

            var filteredOnOtherCategory = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions, new TransactionFilter(CategoryId: housing.Id));
            Check(filteredOnOtherCategory.Transactions.Count == 0, "filtering by an unrelated category yields an empty list");

            var filteredOnMatchingCategory = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions, new TransactionFilter(CategoryId: groceries.Id));
            Check(filteredOnMatchingCategory.Transactions.Count == 1, "filtering by the matching category keeps the transaction");

            var filteredOnDateRange = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions,
                new TransactionFilter(DateFrom: new DateTime(2026, 9, 1), DateTo: new DateTime(2026, 9, 30)));
            Check(filteredOnDateRange.Transactions.Count == 0, "the August transaction falls outside a September date range");

            var filteredOnAmountRange = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions,
                new TransactionFilter(AmountMinMinor: 4_000, AmountMaxMinor: 4_500));
            Check(filteredOnAmountRange.Transactions.Count == 1, "amount filter matches by magnitude regardless of the transaction's sign");

            var filteredOnAmountRangeMiss = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions,
                new TransactionFilter(AmountMinMinor: 5_000));
            Check(filteredOnAmountRangeMiss.Transactions.Count == 0, "amount filter excludes a transaction below the minimum magnitude");

            var filteredOnMatchingText = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions, new TransactionFilter(Text: "carre"));
            Check(filteredOnMatchingText.Transactions.Count == 1, "text filter matches a case-insensitive substring of the label");

            var filteredOnOtherText = TransactionsViewModelBuilder.Build(
                app.Accounts, app.Categories, app.Counterparties, app.Transactions, new TransactionFilter(Text: "nomatch"));
            Check(filteredOnOtherText.Transactions.Count == 0, "text filter excludes a label that doesn't contain the search text");

            var transactionsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TransactionsUxmlPath);
            if (transactionsTree == null)
            {
                throw new FileNotFoundException($"Transactions UXML not found at {TransactionsUxmlPath}");
            }

            var transactionsRoot = transactionsTree.Instantiate();
            var transactionsController = new TransactionsController(
                transactionsRoot, app.Accounts, app.Categories, app.Counterparties, app.Transactions,
                app.InternalTransfers, app.TransferDetection, app.Settings);

            var listView = transactionsRoot.Q<MultiColumnListView>("transactions-list-view");
            Check(listView.itemsSource.Count == 1, "transactions controller renders the one seeded transaction");
            Check(listView.columns.Count == 5, "five table columns configured");
            Check(transactionsRoot.Q<Label>("transactions-empty").style.display == DisplayStyle.None, "empty-state hidden when transactions exist");

            transactionsController.Refresh();
            Check(listView.itemsSource.Count == 1, "refresh re-renders without duplication");

            // The tree instantiated above has no panel, so a normal .value assignment's ChangeEvent
            // never dispatches here (same limitation as Button.clicked, cf. TransactionsController's
            // public OnFilterChanged) — use SetValueWithoutNotify then call it directly.
            var filterTextField = transactionsRoot.Q<TextField>("filter-text");
            filterTextField.SetValueWithoutNotify("carrefour");
            transactionsController.OnFilterChanged();
            Check(listView.itemsSource.Count == 1, "filter-text field keeps a transaction whose label matches, driven through the real UXML field");

            filterTextField.SetValueWithoutNotify("nomatch");
            transactionsController.OnFilterChanged();
            Check(listView.itemsSource.Count == 0, "filter-text field hides transactions once the value matches nothing");
            Check(transactionsRoot.Q<Label>("transactions-empty").style.display == DisplayStyle.Flex, "empty-state reappears when a live filter matches nothing");
            filterTextField.SetValueWithoutNotify(string.Empty);
            transactionsController.OnFilterChanged();
            Check(listView.itemsSource.Count == 1, "clearing filter-text restores the unfiltered list");

            var filterDateFromField = transactionsRoot.Q<TextField>("filter-date-from");
            filterDateFromField.SetValueWithoutNotify("01/09/2026");
            transactionsController.OnFilterChanged();
            Check(listView.itemsSource.Count == 0, "filter-date-from excludes the August-dated seed transaction");
            filterDateFromField.SetValueWithoutNotify("not a date");
            transactionsController.OnFilterChanged();
            Check(listView.itemsSource.Count == 1, "an unparseable filter-date-from value is treated as no constraint rather than blocking the list");
            filterDateFromField.SetValueWithoutNotify(string.Empty);
            transactionsController.OnFilterChanged();

            var filterAmountMinField = transactionsRoot.Q<TextField>("filter-amount-min");
            filterAmountMinField.SetValueWithoutNotify("50");
            transactionsController.OnFilterChanged();
            Check(listView.itemsSource.Count == 0, "filter-amount-min excludes a transaction below the minimum magnitude");
            filterAmountMinField.SetValueWithoutNotify(string.Empty);
            transactionsController.OnFilterChanged();
            Check(listView.itemsSource.Count == 1, "clearing filter-amount-min restores the unfiltered list");

            Check(transactionsRoot.Q<Label>("transfer-suggestions-count").text == "0", "no transfer suggestion with only one seeded transaction");
            Check(transactionsRoot.Q<Label>("transfer-suggestions-empty").style.display == DisplayStyle.Flex, "transfer-suggestions empty-state shown with nothing to suggest");

            // Isolated fixture (own temp database), same reasoning as the "Prochaines opérations"
            // block above: a genuine unlinked transfer pair, with a balance history fully under
            // this test's control rather than the shared fixture's, so the suggestion count is
            // unambiguous regardless of what else `app` accumulates elsewhere in this file.
            var transferTestPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-transfer-{Guid.NewGuid():N}.db");
            using (var transferApp = new AppContainer(transferTestPath))
            {
                var transferCurrent = transferApp.Accounts.CreateAccount("Compte courant", AccountType.Current, "EUR", 100_000);
                var transferSavings = transferApp.Accounts.CreateAccount("Livret A", AccountType.Savings, "EUR", 50_000);

                var transferOutgoing = transferApp.Transactions.CreateManual(
                    transferCurrent.Id, -20_000, "EUR", new DateTime(2026, 9, 6), "Retrait");
                var transferIncoming = transferApp.Transactions.CreateManual(
                    transferSavings.Id, 20_000, "EUR", new DateTime(2026, 9, 7), "Dépôt");

                var transferTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TransactionsUxmlPath);
                var transferRoot = transferTree.Instantiate();
                var transferController = new TransactionsController(
                    transferRoot, transferApp.Accounts, transferApp.Categories, transferApp.Counterparties,
                    transferApp.Transactions, transferApp.InternalTransfers, transferApp.TransferDetection, transferApp.Settings);

                Check(transferRoot.Q<Label>("transfer-suggestions-count").text == "1", "one suggested transfer pair is counted");
                Check(transferRoot.Q<Label>("transfer-suggestions-empty").style.display == DisplayStyle.None, "suggestions empty-state hidden when a candidate exists");
                Check(transferRoot.Q<VisualElement>("transfer-suggestions-list").childCount == 1, "one suggestion row rendered");

                transferApp.TransferDetection.Reject(transferOutgoing.Id, transferIncoming.Id);
                transferController.Refresh();
                Check(transferRoot.Q<Label>("transfer-suggestions-count").text == "0", "rejecting the pair removes it from the suggestion list on the next refresh");
                Check(transferRoot.Q<Label>("transfer-suggestions-empty").style.display == DisplayStyle.Flex, "suggestions empty-state reappears once nothing is left to suggest");
                Check(!transferApp.Transactions.FindById(transferOutgoing.Id)!.IsInternalTransfer, "rejecting from the UI leaves the underlying transactions untouched, same as the App-layer behavior");
            }

            TryDeleteQuietly(transferTestPath);

            // Dashboard expense-breakdown donut: "Carrefour" above is dated in August, outside
            // this month's window, so a couple of September-dated expenses are needed to exercise
            // ExpenseDonutElement with real (non-empty) data.
            app.Transactions.CreateManual(account.Id, -3_000, "EUR", new DateTime(2026, 9, 8), "Monoprix", categoryId: groceries.Id);
            app.Transactions.CreateManual(account.Id, -9_000, "EUR", new DateTime(2026, 9, 9), "Loyer manuel", categoryId: housing.Id);

            var septemberViewModel = DashboardViewModelBuilder.Build(app, today);
            Check(septemberViewModel!.ExpenseBreakdown.Count == 2, "two categories appear in this month's expense breakdown");
            Check(septemberViewModel.ExpenseBreakdown[0].CategoryName == "Logement", "biggest expense sorts first");
            Check(septemberViewModel.ExpenseBreakdown[0].AmountMinor == 9_000, "raw amount is a positive magnitude, not the signed transaction amount");
            Check(Normalize(septemberViewModel.ExpenseBreakdown[0].AmountText) == "90,00 €", "amount formatted correctly");
            Check(septemberViewModel.ExpenseBreakdown[0].PercentText == "75 %", "75% of the 120,00 € total is Logement's share");
            Check(septemberViewModel.ExpenseBreakdown[1].CategoryName == "Alimentation", "smaller category sorts second");
            Check(septemberViewModel.ExpenseBreakdown[1].PercentText == "25 %", "the remaining 25% is Alimentation's share");

            controller.Render(septemberViewModel);
            Check(root.Q<Label>("donut-empty").style.display == DisplayStyle.None, "donut empty-state hidden once expenses exist");
            Check(root.Q<VisualElement>("donut-row").style.display == DisplayStyle.Flex, "donut row shown once expenses exist");
            var donutChart = root.Q<ExpenseDonutElement>();
            Check(donutChart is not null, "donut chart element added to the chart container");
            Check(donutChart!.Slices.Count == 2, "donut chart element receives one slice per category");
            Check(donutChart.style.flexGrow.value == 1f, "donut chart element grows to fill its fixed-size container, same requirement as the other two charts");
            Check(donutChart.Q<Label>(className: "chart-tooltip") is not null, "donut chart's hover tooltip label exists");
            Check(donutChart.Q<Label>(className: "chart-tooltip")!.style.display == DisplayStyle.None, "donut chart's hover tooltip is hidden by default");

            // 200x200 square, matching the private padding/inner-radius constants: center (100,100),
            // outer radius 94, inner radius ~51.7 — a mid-ring point straight right of center sits
            // deep inside the biggest slice (Logement, 75%), straight up-left inside the smaller one
            // (Alimentation, 25%, the last 90° before wrapping back to the top).
            Check(ExpenseDonutElement.FindSliceUnderPointer(donutChart.Slices, 200, 200, 175, 100) == 0, "pointer to the right of center hits the biggest (first) slice");
            Check(ExpenseDonutElement.FindSliceUnderPointer(donutChart.Slices, 200, 200, 47, 47) == 1, "pointer up-and-left of center hits the smaller (second) slice");
            Check(ExpenseDonutElement.FindSliceUnderPointer(donutChart.Slices, 200, 200, 100, 100) is null, "pointer at dead center (inside the hole) hits nothing");
            Check(ExpenseDonutElement.FindSliceUnderPointer(donutChart.Slices, 200, 200, 1000, 1000) is null, "pointer far outside the ring hits nothing");
            Check(ExpenseDonutElement.FindSliceUnderPointer(Array.Empty<ExpenseCategorySliceViewModel>(), 200, 200, 175, 100) is null, "no slice to hit with an empty list");
            Check(root.Q<VisualElement>("donut-legend").childCount == 2, "one legend row per category");

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

            // Real bug found via a real account (see project memory, 2026-09-13): a start date one
            // day after the chosen day-of-month silently skips the whole first month.
            var skippedFirstDate = app.RecurringOperations.PreviewFirstOccurrenceDate(
                RecurringFrequency.Monthly, new DateTime(2026, 9, 29), expectedDayOfMonth: 28);
            Check(skippedFirstDate == new DateTime(2026, 10, 28), "mismatched start date/day-of-month previews a skipped first month, not September");

            var alignedFirstDate = app.RecurringOperations.PreviewFirstOccurrenceDate(
                RecurringFrequency.Monthly, new DateTime(2026, 9, 28), expectedDayOfMonth: 28);
            Check(alignedFirstDate == new DateTime(2026, 9, 28), "matching start date/day-of-month previews the same month, no skip");

            var weeklyFirstDate = app.RecurringOperations.PreviewFirstOccurrenceDate(
                RecurringFrequency.Weekly, new DateTime(2026, 9, 29), expectedDayOfMonth: null);
            Check(weeklyFirstDate == new DateTime(2026, 9, 29), "a weekly schedule always starts on its own start date — no day-of-month mismatch possible");

            var deletableOperation = app.RecurringOperations.Create(
                "Abonnement test", RecurringOperationType.Expense, 999, RecurringFrequency.Monthly,
                new DateTime(2026, 1, 1), sourceAccountId: account.Id, expectedDayOfMonth: 1);
            app.RecurringOperations.GenerateOccurrences(deletableOperation.Id, deletableOperation.StartDate, today.AddDays(30));
            app.RecurringOperations.Delete(deletableOperation.Id);
            Check(app.RecurringOperations.FindById(deletableOperation.Id) is null, "deleting an operation with no matched occurrence removes it");
            Check(app.ForecastOccurrences.ListForRecurringOperation(deletableOperation.Id, deletableOperation.StartDate, today.AddYears(1)).Count == 0,
                "deleting an operation cascades to its occurrences (ON DELETE CASCADE)");

            // A separate operation for this case (rather than reusing "rent"/"salary"): confirming
            // an occurrence removes it from the verification queue other assertions further down
            // still rely on.
            var historyOperation = app.RecurringOperations.Create(
                "Abonnement avec historique", RecurringOperationType.Expense, 500, RecurringFrequency.Monthly,
                new DateTime(2026, 1, 1), sourceAccountId: account.Id, expectedDayOfMonth: 1);
            var historyOccurrences = app.RecurringOperations.GenerateOccurrences(historyOperation.Id, historyOperation.StartDate, today.AddDays(30));
            var matchedOccurrence = historyOccurrences.First();
            app.ForecastOccurrences.ConfirmAsTransaction(matchedOccurrence.Id, matchedOccurrence.ExpectedDate, matchedOccurrence.ExpectedAmountMinor);
            var deleteBlocked = false;
            try
            {
                app.RecurringOperations.Delete(historyOperation.Id);
            }
            catch (InvalidOperationException)
            {
                deleteBlocked = true;
            }

            Check(deleteBlocked, "deleting an operation with a matched (confirmed) occurrence is refused");
            Check(app.RecurringOperations.FindById(historyOperation.Id) is not null, "the refused operation is still there afterward");

            var recurringOperationsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RecurringOperationsUxmlPath);
            if (recurringOperationsTree == null)
            {
                throw new FileNotFoundException($"RecurringOperations UXML not found at {RecurringOperationsUxmlPath}");
            }

            var recurringOperationsRoot = recurringOperationsTree.Instantiate();
            var recurringOperationsController = new RecurringOperationsController(
                recurringOperationsRoot, app.Accounts, app.Categories, app.Counterparties, app.RecurringOperations, app.Settings);

            var operationsListView = recurringOperationsRoot.Q<MultiColumnListView>("operations-list-view");
            Check(operationsListView.itemsSource.Count == 4, "controller renders every recurring operation, including the undeletable one with history");
            Check(operationsListView.columns.Count == 6, "six table columns configured");
            Check(recurringOperationsRoot.Q<Label>("operations-empty").style.display == DisplayStyle.None, "empty-state hidden when operations exist");

            recurringOperationsController.Refresh();
            Check(operationsListView.itemsSource.Count == 4, "refresh re-renders without duplication");

            Check(recurringOperationsRoot.Q<Label>("form-skip-warning") is not null, "skip-warning label is bound");
            Check(recurringOperationsRoot.Q<Button>("form-delete-button") is not null, "delete button is bound");

            // Isolated fixture: correcting an account regenerates this operation's occurrences,
            // which would otherwise shift the "4 operations"/verification-queue assertions the
            // shared `app`/`account` fixture already feeds above and below this block.
            var wrongAccountUiPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-wrong-account-{Guid.NewGuid():N}.db");
            using (var wrongAccountApp = new AppContainer(wrongAccountUiPath))
            {
                wrongAccountApp.Categories.SeedDefaultCategoriesIfEmpty();
                var wrongAccountHousing = wrongAccountApp.Categories.ListActive().First(c => c.Name == "Logement");

                var rightAccount = wrongAccountApp.Accounts.CreateAccount("Compte courant", AccountType.Current, "EUR", 100_000);
                var wrongAccount = wrongAccountApp.Accounts.CreateAccount("Livret A", AccountType.Savings, "EUR", 50_000);

                var operation = wrongAccountApp.RecurringOperations.Create(
                    "Loyer mal saisi", RecurringOperationType.Expense, 60_000, RecurringFrequency.Monthly,
                    new DateTime(2026, 9, 1), sourceAccountId: wrongAccount.Id, expectedDayOfMonth: 1);

                var wrongAccountRoot = recurringOperationsTree.Instantiate();
                var wrongAccountController = new RecurringOperationsController(
                    wrongAccountRoot, wrongAccountApp.Accounts, wrongAccountApp.Categories, wrongAccountApp.Counterparties,
                    wrongAccountApp.RecurringOperations, wrongAccountApp.Settings);

                var rowViewModel = RecurringOperationsViewModelBuilder.Build(
                        wrongAccountApp.Accounts, wrongAccountApp.Categories, wrongAccountApp.Counterparties, wrongAccountApp.RecurringOperations)
                    .Operations.Single(o => o.Id == operation.Id);

                wrongAccountController.OpenEditForm(rowViewModel);
                Check(wrongAccountRoot.Q<VisualElement>("form-source-account-row").style.display == DisplayStyle.Flex,
                    "the source-account field is editable in edit mode, not read-only");
                Check(wrongAccountRoot.Q<DropdownField>("form-source-account").value == "Livret A",
                    "the account dropdown opens pre-selected to the operation's current (wrong) account");

                // ADR-140: type, frequency, day-of-month, category and counterparty are all
                // editable rows now too, not read-only labels — and each opens pre-filled from
                // the operation's own current values, same as the account fields above.
                Check(wrongAccountRoot.Q<VisualElement>("form-type-row").style.display == DisplayStyle.Flex, "type row is editable in edit mode");
                Check(wrongAccountRoot.Q<DropdownField>("form-type").value == "Dépense", "type dropdown opens pre-selected to the operation's current type");
                Check(wrongAccountRoot.Q<VisualElement>("form-frequency-row").style.display == DisplayStyle.Flex, "frequency row is editable in edit mode");
                Check(wrongAccountRoot.Q<DropdownField>("form-frequency").value == "Mensuelle", "frequency dropdown opens pre-selected to the operation's current frequency");
                Check(wrongAccountRoot.Q<VisualElement>("form-day-of-month-row").style.display == DisplayStyle.Flex, "day-of-month row is editable in edit mode");
                Check(wrongAccountRoot.Q<TextField>("form-day-of-month").value == "1", "day-of-month field opens pre-filled with the operation's current value");
                Check(wrongAccountRoot.Q<VisualElement>("form-category-row").style.display == DisplayStyle.Flex, "category row is editable in edit mode");
                Check(wrongAccountRoot.Q<DropdownField>("form-category").value == "Aucune", "category dropdown opens on Aucune when the operation has no category");
                Check(wrongAccountRoot.Q<VisualElement>("form-counterparty-row").style.display == DisplayStyle.Flex, "counterparty row is editable in edit mode");
                Check(wrongAccountRoot.Q<TextField>("form-counterparty").value == string.Empty, "counterparty field opens empty when the operation has no counterparty");

                // Start date, previously read-only in edit mode, now reuses the same App UI
                // DatePicker trigger as creation (ADR-145 follow-up) — opens pre-filled with the
                // operation's actual start date, not today's.
                Check(wrongAccountRoot.Q<VisualElement>("form-start-date-row").style.display == DisplayStyle.Flex, "start-date row is editable in edit mode");
                Check(wrongAccountRoot.Q<Button>("form-start-date").text == DateFormat.ForInput(operation.StartDate),
                    "start-date trigger opens pre-filled with the operation's current start date");

                wrongAccountRoot.Q<DropdownField>("form-source-account").SetValueWithoutNotify("Compte courant");
                wrongAccountRoot.Q<DropdownField>("form-frequency").SetValueWithoutNotify("Trimestrielle");
                wrongAccountRoot.Q<DropdownField>("form-category").SetValueWithoutNotify("Logement");
                wrongAccountRoot.Q<TextField>("form-counterparty").SetValueWithoutNotify("Bailleur Test");
                wrongAccountController.SubmitForm();

                var corrected = wrongAccountApp.RecurringOperations.FindById(operation.Id)!;
                Check(corrected.SourceAccountId == rightAccount.Id, "submitting the edit form with a different account persists the correction");
                Check(corrected.Frequency == RecurringFrequency.Quarterly, "submitting the edit form with a different frequency persists the correction");
                Check(corrected.CategoryId == wrongAccountHousing.Id, "submitting the edit form with a category persists it");
                Check(corrected.CounterpartyId is not null, "submitting the edit form with a counterparty name creates and persists it");
                Check(wrongAccountApp.ForecastOccurrences.ListForAccount(wrongAccount.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)).Count == 0,
                    "the operation's occurrence no longer sits on the old account after the UI-driven correction");
            }

            TryDeleteQuietly(wrongAccountUiPath);

            var forecastsViewModel = ForecastsViewModelBuilder.Build(app, null, today);
            Check(forecastsViewModel.SelectedAccountId == account.Id, "primary account resolved to the Current-type account");
            Check(forecastsViewModel.Synthesis is not null, "synthesis built for the resolved account");
            Check(forecastsViewModel.VerificationQueue.Any(v => v.Label == "Loyer"), "rent occurrence appears in the verification queue");
            Check(forecastsViewModel.Occurrences.Count > 0, "occurrences list is non-empty for an account with recurring operations");
            Check(forecastsViewModel.Timeline.All(t => !string.IsNullOrEmpty(t.EventsText)), "every timeline row carries at least one event");
            Check(forecastsViewModel.ChartSeries.Count > forecastsViewModel.Timeline.Count, "chart series covers every day in the horizon, not just event days like the textual timeline");
            Check(forecastsViewModel.ChartSeries.Where(p => p.Date <= today).All(p => p.IsActual), "chart points up to today are flagged actual");
            Check(forecastsViewModel.ChartSeries.Where(p => p.Date > today).All(p => !p.IsActual), "chart points after today are flagged forecast, not actual");

            var livretPersoViewModel = ForecastsViewModelBuilder.Build(app, newSavings.Id, today);
            Check(livretPersoViewModel.SelectedAccountId == newSavings.Id, "explicit account selection is honored");
            Check(Normalize(livretPersoViewModel.Synthesis!.CurrentBalanceText) == "0,00 €", "Livret Perso starts at zero");

            var forecastsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ForecastsUxmlPath);
            if (forecastsTree == null)
            {
                throw new FileNotFoundException($"Forecasts UXML not found at {ForecastsUxmlPath}");
            }

            var forecastsRoot = forecastsTree.Instantiate();
            var forecastsController = new ForecastsController(forecastsRoot, app);

            var forecastOccurrencesListView = forecastsRoot.Q<MultiColumnListView>("occurrences-list-view");
            Check(forecastOccurrencesListView.itemsSource.Count == forecastsViewModel.Occurrences.Count, "forecasts controller renders the same occurrence count as the view model");
            Check(forecastOccurrencesListView.columns.Count == 4, "four occurrence columns configured");

            var cashFlowChart = forecastsRoot.Q<LineChartElement>();
            Check(cashFlowChart is not null, "cash-flow chart element added to the timeline card");
            Check(cashFlowChart!.Points.Count == forecastsViewModel.ChartSeries.Count, "chart element receives the full chart series");
            Check(cashFlowChart.style.flexGrow.value == 1f, "chart element grows to fill its fixed-height container — without this it renders nothing (blank contentRect), confirmed by screenshot");
            Check(!cashFlowChart.DarkTheme, "forecasts chart follows AppSettings.Theme, light by default");

            app.Settings.UpdateTheme(AppTheme.Dark);
            var darkForecastsRoot = forecastsTree.Instantiate();
            _ = new ForecastsController(darkForecastsRoot, app);
            Check(darkForecastsRoot.Q<LineChartElement>()!.DarkTheme, "a fresh ForecastsController picks up AppSettings.Theme at construction time");
            app.Settings.UpdateTheme(AppTheme.Light);

            var forecastTimelineListView = forecastsRoot.Q<MultiColumnListView>("timeline-list-view");
            Check(forecastTimelineListView.columns.Count == 3, "three timeline columns configured");

            var forecastVerificationList = forecastsRoot.Q<VisualElement>("verification-list");
            Check(forecastVerificationList.childCount == forecastsViewModel.VerificationQueue.Count, "verification queue rows rendered");
            Check(forecastsRoot.Q<Label>("verification-count").text == forecastsViewModel.VerificationQueue.Count.ToString(), "verification badge count bound");

            Check(forecastsRoot.Q<VisualElement>("simulation-body").style.display == DisplayStyle.None, "simulation body collapsed by default");
            Check(forecastsRoot.Q<Button>("simulation-toggle-button").text == "Simuler un scénario", "simulation toggle shows its initial label");

            Check(forecastsRoot.Q<VisualElement>("occurrence-form-body").style.display == DisplayStyle.None, "one-off occurrence form collapsed by default");
            Check(forecastsRoot.Q<Button>("occurrence-toggle-button").text == "+ Nouvelle occurrence", "occurrence toggle shows its initial label");
            Check(forecastsRoot.Q<Button>("occurrence-toggle-button").enabledSelf, "occurrence toggle enabled once an account is selected");
            Check(forecastsRoot.Q<DropdownField>("occurrence-category").choices.Count == forecastsViewModel.Categories.Count + 1,
                "occurrence category dropdown carries every active category plus the \"Aucune\" placeholder");

            forecastsController.Refresh();
            Check(forecastOccurrencesListView.itemsSource.Count == forecastsViewModel.Occurrences.Count, "refresh re-renders without duplication");

            var oneOffOccurrenceForForecastsScreen = app.ForecastOccurrences.Create(
                account.Id, "Cadeau anniversaire", new DateTime(2026, 9, 18), 15_000);
            forecastsController.Refresh();
            Check(forecastOccurrencesListView.itemsSource.Count == forecastsViewModel.Occurrences.Count + 1,
                "a one-off occurrence created directly through the service appears in the occurrences list after refresh, same path the form's submit button uses");

            var septemberBudget = app.Budget.GetOrCreate(2026, 9);
            app.Budget.UpsertAllocation(septemberBudget.Id, housing.Id, 70_000);

            var savingsCategory = app.Categories.ListActive().First(c => c.Name == "Épargne");
            app.Transactions.CreateManual(
                account.Id, -20_000, "EUR", new DateTime(2026, 9, 12), "Vers Livret Perso", categoryId: savingsCategory.Id);

            var budgetsViewModel = BudgetsViewModelBuilder.Build(app.Budget, app.Categories, 2026, 9);
            Check(budgetsViewModel.BudgetExists, "budget exists for September once created");
            Check(budgetsViewModel.MonthLabel == "Septembre 2026", "month label capitalized");
            Check(budgetsViewModel.Allocations.Count == 1, "one allocation appears in the view model");
            Check(budgetsViewModel.Allocations[0].CategoryName == "Logement", "category resolved by name");
            Check(budgetsViewModel.Overview is not null, "overview built once a budget exists");
            Check(Normalize(budgetsViewModel.Overview!.RemainingToLiveText) == "−40,00 €", "reste à vivre is prévu − réel − engagé (70 000 − 9 000 − 65 000 minor), over budget here");
            Check(budgetsViewModel.ChartGroups.Count == 1, "one bar group appears in the view model, matching the one allocation");
            Check(budgetsViewModel.ChartGroups[0].CategoryName == "Logement", "bar group resolved by category name");
            Check(budgetsViewModel.ChartGroups[0].PlannedMinor == 70_000, "bar group carries the raw planned amount, not display text");

            Check(budgetsViewModel.SavingsEvolution.Count == 6, "six months of savings history, independent of any Budget row existing for them");
            Check(budgetsViewModel.SavingsEvolution[5].MonthLabel == "sept. 2026", "last point is the viewed month, abbreviated");
            Check(budgetsViewModel.SavingsEvolution[5].SavingsMinor == 20_000, "September's savings transaction is reflected in the last point");
            Check(budgetsViewModel.SavingsEvolution[0].MonthLabel == "avr. 2026", "first point is five months before the viewed month");
            Check(budgetsViewModel.SavingsEvolution[0].SavingsMinor == 0, "no savings activity in April in this fixture");

            var dashboardViewModelWithBudget = DashboardViewModelBuilder.Build(app, today);
            Check(Normalize(dashboardViewModelWithBudget!.RemainingToLiveText) == "−40,00 €", "dashboard reflects the same reste à vivre as the budget screen, once a budget exists");
            Check(dashboardViewModelWithBudget.BudgetSummary.Count == 1, "one row in the budget summary, matching the one allocation");
            Check(dashboardViewModelWithBudget.BudgetSummary[0].CategoryName == "Logement", "budget summary row resolved by category name");
            Check(Normalize(dashboardViewModelWithBudget.BudgetSummary[0].ActualText) == "90,00 €", "réel formatted correctly (9 000 minor)");
            Check(Normalize(dashboardViewModelWithBudget.BudgetSummary[0].PlannedText) == "700,00 €", "prévu formatted correctly (70 000 minor)");
            Check(dashboardViewModelWithBudget.BudgetSummary[0].IsOverBudget, "réel (90€) + engagé (650€) already exceeds prévu (700€)");
            Check(Normalize(dashboardViewModelWithBudget.BudgetSummary[0].NoteText) == "Dépassement de 40,00 €", "note phrases the overage rather than showing a raw signed amount");
            var spentRatio = dashboardViewModelWithBudget.BudgetSummary[0].SpentRatio;
            Check(spentRatio > 0.12f && spentRatio < 0.13f, "spent ratio is réel-only over prévu (9 000 / 70 000), not réel+engagé");

            controller.Render(dashboardViewModelWithBudget);
            Check(root.Q<Label>("budget-summary-empty").style.display == DisplayStyle.None, "budget-summary empty-state hidden once a budget exists");
            Check(root.Q<VisualElement>("budget-summary-list").childCount == 1, "one budget-summary row rendered");
            var budgetSummaryFill = root.Q<VisualElement>(className: "budget-summary-bar-fill");
            Check(budgetSummaryFill is not null, "budget-summary bar fill element bound");
            Check(budgetSummaryFill!.ClassListContains("budget-summary-bar-fill-over"), "over-budget row's bar fill carries the over-budget class");

            Check(dashboardViewModelWithBudget.Alerts.Count == 1, "the over-budget Logement category raises exactly one alert — the balance is still well above the low-balance threshold");
            Check(dashboardViewModelWithBudget.Alerts[0].IsSevere, "a budget overrun is a severe alert");
            Check(dashboardViewModelWithBudget.Alerts[0].Message == "Dépassement de budget : Logement — Dépassement de 40,00 €.", "alert message composed from the category name and the already-computed note text");

            Check(root.Q<Label>("alerts-empty").style.display == DisplayStyle.None, "alerts empty-state hidden once an alert exists");
            var alertRow = root.Q<VisualElement>("alerts-list").Children().First();
            Check(alertRow.Q<VisualElement>(className: "alert-dot")!.ClassListContains("alert-dot-severe"), "severe alert's dot carries the severe class");

            // Isolated fixture for "solde faible": the shared scenario's account balance never
            // dips anywhere near the 200,00 € default threshold, so exercising this alert needs
            // its own small account rather than trying to engineer the shared one into a dip.
            var lowBalancePath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-lowbalance-{Guid.NewGuid():N}.db");
            using (var lowBalanceApp = new AppContainer(lowBalancePath))
            {
                lowBalanceApp.Categories.SeedDefaultCategoriesIfEmpty();
                var lowBalanceAccount = lowBalanceApp.Accounts.CreateAccount("Compte fragile", AccountType.Current, "EUR", 5_000);
                lowBalanceApp.Accounts.RecordOfficialBalance(lowBalanceAccount.Id, 5_000, today);

                var lowBalanceViewModel = DashboardViewModelBuilder.Build(lowBalanceApp, today);
                Check(lowBalanceViewModel!.Alerts.Count == 1, "a 50,00 € balance is below the 200,00 € default low-balance threshold");
                Check(!lowBalanceViewModel.Alerts[0].IsSevere, "a low projected balance is a heads-up, not yet a problem");
                Check(lowBalanceViewModel.Alerts[0].Message.StartsWith("Solde faible : 50,00 €"), "alert message states the actual projected low balance");
            }

            TryDeleteQuietly(lowBalancePath);

            var octoberViewModel = BudgetsViewModelBuilder.Build(app.Budget, app.Categories, 2026, 10);
            Check(!octoberViewModel.BudgetExists, "no budget exists yet for a month never created");
            Check(octoberViewModel.Overview is null, "no overview without a budget");
            Check(octoberViewModel.UnallocatedCategories.Count == 10, "all active categories are unallocated for a fresh month");

            var budgetsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BudgetsUxmlPath);
            if (budgetsTree == null)
            {
                throw new FileNotFoundException($"Budgets UXML not found at {BudgetsUxmlPath}");
            }

            var budgetsRoot = budgetsTree.Instantiate();
            var budgetsController = new BudgetsController(budgetsRoot, app.Budget, app.Categories, today);

            Check(budgetsRoot.Q<Label>("month-label").text == "Septembre 2026", "controller shows September's label by default");
            Check(budgetsRoot.Q<VisualElement>("empty-state-card").style.display == DisplayStyle.None, "empty-state hidden when a budget exists");
            Check(budgetsRoot.Q<VisualElement>("overview-card").style.display == DisplayStyle.Flex, "overview shown when a budget exists");

            var allocationsListView = budgetsRoot.Q<MultiColumnListView>("allocations-list-view");
            Check(allocationsListView.itemsSource.Count == 1, "controller renders the one seeded allocation");
            Check(allocationsListView.columns.Count == 5, "five allocation columns configured");

            Check(budgetsRoot.Q<VisualElement>("chart-card").style.display == DisplayStyle.Flex, "bar chart card shown when a budget exists");
            var budgetChart = budgetsRoot.Q<BudgetBarChartElement>();
            Check(budgetChart is not null, "bar chart element added to the chart card");
            Check(budgetChart!.Groups.Count == budgetsViewModel.ChartGroups.Count, "bar chart element receives every bar group");
            Check(budgetChart.style.flexGrow.value == 1f, "bar chart element grows to fill its fixed-height container, same requirement as the cash-flow chart");
            Check(budgetChart.Q<Label>(className: "chart-tooltip") is not null, "budget bar chart's hover tooltip label exists");
            Check(budgetChart.Q<Label>(className: "chart-tooltip")!.style.display == DisplayStyle.None, "budget bar chart's hover tooltip is hidden by default");

            Check(!budgetChart.DarkTheme, "budget bar chart defaults to light theme when isDarkTheme is omitted");
            var darkBudgetsRoot = budgetsTree.Instantiate();
            _ = new BudgetsController(darkBudgetsRoot, app.Budget, app.Categories, today, isDarkTheme: true);
            Check(darkBudgetsRoot.Q<BudgetBarChartElement>()!.DarkTheme, "isDarkTheme: true reaches the budget bar chart");
            Check(darkBudgetsRoot.Q<SavingsEvolutionElement>()!.DarkTheme, "isDarkTheme: true reaches the savings-evolution chart too");

            // 300x200: one group (Logement, planned 70 000 / actual 9 000 / committed 65 000 minor
            // at this point in the scenario), groupWidth 300, drawableHeight 170 (200-12-18).
            // Prévu is the tallest bar (it's the max), so any y in-bounds hits it; Réel is short
            // (≈22px tall out of 170), so the same x with a y above its drawn top must miss.
            var budgetGroups = budgetChart.Groups;
            Check(BudgetBarChartElement.FindBarUnderPointer(budgetGroups, 300, 200, 50, 100) == (0, 0), "pointer over the tall Prévu bar hits group 0 / série 0");
            Check(BudgetBarChartElement.FindBarUnderPointer(budgetGroups, 300, 200, 150, 170) == (0, 1), "pointer low enough over the short Réel bar hits group 0 / série 1");
            Check(BudgetBarChartElement.FindBarUnderPointer(budgetGroups, 300, 200, 150, 100) is null, "pointer above the short Réel bar's actual drawn height hits nothing");
            Check(BudgetBarChartElement.FindBarUnderPointer(budgetGroups, 300, 200, 101, 100) is null, "pointer in the gap between two bars hits nothing");
            Check(BudgetBarChartElement.FindBarUnderPointer(budgetGroups, 300, 200, 350, 100) is null, "pointer past the only group hits nothing");

            Check(budgetsRoot.Q<VisualElement>("savings-chart-card").style.display == DisplayStyle.Flex, "savings chart card shown when a budget exists");
            var savingsChart = budgetsRoot.Q<SavingsEvolutionElement>();
            Check(savingsChart is not null, "savings chart element added to the savings chart card");
            Check(savingsChart!.Points.Count == budgetsViewModel.SavingsEvolution.Count, "savings chart element receives every month's point");
            Check(savingsChart.style.flexGrow.value == 1f, "savings chart element grows to fill its fixed-height container, same requirement as the other two charts");
            Check(savingsChart.Q<Label>(className: "chart-tooltip") is not null, "savings chart's hover tooltip label exists");
            Check(savingsChart.Q<Label>(className: "chart-tooltip")!.style.display == DisplayStyle.None, "savings chart's hover tooltip is hidden by default");

            // 600 wide over 6 months: 100px/slot. Only September (index 5, the last one) has real
            // savings in this scenario — every other month's bar isn't drawn at all (zero value),
            // so hovering its slot must miss even though the x/y would otherwise look plausible.
            var savingsPoints = savingsChart.Points;
            Check(SavingsEvolutionElement.FindBarUnderPointer(savingsPoints, 600, 200, 550, 100) == 5, "pointer over September's bar hits point index 5");
            Check(SavingsEvolutionElement.FindBarUnderPointer(savingsPoints, 600, 200, 50, 100) is null, "pointer over April's slot hits nothing — its bar has zero savings, nothing drawn there");
            Check(SavingsEvolutionElement.FindBarUnderPointer(savingsPoints, 600, 200, 505, 100) is null, "pointer in the gap next to September's bar hits nothing");
            Check(SavingsEvolutionElement.FindBarUnderPointer(savingsPoints, 600, 200, 700, 100) is null, "pointer past the last month's slot hits nothing");
            Check(budgetsRoot.Q<Label>("savings-empty").style.display == DisplayStyle.None, "savings empty-state hidden once September has real savings activity");
            Check(budgetsRoot.Q<VisualElement>("savings-chart-container").style.display == DisplayStyle.Flex, "savings chart container shown once September has real savings activity");

            // A month whose own 6-month window (March–August) contains none of the September
            // savings transaction above — the "budget exists but literally zero savings
            // everywhere" case a real screenshot showed as a blank, message-less chart area.
            app.Budget.GetOrCreate(2026, 8);
            var augustRoot = budgetsTree.Instantiate();
            _ = new BudgetsController(augustRoot, app.Budget, app.Categories, new DateTime(2026, 8, 15));
            Check(augustRoot.Q<Label>("savings-empty").style.display == DisplayStyle.Flex, "savings empty-state shown when every month in the window has zero savings");
            Check(augustRoot.Q<VisualElement>("savings-chart-container").style.display == DisplayStyle.None, "savings chart container hidden when every month in the window has zero savings");

            budgetsController.Refresh();
            Check(allocationsListView.itemsSource.Count == 1, "refresh re-renders without duplication");

            var emptyBudgetsRoot = budgetsTree.Instantiate();
            _ = new BudgetsController(emptyBudgetsRoot, app.Budget, app.Categories, new DateTime(2026, 10, 15));
            Check(emptyBudgetsRoot.Q<VisualElement>("empty-state-card").style.display == DisplayStyle.Flex, "empty-state shown for a month with no budget yet");
            Check(emptyBudgetsRoot.Q<VisualElement>("overview-card").style.display == DisplayStyle.None, "overview hidden for a month with no budget yet");
            Check(emptyBudgetsRoot.Q<VisualElement>("chart-card").style.display == DisplayStyle.None, "bar chart card hidden for a month with no budget yet");
            Check(emptyBudgetsRoot.Q<BudgetBarChartElement>()!.Groups.Count == 0, "bar chart has no groups with no budget — constructing it did not throw");
            Check(emptyBudgetsRoot.Q<VisualElement>("savings-chart-card").style.display == DisplayStyle.None, "savings chart card hidden for a month with no budget yet");
            Check(emptyBudgetsRoot.Q<SavingsEvolutionElement>()!.Points.Count == 0, "savings chart has no points with no budget — constructing it did not throw");

            var restaurants = app.Categories.Create("Restaurants", CategoryType.Expense, parentId: groceries.Id);

            var categoriesViewModel = CategoriesViewModelBuilder.Build(app.Categories);
            Check(categoriesViewModel.Categories.Count == 11, "ten seeded defaults plus the new subcategory");
            var categoriesList = categoriesViewModel.Categories.ToList();
            var restaurantsRow = categoriesList.First(c => c.Name == "Restaurants");
            Check(restaurantsRow.ParentId == groceries.Id, "subcategory's parent id carried through");
            Check(restaurantsRow.ParentText == "Alimentation", "subcategory's parent name resolved");
            Check(!restaurantsRow.IsSystem, "a user-created category is not a system category");
            var groceriesIndex = categoriesList.FindIndex(c => c.Name == "Alimentation");
            var restaurantsIndex = categoriesList.FindIndex(c => c.Name == "Restaurants");
            Check(restaurantsIndex == groceriesIndex + 1, "a subcategory sorts immediately after its parent");
            Check(categoriesViewModel.ParentOptions.All(o => o.Id != restaurants.Id), "a category that is itself a subcategory can never be offered as a parent");

            var thirdLevelBlocked = false;
            try
            {
                app.Categories.Create("Fast-food", CategoryType.Expense, parentId: restaurants.Id);
            }
            catch (ArgumentException)
            {
                thirdLevelBlocked = true;
            }

            Check(thirdLevelBlocked, "a subcategory cannot itself have a subcategory (deux niveaux maximum)");

            var parentWithChildrenBlocked = false;
            try
            {
                app.Categories.MoveUnder(groceries.Id, housing.Id);
            }
            catch (ArgumentException)
            {
                parentWithChildrenBlocked = true;
            }

            Check(parentWithChildrenBlocked, "a category with existing subcategories cannot itself become a subcategory");

            var selfParentBlocked = false;
            try
            {
                app.Categories.MoveUnder(housing.Id, housing.Id);
            }
            catch (ArgumentException)
            {
                selfParentBlocked = true;
            }

            Check(selfParentBlocked, "a category cannot be its own parent");

            var systemArchiveBlocked = false;
            try
            {
                app.Categories.Archive(housing.Id);
            }
            catch (InvalidOperationException)
            {
                systemArchiveBlocked = true;
            }

            Check(systemArchiveBlocked, "a system category cannot be archived");

            var categoriesTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CategoriesUxmlPath);
            if (categoriesTree == null)
            {
                throw new FileNotFoundException($"Categories UXML not found at {CategoriesUxmlPath}");
            }

            var categoriesRoot = categoriesTree.Instantiate();
            var categoriesController = new CategoriesController(categoriesRoot, app.Categories);

            var categoriesListView = categoriesRoot.Q<MultiColumnListView>("categories-list-view");
            Check(categoriesListView.itemsSource.Count == 11, "categories controller renders every category, including the new subcategory");
            Check(categoriesRoot.Q<Button>("new-category-button-list") is not null, "the list-adjacent new-category button exists (ADR-142)");
            Check(categoriesListView.columns.Count == 4, "four category columns configured");
            Check(categoriesRoot.Q<Label>("categories-empty").style.display == DisplayStyle.None, "empty-state hidden when categories exist");

            categoriesController.Refresh();
            Check(categoriesListView.itemsSource.Count == 11, "refresh re-renders without duplication");

            var settingsViewModel = SettingsViewModelBuilder.Build(app.Settings, app.Accounts, app.DatabasePath);
            Check(settingsViewModel.Currency == "EUR", "currency is fixed EUR in V1");
            Check(settingsViewModel.ForecastHorizonDays == 90, "default forecast horizon before any change");
            Check(settingsViewModel.MissedThresholdDays == 15, "default missed threshold before any change");
            Check(settingsViewModel.DatabasePath == app.DatabasePath, "database path passed through");
            Check(settingsViewModel.Accounts.Count == 2, "the two active accounts are listed for the default-account picker");

            app.Settings.UpdateForecastHorizon(45);
            app.Settings.SetDefaultCurrentAccount(newSavings.Id);
            var updatedSettingsViewModel = SettingsViewModelBuilder.Build(app.Settings, app.Accounts, app.DatabasePath);
            Check(updatedSettingsViewModel.ForecastHorizonDays == 45, "forecast horizon reflects the update");
            Check(updatedSettingsViewModel.DefaultCurrentAccountId == newSavings.Id, "default account reflects the update");

            // Every account-choice dropdown that assigns an account to something new (not a
            // filter, where "Tous les comptes" is the correct neutral default) should now open
            // preselected to the user's configured default account instead of just the first
            // choice in the list. Refresh first — newSavings ("Livret Perso") was created before
            // transactionsController's last Refresh(), so its own _creatableAccounts cache would
            // otherwise still be stale.
            transactionsController.Refresh();
            transactionsController.OpenCreateForm();
            Check(transactionsRoot.Q<DropdownField>("form-account").value == "Livret Perso",
                "the transaction form's account field opens preselected to the default account, not just the first one");

            recurringOperationsController.Refresh();
            recurringOperationsController.OpenCreateForm();
            Check(recurringOperationsRoot.Q<DropdownField>("form-source-account").value == "Livret Perso",
                "the recurring-operation form's source-account field opens preselected to the default account");

            // ADR-145 (essai) : la date de début est maintenant un Button (déclenche le DatePicker
            // App UI au clic, non simulable en batchmode) plutôt qu'un TextField — vérifie
            // seulement qu'OpenCreateForm en fixe bien le texte à aujourd'hui, pas le placeholder.
            Check(recurringOperationsRoot.Q<Button>("form-start-date").text != "jj/mm/aaaa",
                "the start-date trigger shows today's date, not the placeholder, once the create form opens");

            var settingsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SettingsUxmlPath);
            if (settingsTree == null)
            {
                throw new FileNotFoundException($"Settings UXML not found at {SettingsUxmlPath}");
            }

            var settingsRoot = settingsTree.Instantiate();
            var settingsController = new SettingsController(settingsRoot, app.Settings, app.Accounts, app.Backup, app.DatabasePath);

            Check(settingsRoot.Q<Label>("currency-value").text == "EUR", "currency label bound");
            Check(settingsRoot.Q<TextField>("horizon-field").value == "45", "horizon field reflects the persisted setting");
            Check(settingsRoot.Q<Label>("database-path-value").text == app.DatabasePath, "database path label bound");
            Check(settingsRoot.Q<DropdownField>("default-account-field").choices.Count == 3, "default-account dropdown has 'Aucun' plus each active account");

            settingsController.Refresh();
            Check(settingsRoot.Q<TextField>("horizon-field").value == "45", "refresh re-renders without changing the value");

            Check(settingsRoot.Q<DropdownField>("theme-field").choices.Count == 2, "theme dropdown offers exactly Clair/Sombre");
            Check(settingsRoot.Q<DropdownField>("theme-field").value == "Clair", "light theme shown by default");

            var themeChangedCallCount = 0;
            var lastThemeChangedTo = AppTheme.Light;
            var themeSettingsRoot = settingsTree.Instantiate();
            var themeController = new SettingsController(
                themeSettingsRoot, app.Settings, app.Accounts, app.Backup, app.DatabasePath,
                onThemeChanged: theme =>
                {
                    themeChangedCallCount++;
                    lastThemeChangedTo = theme;
                });
            themeSettingsRoot.Q<DropdownField>("theme-field").SetValueWithoutNotify("Sombre");
            themeController.OnThemeFieldChanged();
            Check(app.Settings.Get().Theme == AppTheme.Dark, "selecting Sombre persists immediately, not gated behind Enregistrer");
            Check(themeChangedCallCount == 1 && lastThemeChangedTo == AppTheme.Dark, "the onThemeChanged callback fires with the new theme");

            themeSettingsRoot.Q<DropdownField>("theme-field").SetValueWithoutNotify("Clair");
            themeController.OnThemeFieldChanged();
            Check(app.Settings.Get().Theme == AppTheme.Light, "selecting Clair reverts it, same live behavior");

            var noAccountPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-empty-{Guid.NewGuid():N}.db");
            using (var noAccountApp = new AppContainer(noAccountPath))
            {
                Check(DashboardViewModelBuilder.Build(noAccountApp, today) is null, "no account yields a null view model, not a crash");

                var emptyAccountsRoot = accountsTree.Instantiate();
                _ = new AccountsController(emptyAccountsRoot, noAccountApp.Accounts, noAccountApp.Settings);
                Check(emptyAccountsRoot.Q<VisualElement>("accounts-list").childCount == 0, "no accounts renders an empty list");
                Check(emptyAccountsRoot.Q<Label>("accounts-empty").style.display == DisplayStyle.Flex, "empty-state shown when no accounts exist");
                Check(emptyAccountsRoot.Q<Button>("new-account-button-list") is not null, "the list-adjacent new-account button exists (ADR-142)");

                var emptyTransactionsRoot = transactionsTree.Instantiate();
                _ = new TransactionsController(
                    emptyTransactionsRoot, noAccountApp.Accounts, noAccountApp.Categories, noAccountApp.Counterparties,
                    noAccountApp.Transactions, noAccountApp.InternalTransfers, noAccountApp.TransferDetection, noAccountApp.Settings);
                Check(emptyTransactionsRoot.Q<Label>("transactions-empty").style.display == DisplayStyle.Flex, "empty-state shown when no transactions exist");
                Check(!emptyTransactionsRoot.Q<Button>("new-transaction-button").enabledSelf, "new-transaction button disabled with no account to post against");
                Check(!emptyTransactionsRoot.Q<Button>("new-transaction-button-list").enabledSelf,
                    "the list-adjacent new-transaction button (ADR-142) mirrors the same disabled state");

                var emptyRecurringOperationsRoot = recurringOperationsTree.Instantiate();
                _ = new RecurringOperationsController(
                    emptyRecurringOperationsRoot, noAccountApp.Accounts, noAccountApp.Categories, noAccountApp.Counterparties,
                    noAccountApp.RecurringOperations, noAccountApp.Settings);
                Check(emptyRecurringOperationsRoot.Q<Label>("operations-empty").style.display == DisplayStyle.Flex, "empty-state shown when no operations exist");
                Check(!emptyRecurringOperationsRoot.Q<Button>("new-operation-button").enabledSelf, "new-operation button disabled with no account to post against");
                Check(!emptyRecurringOperationsRoot.Q<Button>("new-operation-button-list").enabledSelf,
                    "the list-adjacent new-operation button (ADR-142) mirrors the same disabled state");

                var emptyForecastsRoot = forecastsTree.Instantiate();
                _ = new ForecastsController(emptyForecastsRoot, noAccountApp);
                Check(emptyForecastsRoot.Q<Label>("kpi-current-value").text == "—", "empty synthesis falls back to placeholders");
                Check(!emptyForecastsRoot.Q<Button>("simulation-run-button").enabledSelf, "simulation disabled with no account");
                Check(emptyForecastsRoot.Q<DropdownField>("forecast-account-select").value == "Aucun compte", "account selector shows a placeholder instead of rendering blank");
                Check(!emptyForecastsRoot.Q<DropdownField>("forecast-account-select").enabledSelf, "account selector disabled with no account to choose");
                Check(emptyForecastsRoot.Q<LineChartElement>()!.Points.Count == 0, "chart has no points with no account — constructing it did not throw");

                var emptySettingsRoot = settingsTree.Instantiate();
                _ = new SettingsController(
                    emptySettingsRoot, noAccountApp.Settings, noAccountApp.Accounts, noAccountApp.Backup, noAccountApp.DatabasePath);
                Check(emptySettingsRoot.Q<DropdownField>("default-account-field").choices.Count == 1, "only 'Aucun (automatique)' when no accounts exist");

                // noAccountApp never calls SeedDefaultCategoriesIfEmpty, so this is also the
                // "zero categories at all" case — distinct from every other screen's empty state,
                // which is about missing accounts, not missing categories.
                var emptyCategoriesRoot = categoriesTree.Instantiate();
                _ = new CategoriesController(emptyCategoriesRoot, noAccountApp.Categories);
                Check(emptyCategoriesRoot.Q<Label>("categories-empty").style.display == DisplayStyle.Flex, "empty-state shown when no categories exist");
                Check(emptyCategoriesRoot.Q<MultiColumnListView>("categories-list-view").style.display == DisplayStyle.None, "categories table hidden when no categories exist");
            }

            TryDeleteQuietly(noAccountPath);

            var onboardingPath = Path.Combine(Path.GetTempPath(), $"financeos-ui-smoke-onboarding-{Guid.NewGuid():N}.db");
            using (var onboardingApp = new AppContainer(onboardingPath))
            {
                var freshOnboardingViewModel = OnboardingViewModelBuilder.Build(onboardingApp.Accounts, onboardingApp.RecurringOperations, onboardingApp.Categories);
                Check(freshOnboardingViewModel.Accounts.Count == 0, "a fresh onboarding scenario starts with no accounts");
                Check(freshOnboardingViewModel.Operations.Count == 0, "a fresh onboarding scenario starts with no operations");

                var onboardingTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(OnboardingUxmlPath);
                if (onboardingTree == null)
                {
                    throw new FileNotFoundException($"Onboarding UXML not found at {OnboardingUxmlPath}");
                }

                var onboardingRoot = onboardingTree.Instantiate();
                var onboardingFinishedCalls = 0;
                var onboardingController = new OnboardingController(
                    onboardingRoot, onboardingApp.Accounts, onboardingApp.RecurringOperations, onboardingApp.Categories,
                    () => onboardingFinishedCalls++);

                Check(onboardingRoot.Q<VisualElement>("step-welcome").style.display == DisplayStyle.Flex, "welcome step shown first");
                Check(onboardingRoot.Q<VisualElement>("step-account").style.display == DisplayStyle.None, "account step hidden initially");
                Check(onboardingRoot.Q<Label>("step-indicator").text == "Étape 1 sur 4", "step indicator starts at step 1");

                onboardingController.GoToStep(1);
                Check(onboardingRoot.Q<VisualElement>("step-welcome").style.display == DisplayStyle.None, "welcome step hidden after advancing");
                Check(onboardingRoot.Q<VisualElement>("step-account").style.display == DisplayStyle.Flex, "account step shown");
                Check(onboardingRoot.Q<Label>("step-indicator").text == "Étape 2 sur 4", "step indicator advances to step 2");

                onboardingController.TryAdvanceFromAccountStep();
                Check(onboardingRoot.Q<VisualElement>("step-recurring").style.display == DisplayStyle.None, "cannot advance past the account step with no account created");
                Check(onboardingRoot.Q<Label>("account-next-error").style.display == DisplayStyle.Flex, "error shown when trying to advance with no account");

                onboardingRoot.Q<TextField>("account-name-field").value = "Compte courant";
                onboardingRoot.Q<DropdownField>("account-type-field").value = "Épargne";
                onboardingRoot.Q<TextField>("account-balance-field").value = "1500";
                onboardingController.AddAccount();

                Check(onboardingApp.Accounts.ListAll().Count == 1, "the account was actually persisted");
                var createdAccount = onboardingApp.Accounts.ListAll()[0];
                Check(createdAccount.Name == "Compte courant", "account name carried through");
                Check(createdAccount.Type == AccountType.Savings, "account type carried through");
                Check(createdAccount.OfficialBalanceMinor == 150_000, "account balance carried through (1500,00 €)");
                Check(onboardingRoot.Q<VisualElement>("account-list").childCount == 1, "the newly added account is rendered in the step's own list");
                Check(string.IsNullOrEmpty(onboardingRoot.Q<TextField>("account-name-field").value), "name field clears after a successful add, ready for another account");

                // ADR-142: a mistakenly-added account can now be removed before finishing —
                // add a second one, confirm it renders, remove it, confirm it's gone and the
                // original survives untouched (every assertion below this point still expects
                // exactly one account, unchanged from before this block).
                onboardingRoot.Q<TextField>("account-name-field").value = "Compte à retirer";
                onboardingRoot.Q<DropdownField>("account-type-field").value = "Courant";
                onboardingRoot.Q<TextField>("account-balance-field").value = "10";
                onboardingController.AddAccount();
                Check(onboardingApp.Accounts.ListAll().Count == 2, "a second account was persisted");
                Check(onboardingRoot.Q<VisualElement>("account-list").childCount == 2, "both accounts render in the step's own list");

                var accountToRemove = onboardingApp.Accounts.ListAll().First(a => a.Name == "Compte à retirer");
                onboardingController.RemoveAccount(accountToRemove.Id);
                Check(onboardingApp.Accounts.FindById(accountToRemove.Id)!.IsArchived,
                    "removing an account archives it rather than deleting it — no hard-delete path exists for accounts anywhere in this app");
                Check(onboardingRoot.Q<VisualElement>("account-list").childCount == 1, "the removed account no longer renders in the step's own list");

                // Created before advancing to the recurring-operations step so that step's own
                // Refresh() (triggered by TryAdvanceFromAccountStep below) picks it up when
                // building the category dropdown's choices — the same live-rebuild-on-navigation
                // pattern the account dropdown already relies on.
                var salaryCategory = onboardingApp.Categories.Create("Salaire", CategoryType.Income);

                onboardingController.TryAdvanceFromAccountStep();
                Check(onboardingRoot.Q<Label>("account-next-error").style.display == DisplayStyle.None, "no error once an account exists");
                Check(onboardingRoot.Q<VisualElement>("step-recurring").style.display == DisplayStyle.Flex, "advances to the recurring-operations step now that an account exists");
                Check(onboardingRoot.Q<Button>("recurring-next-button").text == "Passer", "the advance button reads 'Passer' with nothing added yet — this step is optional");

                // The date and category fields were both missing before this pass — every
                // onboarding operation silently started "today" with no category at all. The date
                // trigger can't be click-simulated here (same live-panel limitation as every other
                // AppDatePickerField button in this project), so only its default pre-fill is
                // checked; category selection works fine since DropdownField.value can be set
                // directly without a panel.
                Check(onboardingRoot.Q<Button>("operation-date-field").text != "jj/mm/aaaa",
                    "operation date field is pre-filled with today's date, not the placeholder");

                onboardingRoot.Q<TextField>("operation-name-field").value = "Salaire";
                onboardingRoot.Q<DropdownField>("operation-type-field").value = "Revenu";
                onboardingRoot.Q<TextField>("operation-amount-field").value = "2500";
                onboardingRoot.Q<DropdownField>("operation-category-field").value = "Salaire";
                onboardingController.AddOperation();

                Check(onboardingApp.RecurringOperations.ListAll().Count == 1, "the recurring operation was actually persisted");
                var createdOperation = onboardingApp.RecurringOperations.ListAll()[0];
                Check(createdOperation.Name == "Salaire", "operation name carried through");
                Check(createdOperation.Type == RecurringOperationType.Income, "operation type carried through");
                Check(createdOperation.DestinationAccountId == createdAccount.Id, "income targets the account created in step 2 as its destination");
                Check(createdOperation.ExpectedAmountMinor == 250_000, "operation amount carried through (2500,00 €)");
                Check(createdOperation.Frequency == RecurringFrequency.Monthly, "onboarding always creates monthly operations — no frequency field to keep the form minimal");
                Check(createdOperation.StartDate.Date == DateTime.Now.Date, "operation start date defaults to today, now editable via the same App UI DatePicker as every other date field");
                Check(createdOperation.CategoryId == salaryCategory.Id, "operation category is now carried through — previously there was no field to set it at all");
                Check(onboardingRoot.Q<Button>("recurring-next-button").text == "Suivant", "the advance button relabels to 'Suivant' once something has been added");
                Check(onboardingRoot.Q<Button>("operation-date-field").text == DateFormat.ForInput(DateTime.Now),
                    "date field resets to today after a successful add, same reset pattern as name/amount");
                Check(onboardingRoot.Q<DropdownField>("operation-category-field").value == "Aucune",
                    "category field resets to Aucune after a successful add, same reset pattern as the account field");

                // Same removal coverage for a mistakenly-added recurring operation — a real
                // delete this time (RecurringOperationService.Delete), safe here since no
                // occurrence is ever generated during onboarding itself.
                onboardingRoot.Q<TextField>("operation-name-field").value = "Opération à retirer";
                onboardingRoot.Q<DropdownField>("operation-type-field").value = "Dépense";
                onboardingRoot.Q<TextField>("operation-amount-field").value = "50";
                onboardingController.AddOperation();
                Check(onboardingApp.RecurringOperations.ListAll().Count == 2, "a second operation was persisted");
                Check(onboardingRoot.Q<VisualElement>("operation-list").childCount == 2, "both operations render in the step's own list");

                var operationToRemove = onboardingApp.RecurringOperations.ListAll().First(o => o.Name == "Opération à retirer");
                onboardingController.RemoveOperation(operationToRemove.Id);
                Check(onboardingApp.RecurringOperations.FindById(operationToRemove.Id) is null, "removing an operation actually deletes it");
                Check(onboardingRoot.Q<VisualElement>("operation-list").childCount == 1, "the removed operation no longer renders in the step's own list");

                onboardingController.GoToStep(3);
                Check(onboardingRoot.Q<Label>("step-indicator").text == "Étape 4 sur 4", "step indicator reaches the last step");
                Check(onboardingRoot.Q<VisualElement>("recap-account-list").childCount == 1, "recap lists the account created earlier");
                Check(onboardingRoot.Q<VisualElement>("recap-operation-list").childCount == 1, "recap lists the operation created earlier");
                Check(onboardingRoot.Q<Label>("recap-operations-empty").style.display == DisplayStyle.None, "operations empty-state hidden once one was added");

                onboardingController.Finish();
                Check(onboardingFinishedCalls == 1, "finishing the flow calls the completion callback exactly once");
            }

            TryDeleteQuietly(onboardingPath);
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
            var forecastsCalls = 0;
            var budgetsCalls = 0;
            var categoriesCalls = 0;
            var settingsCalls = 0;
            var quitCalls = 0;
            var shell = new ShellController(
                root, () => dashboardCalls++, () => accountsCalls++, () => transactionsCalls++,
                () => recurringOperationsCalls++, () => forecastsCalls++, () => budgetsCalls++,
                () => categoriesCalls++, () => settingsCalls++, () => quitCalls++);

            var content = new VisualElement();
            shell.SetContent(content);

            shell.SetSidebarVisible(false);
            Check(root.Q<VisualElement>("sidebar").style.display == DisplayStyle.None, "sidebar hidden during onboarding");
            shell.SetSidebarVisible(true);
            Check(root.Q<VisualElement>("sidebar").style.display == DisplayStyle.Flex, "sidebar shown again once onboarding finishes");
            Check(root.Q<VisualElement>("content-area").childCount == 1, "shell content area receives the screen's content");

            Check(root.Q<Button>("quit-button") is not null, "quit button is bound (ADR-138) — a reliable way to close the app regardless of window chrome");

            Check(root.Q<VisualElement>("update-ready-overlay").style.display == DisplayStyle.None, "update-ready overlay hidden by default (ADR-139)");
            shell.ShowUpdateReady("9.9.9", () => { }, () => { });
            Check(root.Q<VisualElement>("update-ready-overlay").style.display == DisplayStyle.Flex, "ShowUpdateReady reveals the overlay");
            Check(root.Q<Label>("update-ready-message").text.Contains("9.9.9"), "the overlay message names the new version");
            shell.HideUpdateReady();
            Check(root.Q<VisualElement>("update-ready-overlay").style.display == DisplayStyle.None, "HideUpdateReady hides it again");

            // UpdateChecker's actual network/coroutine flow can't run here (no real HTTP in
            // batchmode, and it shouldn't depend on live internet either way) — but its decision
            // logic is deliberately pure and separated out specifically so it's still verifiable.
            Check(UpdateChecker.IsNewerVersion("v1.2.0", "1.0.1"), "a later tagged release counts as newer");
            Check(UpdateChecker.IsNewerVersion("1.2.0", "1.0.1"), "the leading 'v' on a release tag is optional");
            Check(!UpdateChecker.IsNewerVersion("1.0.1", "1.0.1"), "the same version is never \"newer\"");
            Check(!UpdateChecker.IsNewerVersion("1.0.0", "1.0.1"), "an older tag is never newer");
            Check(!UpdateChecker.IsNewerVersion("not-a-version", "1.0.1"), "a malformed tag is treated as not-newer rather than throwing");
            Check(!UpdateChecker.IsNewerVersion("1.2.0", "not-a-version"), "a malformed current version is treated as not-newer rather than throwing");

            var installerAsset = new ReleaseAssetInfo("FinanceOS-Setup-1.2.0.exe", "https://example.invalid/FinanceOS-Setup-1.2.0.exe");
            var sourceZipAsset = new ReleaseAssetInfo("Source code.zip", "https://example.invalid/source.zip");
            Check(UpdateChecker.FindInstallerDownloadUrl(new[] { sourceZipAsset, installerAsset }) == installerAsset.Url,
                "the .exe asset is picked regardless of its position among a release's assets");
            Check(UpdateChecker.FindInstallerDownloadUrl(new[] { sourceZipAsset }) is null,
                "a release with no .exe asset yields no download URL rather than picking the wrong file");

            Check(!root.Q<VisualElement>("shell-root").ClassListContains("theme-dark"), "light theme by default — no theme-dark class");
            shell.SetTheme(AppTheme.Dark);
            Check(root.Q<VisualElement>("shell-root").ClassListContains("theme-dark"), "SetTheme(Dark) adds the theme-dark class that redefines every --color-* token");
            shell.SetTheme(AppTheme.Light);
            Check(!root.Q<VisualElement>("shell-root").ClassListContains("theme-dark"), "SetTheme(Light) removes it again");

            shell.SetActive(ShellScreen.Accounts);
            Check(root.Q<Button>("nav-accounts").ClassListContains("nav-item-active"), "accounts nav item marked active");
            Check(!root.Q<Button>("nav-dashboard").ClassListContains("nav-item-active"), "dashboard nav item not active");
            Check(!root.Q<Button>("nav-transactions").ClassListContains("nav-item-active"), "transactions nav item not active");
            Check(!root.Q<Button>("nav-recurring-operations").ClassListContains("nav-item-active"), "recurring operations nav item not active");
            Check(!root.Q<Button>("nav-forecasts").ClassListContains("nav-item-active"), "forecasts nav item not active");

            shell.SetActive(ShellScreen.Transactions);
            Check(root.Q<Button>("nav-transactions").ClassListContains("nav-item-active"), "transactions nav item marked active");
            Check(!root.Q<Button>("nav-accounts").ClassListContains("nav-item-active"), "accounts nav item not active");

            shell.SetActive(ShellScreen.RecurringOperations);
            Check(root.Q<Button>("nav-recurring-operations").ClassListContains("nav-item-active"), "recurring operations nav item marked active");
            Check(!root.Q<Button>("nav-transactions").ClassListContains("nav-item-active"), "transactions nav item not active");

            shell.SetActive(ShellScreen.Forecasts);
            Check(root.Q<Button>("nav-forecasts").ClassListContains("nav-item-active"), "forecasts nav item marked active");
            Check(!root.Q<Button>("nav-recurring-operations").ClassListContains("nav-item-active"), "recurring operations nav item not active");

            shell.SetActive(ShellScreen.Budgets);
            Check(root.Q<Button>("nav-budgets").ClassListContains("nav-item-active"), "budgets nav item marked active");
            Check(!root.Q<Button>("nav-forecasts").ClassListContains("nav-item-active"), "forecasts nav item not active");

            shell.SetActive(ShellScreen.Categories);
            Check(root.Q<Button>("nav-categories").ClassListContains("nav-item-active"), "categories nav item marked active");
            Check(!root.Q<Button>("nav-budgets").ClassListContains("nav-item-active"), "budgets nav item not active");

            shell.SetActive(ShellScreen.Settings);
            Check(root.Q<Button>("nav-settings").ClassListContains("nav-item-active"), "settings nav item marked active");
            Check(!root.Q<Button>("nav-categories").ClassListContains("nav-item-active"), "categories nav item not active");

            shell.SetActive(ShellScreen.Dashboard);
            Check(root.Q<Button>("nav-dashboard").ClassListContains("nav-item-active"), "dashboard nav item marked active");
            Check(!root.Q<Button>("nav-accounts").ClassListContains("nav-item-active"), "accounts nav item not active");
            Check(!root.Q<Button>("nav-transactions").ClassListContains("nav-item-active"), "transactions nav item not active");
            Check(!root.Q<Button>("nav-recurring-operations").ClassListContains("nav-item-active"), "recurring operations nav item not active");
            Check(!root.Q<Button>("nav-forecasts").ClassListContains("nav-item-active"), "forecasts nav item not active");
            Check(!root.Q<Button>("nav-budgets").ClassListContains("nav-item-active"), "budgets nav item not active");
            Check(!root.Q<Button>("nav-categories").ClassListContains("nav-item-active"), "categories nav item not active");
            Check(!root.Q<Button>("nav-settings").ClassListContains("nav-item-active"), "settings nav item not active");

            using (var clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = root.Q<Button>("nav-accounts");
                root.Q<Button>("nav-accounts").SendEvent(clickEvent);
            }

            Debug.Log($"[UISmokeTest] Shell nav click delivery: dashboard={dashboardCalls}, accounts={accountsCalls}, transactions={transactionsCalls}, recurringOperations={recurringOperationsCalls}, forecasts={forecastsCalls}, budgets={budgetsCalls}, settings={settingsCalls} (best-effort without an attached panel).");
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
