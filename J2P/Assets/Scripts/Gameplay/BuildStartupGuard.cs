using UnityEngine;

namespace TankArena2D
{
    public static class BuildStartupGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureBuildWindow()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE_LINUX
            Resolution current = Screen.currentResolution;
            Screen.SetResolution(current.width, current.height, FullScreenMode.Windowed);
#endif
        }
    }
}
