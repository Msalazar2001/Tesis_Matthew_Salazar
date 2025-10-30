using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonLoadScene : MonoBehaviour
{
    // Nombre de la escena que quieres cargar (debe estar en Build Settings)
    public string sceneName = "Oficina";

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
