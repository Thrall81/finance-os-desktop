using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FinanceOS.EditorTools
{
    /// <summary>
    /// Produces the Windows Standalone IL2CPP player that packaging/FinanceOS.iss wraps into an
    /// installer. See docs/04-Stack_technique.md §9 and docs/09-Decisions_techniques.md ADR-136.
    /// </summary>
    internal static class BuildScript
    {
        private const string BuildOutputDirectory = "Build/Windows";
        private const string ExecutableName = "FinanceOS.exe";

        [MenuItem("Finance OS/Build Windows Player (IL2CPP)")]
        public static void BuildWindowsPlayer()
        {
            PlayerSettings.companyName = "Florent Barbaouat";
            PlayerSettings.productName = "Finance OS";
            PlayerSettings.bundleVersion = "1.0.1";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);

            // A new Unity project defaults to exclusive/borderless fullscreen with no window
            // chrome — fine for a game, wrong for a desktop utility app: the very first installer
            // build shipped this way, and the user had no titlebar, no X button, and no reliable
            // way to close it. Windowed + resizable + a real titlebar matches what a desktop app
            // is expected to look like. See docs/09-Decisions_techniques.md ADR-138.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            AssetDatabase.SaveAssets();

            if (Directory.Exists(BuildOutputDirectory))
            {
                Directory.Delete(BuildOutputDirectory, recursive: true);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = Path.Combine(BuildOutputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log(
                $"[BuildScript] Result: {summary.result} — {summary.totalErrors} error(s), " +
                $"{summary.totalWarnings} warning(s), {summary.totalSize} bytes, output: {summary.outputPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"[BuildScript] Build did not succeed: {summary.result}.");
            }
        }
    }
}
