using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FinanceOS.EditorTools
{
    internal static class ProjectBootstrapTools
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Finance OS/Create Main Scene")]
        public static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, MainScenePath);
            RegisterInBuildSettings();
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            var alreadyRegistered = scenes.Exists(entry => entry.path == MainScenePath);
            if (!alreadyRegistered)
            {
                scenes.Add(new EditorBuildSettingsScene(MainScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
