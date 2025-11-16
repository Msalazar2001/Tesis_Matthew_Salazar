using UnityEngine;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;

        // Carga el prefab desde Resources
        var prefab = Resources.Load<GameManager>("Managers/GameManager");
        if (prefab != null)
        {
            var gm = Object.Instantiate(prefab);
            gm.name = "GameManager";
            // GameManager ya tiene DontDestroyOnLoad en Awake()
        }
        else
        {
            Debug.LogError("No se encontró Resources/Managers/GameManager.prefab");
        }
    }
}
