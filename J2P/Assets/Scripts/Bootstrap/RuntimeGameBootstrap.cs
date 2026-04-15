using UnityEngine;

namespace TankArena2D
{
    // The playable arena is now authored directly in the scene.
    // This component is kept as a harmless no-op to avoid stale references.
    public sealed class RuntimeGameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void DisableRuntimeBootstrap()
        {
        }

        private void Awake()
        {
            Destroy(gameObject);
        }
    }
}
