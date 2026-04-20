using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace TankArena2D.Editor
{
    public static class TankArenaBuild
    {
        private const string LinuxBuildPath = "Builds/Linux/TankArena.x86_64";

        public static void BuildLinuxPlayer()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No hay escenas activas en EditorBuildSettings.");
            }

            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), LinuxBuildPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory());

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Build fallido: {report.summary.result}");
            }
        }
    }
}
