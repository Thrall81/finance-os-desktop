using System;
using FinanceOS.App;
using FinanceOS.Domain;
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
        public VisualTreeAsset? CategoriesAsset;
        public VisualTreeAsset? SettingsAsset;
        public VisualTreeAsset? OnboardingAsset;

        /// <summary>ADR-145 (essai) : App UI.tss, référencé directement plutôt que via un
        /// &lt;Style src&gt; dans une UXML d'écran — un Popover App UI s'ajoute au panel root
        /// (voir Popup.GetRootPopupLayer), pas comme descendant de l'écran qui l'ouvre, donc une
        /// feuille de style scopée à l'UXML de cet écran ne l'atteint jamais. Appliqué directement
        /// sur l'élément du calendrier en C# (AppDatePickerField) à la place.</summary>
        public StyleSheet? AppUiThemeStyleSheet;

        public static AppContainer? Container { get; private set; }

        private static bool IsDarkTheme => Container?.Settings.Get().Theme == AppTheme.Dark;

        private ShellController? _shell;
        private DashboardController? _dashboardController;
        private AccountsController? _accountsController;
        private TransactionsController? _transactionsController;
        private RecurringOperationsController? _recurringOperationsController;
        private ForecastsController? _forecastsController;
        private BudgetsController? _budgetsController;
        private CategoriesController? _categoriesController;
        private SettingsController? _settingsController;
        private OnboardingController? _onboardingController;

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
                root, ShowDashboard, ShowAccounts, ShowTransactions, ShowRecurringOperations, ShowForecasts,
                ShowBudgets, ShowCategories, ShowSettings, QuitApplication);
            _shell.SetTheme(settings.Theme);

            // "Aucun compte en base" is the whole gating condition (docs/01-Perimetre.md §2.1) —
            // deliberately not a separate "onboarding completed" flag: the flow never reappears
            // once a first account exists, including if the user quit right after creating just
            // one (docs/07-Interface.md §4), which this check already satisfies for free.
            if (Container.Accounts.ListAll().Count == 0)
            {
                ShowOnboarding();
            }
            else
            {
                ShowDashboard();
            }

            // The one deliberate, narrowly-scoped exception to "jamais automatique, jamais
            // silencieux" (docs/08-Confidentialite_et_donnees.md §1) — see ADR-139. Started after
            // the first screen already renders, so a slow/offline check never delays startup;
            // every failure path inside UpdateChecker is silent by design, so this is safe to
            // fire-and-forget without any error handling here.
            StartCoroutine(new UpdateChecker().CheckAndDownload(OnUpdateReady));
        }

        private void OnUpdateReady(string version, string installerPath)
        {
            _shell?.ShowUpdateReady(version, () => InstallUpdateAndRestart(installerPath), () => _shell?.HideUpdateReady());
        }

        /// <summary>The explicit confirmation point ShowUpdateReady's doc comment refers to —
        /// nothing from UpdateChecker's silent background download ever reaches this method
        /// without the user clicking "Installer et redémarrer" first. Launches the already-
        /// downloaded installer /VERYSILENT (no further Inno Setup prompts — the user already
        /// confirmed via this app's own dialog) and quits; the installer's own CloseApplications/
        /// RestartApplications directives (packaging/FinanceOS.iss, ADR-139) handle waiting for
        /// this process to fully exit and relaunching it once installation finishes.</summary>
        private void InstallUpdateAndRestart(string installerPath)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo(installerPath) { UseShellExecute = true };
                startInfo.ArgumentList.Add("/VERYSILENT");
                startInfo.ArgumentList.Add("/SUPPRESSMSGBOXES");
                startInfo.ArgumentList.Add("/NORESTART");
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AppBootstrap] Failed to launch the downloaded update: {ex.Message}");
                _shell?.HideUpdateReady();
                return;
            }

            QuitApplication();
        }

        private void ShowOnboarding()
        {
            if (Container is null || _shell is null || OnboardingAsset is null)
            {
                return;
            }

            var content = OnboardingAsset.Instantiate();
            _shell.SetContent(content);
            _shell.SetSidebarVisible(false);
            _onboardingController = new OnboardingController(content, Container.Accounts, Container.RecurringOperations, OnOnboardingFinished);
            ClearOtherControllers();
        }

        private void OnOnboardingFinished()
        {
            if (Container is null)
            {
                return;
            }

            // Any recurring operation added during onboarding needs its occurrences generated
            // before the Dashboard renders — the same idempotent call Awake() already makes on
            // every launch, just re-run now that onboarding may have just created new operations.
            var settings = Container.Settings.Get();
            Container.RecurringOperations.GenerateUpcomingOccurrences(DateTime.Now, settings.ForecastHorizonDays);

            _onboardingController = null;
            _shell?.SetSidebarVisible(true);
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
            _dashboardController = new DashboardController(content, IsDarkTheme);
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
                Container.Transactions, Container.InternalTransfers, Container.TransferDetection,
                Container.Settings, IsDarkTheme);
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
                Container.RecurringOperations, Container.Settings, AppUiThemeStyleSheet);
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
            _budgetsController = new BudgetsController(content, Container.Budget, Container.Categories, isDarkTheme: IsDarkTheme);
            ClearOtherControllers(keepBudgets: true);
            _shell.SetActive(ShellScreen.Budgets);
        }

        private void ShowCategories()
        {
            if (Container is null || _shell is null || CategoriesAsset is null)
            {
                return;
            }

            var content = CategoriesAsset.Instantiate();
            _shell.SetContent(content);
            _categoriesController = new CategoriesController(content, Container.Categories, IsDarkTheme);
            ClearOtherControllers(keepCategories: true);
            _shell.SetActive(ShellScreen.Categories);
        }

        private void ShowSettings()
        {
            if (Container is null || _shell is null || SettingsAsset is null)
            {
                return;
            }

            var content = SettingsAsset.Instantiate();
            _shell.SetContent(content);
            _settingsController = new SettingsController(
                content, Container.Settings, Container.Accounts, Container.Backup, Container.DatabasePath,
                onThemeChanged: _shell.SetTheme);
            ClearOtherControllers(keepSettings: true);
            _shell.SetActive(ShellScreen.Settings);
        }

        private void ClearOtherControllers(
            bool keepDashboard = false,
            bool keepAccounts = false,
            bool keepTransactions = false,
            bool keepRecurringOperations = false,
            bool keepForecasts = false,
            bool keepBudgets = false,
            bool keepCategories = false,
            bool keepSettings = false)
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

            if (!keepCategories)
            {
                _categoriesController = null;
            }

            if (!keepSettings)
            {
                _settingsController = null;
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

        /// <summary>The sidebar's "Quitter" button (ADR-138) — added after the first installer
        /// build shipped in exclusive fullscreen with no titlebar/close button, leaving no
        /// reliable way to close the app at all. Application.Quit() is a documented no-op in the
        /// Editor, so Play Mode instead stops playing directly — either way, OnApplicationQuit()
        /// below still runs and disposes the database connection cleanly.</summary>
        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnApplicationQuit()
        {
            Container?.Dispose();
            Container = null;
        }
    }
}
