using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class CopyReadmeAfterBuild
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(
        BuildTarget target,
        string pathToBuiltProject)
    {
        string sourcePath =
            Path.Combine(Application.dataPath, "Editor", "README.md");

        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning(
                $"README.md를 찾을 수 없습니다.\n경로: {sourcePath}"
            );
            return;
        }

        string buildDirectory =
            Path.GetDirectoryName(pathToBuiltProject);

        if (string.IsNullOrEmpty(buildDirectory))
        {
            Debug.LogWarning("빌드 폴더 경로를 찾을 수 없습니다.");
            return;
        }

        string destinationPath =
            Path.Combine(buildDirectory, "README.md");

        try
        {
            File.Copy(sourcePath, destinationPath, true);

            Debug.Log(
                $"복사 완료\n{destinationPath}"
            );
        }
        catch (IOException e)
        {
            Debug.LogError(
                $"복사 실패: {e.Message}"
            );
        }
    }
}