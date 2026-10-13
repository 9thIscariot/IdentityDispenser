using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class GitHubAutoUpdater : MonoBehaviour
{
    public const string AssetName = "IdentityDispenser.zip";
    public const string ManifestName = "identity-update.json";
    private const string Repository = "9thIscariot/IdentityDispenser";
    [Serializable] public sealed class Settings { public string version; }
#pragma warning disable CS0649 // GitHub response fields are populated by JsonUtility.
    [Serializable] private sealed class Release
    {
        public string tag_name, published_at;
        public bool draft, prerelease;
        public Asset[] assets;
    }
    [Serializable] private sealed class Asset { public string name, browser_download_url, digest; public long size; }
#pragma warning restore CS0649
    [Serializable] private sealed class Job
    {
        public string installDirectory, executable, archive, digest, expectedVersion;
        public int parentId;
    }

    private Text status;
    private GameObject overlay;
    private Font font;
    private FileStream updateLock;
    private string work;
    private bool handedOff;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        var go = new GameObject("GitHubAutoUpdater");
        DontDestroyOnLoad(go);
        go.AddComponent<GitHubAutoUpdater>();
#endif
    }

    private IEnumerator Start()
    {
        // Drive the iterator here so malformed responses / filesystem failures never prevent startup.
        var routine = Run();
        while (true)
        {
            object next = null;
            bool more = false;
            Exception failure = null;
            try { more = routine.MoveNext(); if (more) next = routine.Current; }
            catch (Exception ex) { failure = ex; }
            if (failure != null)
            {
                Debug.LogWarning("Automatic update skipped: " + failure.Message);
                if (status != null) status.text = "업데이트하지 못했습니다. 기존 버전으로 실행합니다.";
                yield return new WaitForSecondsRealtime(2);
                break;
            }
            if (!more) break;
            yield return next;
        }
        (routine as IDisposable)?.Dispose();
        if (!handedOff) Cleanup();
    }

    private IEnumerator Run()
    {
        var settings = JsonUtility.FromJson<Settings>(Resources.Load<TextAsset>("AutoUpdateSettings").text);
        if (!ReleaseVersion.TryParse(settings.version, 0, out long installed)) yield break;
        string executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        string directory = Path.GetDirectoryName(executable);
        // A second updater cannot download/apply concurrently in the same installation.
        updateLock = new FileStream(Path.Combine(directory, ".identity-update.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        ShowStatus("업데이트 확인 중…");
        Release release;
        using (var request = UnityWebRequest.Get("https://api.github.com/repos/" + Repository + "/releases/latest"))
        {
            request.timeout = 10;
            request.SetRequestHeader("Accept", "application/vnd.github+json");
            request.SetRequestHeader("User-Agent", "IdentityDispenser-Updater");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success) throw new IOException(request.error);
            release = JsonUtility.FromJson<Release>(request.downloadHandler.text);
        }
        if (release == null || release.draft || release.prerelease || release.assets == null) yield break;
        if (!DateTimeOffset.TryParse(release.published_at, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var published)) yield break;
        if (!ReleaseVersion.TryParse(release.tag_name, published.Year, out long available) || available <= installed)
            yield break;
        Asset asset = Array.Find(release.assets, item => item.name == AssetName);
        if (asset == null || asset.size <= 0 || !System.Text.RegularExpressions.Regex.IsMatch(asset.digest ?? "", @"^sha256:[a-fA-F0-9]{64}$"))
            throw new IOException("Release ZIP or SHA-256 digest missing.");
        if (!Uri.TryCreate(asset.browser_download_url, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || uri.Host != "github.com" ||
            !uri.AbsolutePath.StartsWith("/" + Repository + "/releases/download/", StringComparison.Ordinal))
            throw new IOException("Unexpected update URL.");

        work = Path.Combine(Path.GetTempPath(), "IdentityDispenserUpdate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        string archive = Path.Combine(work, "update.zip");
        using (var request = UnityWebRequest.Get(uri.AbsoluteUri))
        {
            request.downloadHandler = new DownloadHandlerFile(archive) { removeFileOnAbort = true };
            request.timeout = 600;
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                status.text = $"{release.tag_name} 업데이트 다운로드 중… {request.downloadedBytes / (double)asset.size:P0}\n완료 후 자동으로 다시 실행됩니다.";
                yield return null;
            }
            if (request.result != UnityWebRequest.Result.Success) throw new IOException(request.error);
        }
        if (new FileInfo(archive).Length != asset.size) throw new IOException("Incomplete update download.");
        status.text = "업데이트 파일 확인 중…";
        var hashTask = Task.Run(() =>
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var input = File.OpenRead(archive))
                return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        });
        while (!hashTask.IsCompleted) yield return null;
        if (!string.Equals("sha256:" + hashTask.GetAwaiter().GetResult(), asset.digest, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Update checksum mismatch.");

        File.Copy(Path.Combine(directory, "IdentityUpdater.ps1"), Path.Combine(work, "IdentityUpdater.ps1"));
        File.WriteAllText(Path.Combine(work, "job.json"), JsonUtility.ToJson(new Job
        {
            installDirectory = directory, executable = Path.GetFileName(executable), archive = archive,
            digest = asset.digest.Substring(7), expectedVersion = available.ToString(CultureInfo.InvariantCulture),
            parentId = System.Diagnostics.Process.GetCurrentProcess().Id
        }));
        string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            @"WindowsPowerShell\v1.0\powershell.exe");
        using (var helper = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = powershell,
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + Path.Combine(work, "IdentityUpdater.ps1") + "\"",
            WorkingDirectory = work, UseShellExecute = false, CreateNoWindow = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        }))
        {
            float deadline = Time.realtimeSinceStartup + 180;
            while (!File.Exists(Path.Combine(work, "ready")))
            {
                if (File.Exists(Path.Combine(work, "error.txt")))
                    throw new IOException(File.ReadAllText(Path.Combine(work, "error.txt")));
                if (helper == null || helper.HasExited) throw new IOException("Updater stopped before preparation completed.");
                if (Time.realtimeSinceStartup > deadline)
                {
                    helper.Kill(); helper.WaitForExit();
                    throw new IOException("Updater preparation timed out.");
                }
                yield return null;
            }
            handedOff = true;
            status.text = "업데이트 적용을 위해 다시 실행합니다…";
            Application.Quit();
        }
    }

    private void ShowStatus(string message)
    {
        overlay = new GameObject("UpdateOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var panel = new GameObject("Background", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.04f, 0.97f);
        var textObject = new GameObject("Status", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(80, 80); textRect.offsetMax = new Vector2(-80, -80);
        status = textObject.GetComponent<Text>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 32);
        status.font = font; status.fontSize = 32; status.alignment = TextAnchor.MiddleCenter;
        status.text = message;
    }

    private void Cleanup()
    {
        updateLock?.Dispose(); updateLock = null;
        if (overlay != null) Destroy(overlay);
        if (font != null) Destroy(font);
        if (work != null && Directory.Exists(work))
        {
            // Work is always a freshly generated child of the OS temporary directory.
            try { Directory.Delete(work, true); } catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
    private void OnDestroy() { if (!handedOff) Cleanup(); }
}
