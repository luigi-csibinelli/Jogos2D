using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sanatorio.EditorTools
{
    /// <summary>
    /// Build WebGL para o itch.io (sem compressão: o itch serve direto, sem
    /// cabeçalhos especiais, e evita a tela preta clássica do Brotli).
    ///
    /// Menu: Sanatório > Gerar build WebGL
    /// Linha de comando: -executeMethod Sanatorio.EditorTools.BuildWebGL.BuildFromCommandLine
    /// </summary>
    public static class BuildWebGL
    {
        private const string OutputDir = "Build/WebGL";

        [MenuItem("Sanatório/Gerar build WebGL", false, 20)]
        public static void Menu() => Run();

        public static void BuildFromCommandLine()
        {
            if (!Run()) EditorApplication.Exit(1);
        }

        public static bool Run()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildWebGL] nenhuma cena no Build Settings. Rode Sanatório > Montar projeto antes.");
                return false;
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            if (s.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildWebGL] ok: {OutputDir} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0}s)");
                return true;
            }

            Debug.LogError($"[BuildWebGL] falhou: {s.result}, {s.totalErrors} erro(s).");
            return false;
        }
    }
}
