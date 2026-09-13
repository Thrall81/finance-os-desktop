using System.IO;
using FinanceOS.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FinanceOS.EditorTools
{
    /// <summary>
    /// Wires the shell UI into Main.unity — a GameObject with a UIDocument (PanelSettings +
    /// Shell.uxml) and the AppBootstrap component, which is handed the per-screen UXML assets it
    /// swaps into the shell's content area. Idempotent: re-running it updates the existing setup
    /// rather than duplicating it.
    /// </summary>
    internal static class SceneWiringTools
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string PanelSettingsPath = "Assets/UI/PanelSettings.asset";
        private const string ShellUxmlPath = "Assets/UI/UXML/Shell.uxml";
        private const string DashboardUxmlPath = "Assets/UI/UXML/Dashboard.uxml";
        private const string AccountsUxmlPath = "Assets/UI/UXML/Accounts.uxml";
        private const string TransactionsUxmlPath = "Assets/UI/UXML/Transactions.uxml";
        private const string RecurringOperationsUxmlPath = "Assets/UI/UXML/RecurringOperations.uxml";
        private const string ForecastsUxmlPath = "Assets/UI/UXML/Forecasts.uxml";
        private const string BudgetsUxmlPath = "Assets/UI/UXML/Budgets.uxml";
        private const string CategoriesUxmlPath = "Assets/UI/UXML/Categories.uxml";
        private const string SettingsUxmlPath = "Assets/UI/UXML/Settings.uxml";

        [MenuItem("Finance OS/Wire Shell Into Main Scene")]
        public static void WireShellIntoMainScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            var shellTree = LoadRequired<VisualTreeAsset>(ShellUxmlPath);
            var dashboardTree = LoadRequired<VisualTreeAsset>(DashboardUxmlPath);
            var accountsTree = LoadRequired<VisualTreeAsset>(AccountsUxmlPath);
            var transactionsTree = LoadRequired<VisualTreeAsset>(TransactionsUxmlPath);
            var recurringOperationsTree = LoadRequired<VisualTreeAsset>(RecurringOperationsUxmlPath);
            var forecastsTree = LoadRequired<VisualTreeAsset>(ForecastsUxmlPath);
            var budgetsTree = LoadRequired<VisualTreeAsset>(BudgetsUxmlPath);
            var categoriesTree = LoadRequired<VisualTreeAsset>(CategoriesUxmlPath);
            var settingsTree = LoadRequired<VisualTreeAsset>(SettingsUxmlPath);

            var bootstrap = Object.FindFirstObjectByType<AppBootstrap>(FindObjectsInactive.Include);
            var uiObject = bootstrap != null ? bootstrap.gameObject : new GameObject("UI");

            var uiDocument = uiObject.GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = uiObject.AddComponent<UIDocument>();
            }

            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = shellTree;

            var appBootstrap = uiObject.GetComponent<AppBootstrap>();
            if (appBootstrap == null)
            {
                appBootstrap = uiObject.AddComponent<AppBootstrap>();
            }

            appBootstrap.DashboardAsset = dashboardTree;
            appBootstrap.AccountsAsset = accountsTree;
            appBootstrap.TransactionsAsset = transactionsTree;
            appBootstrap.RecurringOperationsAsset = recurringOperationsTree;
            appBootstrap.ForecastsAsset = forecastsTree;
            appBootstrap.BudgetsAsset = budgetsTree;
            appBootstrap.CategoriesAsset = categoriesTree;
            appBootstrap.SettingsAsset = settingsTree;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[SceneWiringTools] Shell wired into Main.unity.");
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException($"{typeof(T).Name} not found at {path}");
            }

            return asset;
        }
    }
}
