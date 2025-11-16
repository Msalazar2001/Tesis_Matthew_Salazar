using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuMain : MonoBehaviour
{
    [Header("Escena de juego")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Botones (opcional)")]
    [SerializeField] private Button btnCargar; // para desactivar si no hay guardado

    // Claves usadas por GameManager
    private const string KEY_DINERO_TOTAL = "DINERO_TOTAL";
    // Si luego guardas más progreso, agrégalo aquí para borrarlo en "Nuevo Juego"
    private static readonly string[] PROGRESS_KEYS = { KEY_DINERO_TOTAL };

    void Start()
    {
        // Si asignas el botón Cargar, lo desactiva si no hay guardado
        if (btnCargar != null)
        {
            bool hayGuardado = PlayerPrefs.HasKey(KEY_DINERO_TOTAL);
            btnCargar.interactable = hayGuardado;
        }
    }

    // === BOTONES ===
    public void OnEmpezar() // Nuevo juego
    {
        // Borra solo progreso (no DeleteAll, para no perder configuraciones)
        foreach (var key in PROGRESS_KEYS)
            PlayerPrefs.DeleteKey(key);

        PlayerPrefs.Save();
        // Carga la escena limpia (GameManager al iniciar leerá 0)
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("Juego");
    }

    public void OnCargar()
    {
        // Si no hay guardado, actúa como nuevo juego
        if (!PlayerPrefs.HasKey(KEY_DINERO_TOTAL))
        {
            OnEmpezar();
            return;
        }

        // Hay dinero acumulado; GameManager lo cargará en Awake()
        SceneManager.LoadScene(gameplaySceneName);
    }

    // (Opcional) por si tienes un botón "Salir"
    public void OnSalir()
    {
        Application.Quit();
    }
}
