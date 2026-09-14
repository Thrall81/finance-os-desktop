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
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
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
