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

        public static AppContainer? Container { get; private set; }

        private ShellController? _shell;
        private DashboardController? _dashboardController;
        private AccountsController? _accountsController;
        private TransactionsController? _transactionsController;

        private void Awake()
        {
            Container = new AppContainer();
            Container.Categories.SeedDefaultCategoriesIfEmpty();

            var root = GetComponent<UIDocument>().rootVisualElement;
            _shell = new ShellController(root, ShowDashboard, ShowAccounts, ShowTransactions);
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
            _accountsController = null;
            _transactionsController = null;
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
            _dashboardController = null;
            _transactionsController = null;
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
            _dashboardController = null;
            _accountsController = null;
            _shell.SetActive(ShellScreen.Transactions);
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
