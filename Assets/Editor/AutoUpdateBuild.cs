using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class AutoUpdateBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string SettingsPath = "Assets/Resources/AutoUpdateSettings.json";
    public int callbackOrder => 1000;
    [Serializable] private sealed class Manifest
    {
        public int schema = 1;
        public string version, executable;
        public string[] files;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!IsWindows(report)) return;
        ReadVersion();
        string directory = Path.GetDirectoryName(Path.GetFullPath(report.summary.outputPath));
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (directory == project || directory == Application.dataPath || directory.StartsWith(Application.dataPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || Directory.Exists(Path.Combine(directory, "Assets")))
            throw new BuildFailedException("Use a dedicated empty build folder outside Assets.");
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (!IsWindows(report)) return;
        string directory = Path.GetDirectoryName(Path.GetFullPath(report.summary.outputPath));
        File.Copy("Assets/Editor/IdentityUpdater.ps1", Path.Combine(directory, "IdentityUpdater.ps1"), true);
        string manifestPath = Path.Combine(directory, GitHubAutoUpdater.ManifestName);
        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Select(path => path.Substring(directory.Length + 1).Replace('\\', '/'))
            .Where(path => path != GitHubAutoUpdater.ManifestName && path != ".identity-update.lock")
            .Concat(new[] { GitHubAutoUpdater.ManifestName }).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        File.WriteAllText(manifestPath, JsonUtility.ToJson(new Manifest
        {
            version = ReadVersion().ToString(System.Globalization.CultureInfo.InvariantCulture),
            executable = Path.GetFileName(report.summary.outputPath), files = files
        }, true));
        string output = Path.Combine(Path.GetDirectoryName(directory), GitHubAutoUpdater.AssetName);
        string temporary = output + ".tmp";
        try
        {
            using (var zip = ZipFile.Open(temporary, ZipArchiveMode.Create))
                foreach (string file in files)
                    zip.CreateEntryFromFile(Path.Combine(directory, file), file, System.IO.Compression.CompressionLevel.Optimal);
            if (File.Exists(output)) File.Delete(output);
            File.Move(temporary, output);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        Debug.Log("Automatic-update release ZIP: " + output);
    }

    private static bool IsWindows(BuildReport report) => report.summary.platform == BuildTarget.StandaloneWindows64 ||
        report.summary.platform == BuildTarget.StandaloneWindows;

    private static long ReadVersion()
    {
        var settings = JsonUtility.FromJson<GitHubAutoUpdater.Settings>(File.ReadAllText(SettingsPath));
        if (!ReleaseVersion.TryParse(settings.version, 0, out long version))
            throw new BuildFailedException("AutoUpdateSettings.json version must be yyyyMMdd or yyyyMMdd.revision.");
        return version;
    }

    [MenuItem("Tools/Identity Dispenser/Update Version Settings")]
    private static void SelectSettings()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(SettingsPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
