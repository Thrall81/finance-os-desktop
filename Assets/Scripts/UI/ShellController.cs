using System;
using System.Collections.Generic;
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
        private readonly Button _quitButton;

        private readonly VisualElement _updateReadyOverlay;
        private readonly Label _updateReadyMessage;
        private readonly Button _updateLaterButton;
        private readonly Button _updateInstallButton;
        private Action? _onUpdateLater;
        private Action? _onUpdateInstallNow;

        private readonly VisualElement _changelogOverlay;
        private readonly ScrollView _changelogList;
        private readonly Button _changelogCloseButton;
        private Action? _onChangelogClosed;

        public ShellController(
            VisualElement root,
            Action onNavigateToDashboard,
            Action onNavigateToAccounts,
            Action onNavigateToTransactions,
            Action onNavigateToRecurringOperations,
            Action onNavigateToForecasts,
            Action onNavigateToBudgets,
            Action onNavigateToCategories,
            Action onNavigateToSettings,
            Action onQuit)
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
            _quitButton = root.Q<Button>("quit-button");

            _updateReadyOverlay = root.Q<VisualElement>("update-ready-overlay");
            // Same reasoning as every other "hidden by default" element in this app (ADR-117): a
            // bare UXML "display: none" doesn't reliably populate .style.display outside a live
            // panel, so this is set explicitly rather than trusted from the markup alone.
            _updateReadyOverlay.style.display = DisplayStyle.None;
            _updateReadyMessage = root.Q<Label>("update-ready-message");
            _updateLaterButton = root.Q<Button>("update-later-button");
            _updateInstallButton = root.Q<Button>("update-install-button");
            _updateLaterButton.clicked += () => _onUpdateLater?.Invoke();
            _updateInstallButton.clicked += () => _onUpdateInstallNow?.Invoke();

            _changelogOverlay = root.Q<VisualElement>("changelog-overlay");
            _changelogOverlay.style.display = DisplayStyle.None;
            _changelogList = root.Q<ScrollView>("changelog-list");
            _changelogCloseButton = root.Q<Button>("changelog-close-button");
            _changelogCloseButton.clicked += HideChangelog;

            _navDashboard.clicked += onNavigateToDashboard;
            _navAccounts.clicked += onNavigateToAccounts;
            _navTransactions.clicked += onNavigateToTransactions;
            _navRecurringOperations.clicked += onNavigateToRecurringOperations;
            _navForecasts.clicked += onNavigateToForecasts;
            _navBudgets.clicked += onNavigateToBudgets;
            _navCategories.clicked += onNavigateToCategories;
            _navSettings.clicked += onNavigateToSettings;
            _quitButton.clicked += onQuit;
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

        /// <summary>The one deliberate exception to "never automatic" (ADR-139) surfaces here:
        /// a silently-downloaded installer still always stops at this explicit confirmation
        /// before anything gets installed. Shown from any screen — this overlay is a direct
        /// child of .shell-root, not .content-area, so it isn't torn down by screen navigation.</summary>
        public void ShowUpdateReady(string version, Action onInstallNow, Action onLater)
        {
            _updateReadyMessage.text = $"La version {version} a été téléchargée. Voulez-vous l'installer maintenant ? L'application redémarrera.";
            _onUpdateInstallNow = onInstallNow;
            _onUpdateLater = onLater;
            _updateReadyOverlay.style.display = DisplayStyle.Flex;
        }

        public void HideUpdateReady()
        {
            _updateReadyOverlay.style.display = DisplayStyle.None;
            _onUpdateInstallNow = null;
            _onUpdateLater = null;
        }

        /// <summary>Shown either automatically once per version after an update, or on demand from
        /// Paramètres (ADR-150) — <paramref name="onClosed"/> is only meaningfully used for the
        /// former, to mark the version as seen once the user actually looks at it; the on-demand
        /// path passes a no-op. Same "direct child of .shell-root, survives screen navigation"
        /// overlay pattern as <see cref="ShowUpdateReady"/>.</summary>
        public void ShowChangelog(IReadOnlyList<ChangelogEntry> entries, Action onClosed)
        {
            _onChangelogClosed = onClosed;

            _changelogList.Clear();
            foreach (var entry in entries)
            {
                var versionTitle = new Label($"Version {entry.Version}");
                versionTitle.AddToClassList("changelog-version-title");
                _changelogList.Add(versionTitle);

                foreach (var highlight in entry.Highlights)
                {
                    var highlightLabel = new Label($"• {highlight}");
                    highlightLabel.AddToClassList("changelog-highlight");
                    _changelogList.Add(highlightLabel);
                }
            }

            _changelogOverlay.style.display = DisplayStyle.Flex;
        }

        public void HideChangelog()
        {
            _changelogOverlay.style.display = DisplayStyle.None;
            _onChangelogClosed?.Invoke();
            _onChangelogClosed = null;
        }

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
