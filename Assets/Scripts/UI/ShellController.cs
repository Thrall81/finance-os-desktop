using System;
using FinanceOS.Domain;
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
        Categories,
        Settings,
    }

    /// <summary>
    /// Binds Shell.uxml's sidebar navigation and content area. Owns no screen-specific state —
    /// AppBootstrap decides which controller to build and hands it the content area to populate.
    /// See docs/07-Interface.md §2.
    /// </summary>
    public sealed class ShellController
    {
        private readonly VisualElement _themeRoot;
        private readonly VisualElement _sidebar;
        private readonly VisualElement _contentArea;
        private readonly Button _navDashboard;
        private readonly Button _navAccounts;
        private readonly Button _navTransactions;
        private readonly Button _navRecurringOperations;
        private readonly Button _navForecasts;
        private readonly Button _navBudgets;
        private readonly Button _navCategories;
        private readonly Button _navSettings;

        public ShellController(
            VisualElement root,
            Action onNavigateToDashboard,
            Action onNavigateToAccounts,
            Action onNavigateToTransactions,
            Action onNavigateToRecurringOperations,
            Action onNavigateToForecasts,
            Action onNavigateToBudgets,
            Action onNavigateToCategories,
            Action onNavigateToSettings)
        {
            _themeRoot = root.Q<VisualElement>("shell-root");
            _sidebar = root.Q<VisualElement>("sidebar");
            _contentArea = root.Q<VisualElement>("content-area");
            _navDashboard = root.Q<Button>("nav-dashboard");
            _navAccounts = root.Q<Button>("nav-accounts");
            _navTransactions = root.Q<Button>("nav-transactions");
            _navRecurringOperations = root.Q<Button>("nav-recurring-operations");
            _navForecasts = root.Q<Button>("nav-forecasts");
            _navBudgets = root.Q<Button>("nav-budgets");
            _navCategories = root.Q<Button>("nav-categories");
            _navSettings = root.Q<Button>("nav-settings");

            _navDashboard.clicked += onNavigateToDashboard;
            _navAccounts.clicked += onNavigateToAccounts;
            _navTransactions.clicked += onNavigateToTransactions;
            _navRecurringOperations.clicked += onNavigateToRecurringOperations;
            _navForecasts.clicked += onNavigateToForecasts;
            _navBudgets.clicked += onNavigateToBudgets;
            _navCategories.clicked += onNavigateToCategories;
            _navSettings.clicked += onNavigateToSettings;
        }

        public void SetContent(VisualElement content)
        {
            _contentArea.Clear();
            _contentArea.Add(content);
        }

        /// <summary>Hidden during the first-launch onboarding flow (docs/07-Interface.md §4) —
        /// a guided flow shouldn't let the user navigate away mid-flow into an otherwise-empty
        /// screen. Shown again as soon as onboarding finishes.</summary>
        public void SetSidebarVisible(bool visible) =>
            _sidebar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>Toggles the class that redefines every --color-* custom property in
        /// theme.uss (ADR-135) — applied to the same element that first defines them, so USS
        /// cascading resolves the override for every descendant automatically. Does not touch
        /// Painter2D chart colors (LineChartElement and siblings), which read a separate,
        /// per-instance DarkTheme flag instead — a CSS custom property is not readable from C#.</summary>
        public void SetTheme(AppTheme theme) =>
            _themeRoot.EnableInClassList("theme-dark", theme == AppTheme.Dark);

        public void SetActive(ShellScreen screen)
        {
            _navDashboard.EnableInClassList("nav-item-active", screen == ShellScreen.Dashboard);
            _navAccounts.EnableInClassList("nav-item-active", screen == ShellScreen.Accounts);
            _navTransactions.EnableInClassList("nav-item-active", screen == ShellScreen.Transactions);
            _navRecurringOperations.EnableInClassList("nav-item-active", screen == ShellScreen.RecurringOperations);
            _navForecasts.EnableInClassList("nav-item-active", screen == ShellScreen.Forecasts);
            _navBudgets.EnableInClassList("nav-item-active", screen == ShellScreen.Budgets);
            _navCategories.EnableInClassList("nav-item-active", screen == ShellScreen.Categories);
            _navSettings.EnableInClassList("nav-item-active", screen == ShellScreen.Settings);
        }
    }
}
