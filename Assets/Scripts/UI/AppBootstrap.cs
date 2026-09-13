using System;
using FinanceOS.App;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// The scene's entry point: builds the AppContainer and the shell, then renders the first
    /// screen. Thin by design — everything it does is one call into App or UI, per
    /// docs/02-Architecture.md §4. Screen UXML assets are wired in by SceneWiringTools, not
    /// hardcoded here, so this class stays Editor-tooling-free.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppBootstrap : MonoBehaviour
    {
        public VisualTreeAsset? DashboardAsset;
        public VisualTreeAsset? AccountsAsset;
        public VisualTreeAsset? TransactionsAsset;
        public VisualTreeAsset? RecurringOperationsAsset;
        public VisualTreeAsset? ForecastsAsset;
        public VisualTreeAsset? BudgetsAsset;

        public static AppContainer? Container { get; private set; }

        private ShellController? _shell;
        private DashboardController? _dashboardController;
        private AccountsController? _accountsController;
        private TransactionsController? _transactionsController;
        private RecurringOperationsController? _recurringOperationsController;
        private ForecastsController? _forecastsController;
        private BudgetsController? _budgetsController;

        private void Awake()
        {
            Container = new AppContainer();
            Container.Categories.SeedDefaultCategoriesIfEmpty();

            // Idempotent maintenance, run on every launch so the forecast/verification state is
            // never stale just because the app was closed for a while — see
            // RecurringOperationService.GenerateUpcomingOccurrences and
            // ForecastOccurrenceService.MarkStaleAsMissed.
            var settings = Container.Settings.Get();
            Container.RecurringOperations.GenerateUpcomingOccurrences(DateTime.Now, settings.ForecastHorizonDays);
            Container.ForecastOccurrences.MarkStaleAsMissed(DateTime.Now, settings.MissedThresholdDays);

            var root = GetComponent<UIDocument>().rootVisualElement;
            _shell = new ShellController(
                root, ShowDashboard, ShowAccounts, ShowTransactions, ShowRecurringOperations, ShowForecasts, ShowBudgets);
            ShowDashboard();
        }

        private void ShowDashboard()
        {
            if (Container is null || _shell is null || DashboardAsset is null)
            {
                return;
            }

            var content = DashboardAsset.Instantiate();
            _shell.SetContent(content);
            _dashboardController = new DashboardController(content);
            ClearOtherControllers(keepDashboard: true);
            _shell.SetActive(ShellScreen.Dashboard);

            RefreshDashboard();
        }

        private void ShowAccounts()
        {
            if (Container is null || _shell is null || AccountsAsset is null)
            {
                return;
            }

            var content = AccountsAsset.Instantiate();
            _shell.SetContent(content);
            _accountsController = new AccountsController(content, Container.Accounts);
            ClearOtherControllers(keepAccounts: true);
            _shell.SetActive(ShellScreen.Accounts);
        }

        private void ShowTransactions()
        {
            if (Container is null || _shell is null || TransactionsAsset is null)
            {
                return;
            }

            var content = TransactionsAsset.Instantiate();
            _shell.SetContent(content);
            _transactionsController = new TransactionsController(
                content, Container.Accounts, Container.Categories, Container.Counterparties,
                Container.Transactions, Container.InternalTransfers);
            ClearOtherControllers(keepTransactions: true);
            _shell.SetActive(ShellScreen.Transactions);
        }

        private void ShowRecurringOperations()
        {
            if (Container is null || _shell is null || RecurringOperationsAsset is null)
            {
                return;
            }

            var content = RecurringOperationsAsset.Instantiate();
            _shell.SetContent(content);
            _recurringOperationsController = new RecurringOperationsController(
                content, Container.Accounts, Container.Categories, Container.Counterparties,
                Container.RecurringOperations, Container.Settings);
            ClearOtherControllers(keepRecurringOperations: true);
            _shell.SetActive(ShellScreen.RecurringOperations);
        }

        private void ShowForecasts()
        {
            if (Container is null || _shell is null || ForecastsAsset is null)
            {
                return;
            }

            var content = ForecastsAsset.Instantiate();
            _shell.SetContent(content);
            _forecastsController = new ForecastsController(content, Container);
            ClearOtherControllers(keepForecasts: true);
            _shell.SetActive(ShellScreen.Forecasts);
        }

        private void ShowBudgets()
        {
            if (Container is null || _shell is null || BudgetsAsset is null)
            {
                return;
            }

            var content = BudgetsAsset.Instantiate();
            _shell.SetContent(content);
            _budgetsController = new BudgetsController(content, Container.Budget, Container.Categories);
            ClearOtherControllers(keepBudgets: true);
            _shell.SetActive(ShellScreen.Budgets);
        }

        private void ClearOtherControllers(
            bool keepDashboard = false,
            bool keepAccounts = false,
            bool keepTransactions = false,
            bool keepRecurringOperations = false,
            bool keepForecasts = false,
            bool keepBudgets = false)
        {
            if (!keepDashboard)
            {
                _dashboardController = null;
            }

            if (!keepAccounts)
            {
                _accountsController = null;
            }

            if (!keepTransactions)
            {
                _transactionsController = null;
            }

            if (!keepRecurringOperations)
            {
                _recurringOperationsController = null;
            }

            if (!keepForecasts)
            {
                _forecastsController = null;
            }

            if (!keepBudgets)
            {
                _budgetsController = null;
            }
        }

        private void RefreshDashboard()
        {
            if (Container is null || _dashboardController is null)
            {
                return;
            }

            var viewModel = DashboardViewModelBuilder.Build(Container, DateTime.Now);
            if (viewModel is not null)
            {
                _dashboardController.Render(viewModel);
            }
        }

        private void OnApplicationQuit()
        {
            Container?.Dispose();
            Container = null;
        }
    }
}
