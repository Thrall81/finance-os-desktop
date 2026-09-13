using System;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    public enum ShellScreen
    {
        Dashboard,
        Accounts,
        Transactions,
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

        public ShellController(
            VisualElement root,
            Action onNavigateToDashboard,
            Action onNavigateToAccounts,
            Action onNavigateToTransactions)
        {
            _contentArea = root.Q<VisualElement>("content-area");
            _navDashboard = root.Q<Button>("nav-dashboard");
            _navAccounts = root.Q<Button>("nav-accounts");
            _navTransactions = root.Q<Button>("nav-transactions");

            _navDashboard.clicked += onNavigateToDashboard;
            _navAccounts.clicked += onNavigateToAccounts;
            _navTransactions.clicked += onNavigateToTransactions;
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
        }
    }
}
