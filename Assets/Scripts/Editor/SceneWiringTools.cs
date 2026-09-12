using System.IO;
using FinanceOS.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.EditorTools
{
    /// <summary>
    /// Wires the dashboard UI into Main.unity — a GameObject with a UIDocument (PanelSettings +
    /// Dashboard.uxml) and the AppBootstrap component. Idempotent: re-running it updates the
    /// existing setup rather than duplicating it.
    /// </summary>
    internal static class SceneWiringTools
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string PanelSettingsPath = "Assets/UI/PanelSettings.asset";
        private const string DashboardUxmlPath = "Assets/UI/UXML/Dashboard.uxml";

        [MenuItem("Finance OS/Wire Dashboard Into Main Scene")]
        public static void WireDashboardIntoMainScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DashboardUxmlPath);
            if (visualTree == null)
            {
                throw new FileNotFoundException($"Dashboard UXML not found at {DashboardUxmlPath}");
            }

            var bootstrap = Object.FindFirstObjectByType<AppBootstrap>(FindObjectsInactive.Include);
            var uiObject = bootstrap != null ? bootstrap.gameObject : new GameObject("UI");

            var uiDocument = uiObject.GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = uiObject.AddComponent<UIDocument>();
            }

            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = visualTree;

            if (uiObject.GetComponent<AppBootstrap>() == null)
            {
                uiObject.AddComponent<AppBootstrap>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[SceneWiringTools] Dashboard wired into Main.unity.");
        }
    }
}
