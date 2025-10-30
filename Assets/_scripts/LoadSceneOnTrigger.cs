using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnTrigger : MonoBehaviour
{
    [Tooltip("Nombre de la escena a cargar")]
    public string sceneName = "Juego"; // cambia por el nombre exacto de tu escena

    private void OnTriggerEnter(Collider other)
    {
        // Si el que entra es el Player de VRIF
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
