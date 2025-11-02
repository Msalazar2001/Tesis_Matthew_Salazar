using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Puedes dejar este campo opcional si quieres tener un valor por defecto
    [SerializeField] private string defaultSceneName;

    // Opción 1: usar el valor por defecto del Inspector
    public void LoadDefaultScene()
    {
        if (string.IsNullOrEmpty(defaultSceneName))
        {
            Debug.LogWarning("[SceneLoader] No se ha asignado una escena por defecto.");
            return;
        }

        LoadScene(defaultSceneName);
    }

    // Opción 2: pasar el nombre directamente desde el OnClick()
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoader] El nombre de la escena está vacío.");
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        Debug.Log($"[SceneLoader] Cargando escena: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
