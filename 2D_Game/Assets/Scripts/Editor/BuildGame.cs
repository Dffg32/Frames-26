using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the Windows game into Build/Juegardo/Juegardo.exe.
// From the editor: menu Juegardo > Crear .exe para Windows.
// From the command line (with the project closed in the editor):
//   Unity.exe -batchmode -quit -projectPath <project> -executeMethod BuildGame.Windows -logFile build.log
//
// It's a development build on purpose: the game talks to the local server over plain HTTP
// (http://localhost:5080), and the project only allows HTTP in development builds (see NetworkSettings.cs).
public static class BuildGame
{
    const string OutputFolder = "Build/Juegardo";

    [MenuItem("Juegardo/Crear .exe para Windows")]
    public static void Windows()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(OutputFolder, "Juegardo.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Juego compilado en {Path.GetFullPath(OutputFolder)}");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(options.locationPathName);
        }
        else
        {
            Debug.LogError($"La compilación ha fallado: {report.summary.result} ({report.summary.totalErrors} errores)");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
