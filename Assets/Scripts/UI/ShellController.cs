using System;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    public enum ShellScreen
    {
        Dashboard,
        Accounts,
        Transactions,
        RecurringOperations,
        Forecasts,
        Budgets,
    }

    /// <summary>
    /// Binds Shell.uxml's sidebar navigation and content area. Owns no screen-specific state —
    /// AppBootstrap decides which controller to build and hands it the content area to populate.
    /// See docs/07-Interface.md §2.
    /// </summary>
    public sealed class ShellController
    {
        private readonly VisualElement _contentArea;
        private readonly Button _navDashboard;
        private readonly Button _navAccounts;
        private readonly Button _navTransactions;
        private readonly Button _navRecurringOperations;
        private readonly Button _navForecasts;
        private readonly Button _navBudgets;

        public ShellController(
            VisualElement root,
            Action onNavigateToDashboard,
            Action onNavigateToAccounts,
            Action onNavigateToTransactions,
            Action onNavigateToRecurringOperations,
            Action onNavigateToForecasts,
            Action onNavigateToBudgets)
        {
            _contentArea = root.Q<VisualElement>("content-area");
            _navDashboard = root.Q<Button>("nav-dashboard");
            _navAccounts = root.Q<Button>("nav-accounts");
            _navTransactions = root.Q<Button>("nav-transactions");
            _navRecurringOperations = root.Q<Button>("nav-recurring-operations");
            _navForecasts = root.Q<Button>("nav-forecasts");
            _navBudgets = root.Q<Button>("nav-budgets");

            _navDashboard.clicked += onNavigateToDashboard;
            _navAccounts.clicked += onNavigateToAccounts;
            _navTransactions.clicked += onNavigateToTransactions;
            _navRecurringOperations.clicked += onNavigateToRecurringOperations;
            _navForecasts.clicked += onNavigateToForecasts;
            _navBudgets.clicked += onNavigateToBudgets;
        }

        public void SetContent(VisualElement content)
        {
            _contentArea.Clear();
            _contentArea.Add(content);
        }

        public void SetActive(ShellScreen screen)
        {
            _navDashboard.EnableInClassList("nav-item-active", screen == ShellScreen.Dashboard);
            _navAccounts.EnableInClassList("nav-item-active", screen == ShellScreen.Accounts);
            _navTransactions.EnableInClassList("nav-item-active", screen == ShellScreen.Transactions);
            _navRecurringOperations.EnableInClassList("nav-item-active", screen == ShellScreen.RecurringOperations);
            _navForecasts.EnableInClassList("nav-item-active", screen == ShellScreen.Forecasts);
            _navBudgets.EnableInClassList("nav-item-active", screen == ShellScreen.Budgets);
        }
    }
}
