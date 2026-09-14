using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace FinanceOS.UI
{
    /// <summary>
    /// Background version check against this project's public GitHub Releases, and a silent
    /// download of the installer if a newer one exists — the one deliberate, narrowly-scoped
    /// exception to "jamais automatique, jamais silencieux" (docs/08-Confidentialite_et_donnees.md
    /// §1), confirmed explicitly with the user rather than assumed. Never sends anything besides
    /// an anonymous HTTP GET to a public, keyless API — no personal or financial data ever leaves
    /// the machine. Installing the downloaded file always stops short of automatic: that step
    /// still needs an explicit confirmation from the user (see AppBootstrap/ShellController).
    /// See docs/09-Decisions_techniques.md ADR-139. Public (rather than internal) purely so
    /// UISmokeTest.cs can exercise IsNewerVersion/FindInstallerDownloadUrl directly — the real
    /// network/coroutine flow can't run in batchmode at all, so this pure decision logic is the
    /// only part of this class that's actually testable here.
    /// </summary>
    public sealed class UpdateChecker
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/Thrall81/finance-os-desktop/releases/latest";

        [Serializable]
        private sealed class ReleaseAsset
        {
            public string name = string.Empty;
            public string browser_download_url = string.Empty;
        }

        [Serializable]
        private sealed class ReleaseResponse
        {
            public string tag_name = string.Empty;
            public ReleaseAsset[] assets = Array.Empty<ReleaseAsset>();
        }

        /// <summary>Whether <paramref name="latestTag"/> (a GitHub release tag, e.g. "v1.2.0") is
        /// a newer version than <paramref name="currentVersion"/> (e.g. Application.version,
        /// "1.0.1") — pure and separated from the network/coroutine machinery below specifically
        /// so it stays testable without a real HTTP call. Any unparseable input is treated as
        /// "not newer" rather than throwing — a malformed tag should never crash the check, just
        /// silently skip it, consistent with every other failure mode here being silent.</summary>
        public static bool IsNewerVersion(string latestTag, string currentVersion)
        {
            var normalizedLatest = (latestTag ?? string.Empty).TrimStart('v', 'V');
            return Version.TryParse(normalizedLatest, out var latest)
                && Version.TryParse(currentVersion, out var current)
                && latest > current;
        }

        /// <summary>Picks which release asset to download — the one Windows installer .exe. Null
        /// if the release has none (e.g. a release with only source archives).</summary>
        public static string? FindInstallerDownloadUrl(ReleaseAssetInfo[] assets) =>
            assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))?.Url;

        /// <summary>Runs the whole check-then-download flow as a Unity coroutine (started by
        /// AppBootstrap, the only MonoBehaviour in this project) and invokes
        /// <paramref name="onUpdateReady"/> only if a genuinely newer version was found and fully
        /// downloaded. Every failure path (offline, GitHub unreachable, malformed response, no
        /// .exe asset, download failure) exits silently — a version-check failing must never look
        /// like an app error to the user, and must never retry aggressively either; the next
        /// natural check is simply the next launch.</summary>
        public IEnumerator CheckAndDownload(Action<string, string> onUpdateReady)
        {
            using var request = UnityWebRequest.Get(LatestReleaseUrl);
            request.SetRequestHeader("User-Agent", "FinanceOS-Desktop-UpdateChecker");
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }

            ReleaseResponse? release;
            try
            {
                release = JsonUtility.FromJson<ReleaseResponse>(request.downloadHandler.text);
            }
            catch (Exception)
            {
                yield break;
            }

            if (release is null || !IsNewerVersion(release.tag_name, Application.version))
            {
                yield break;
            }

            var assetInfos = release.assets.Select(a => new ReleaseAssetInfo(a.name, a.browser_download_url)).ToArray();
            var downloadUrl = FindInstallerDownloadUrl(assetInfos);
            if (downloadUrl is null)
            {
                yield break;
            }

            var installerFileName = $"FinanceOS-Setup-{release.tag_name.TrimStart('v', 'V')}.exe";
            var downloadPath = Path.Combine(Application.temporaryCachePath, installerFileName);

            using var downloadRequest = new UnityWebRequest(downloadUrl, UnityWebRequest.kHttpVerbGET);
            downloadRequest.downloadHandler = new DownloadHandlerFile(downloadPath);
            downloadRequest.timeout = 120;
            yield return downloadRequest.SendWebRequest();

            if (downloadRequest.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }

            onUpdateReady(release.tag_name.TrimStart('v', 'V'), downloadPath);
        }
    }

    /// <summary>A release asset's name and download URL — just enough to pick the installer,
    /// deliberately not the raw GitHub API shape so the picking logic (FindInstallerDownloadUrl)
    /// can be unit-tested without depending on JSON at all. Public for the same testability
    /// reason as UpdateChecker itself.</summary>
    public sealed record ReleaseAssetInfo(string Name, string Url);
}
