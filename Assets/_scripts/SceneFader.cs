using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SceneFader : MonoBehaviour
{
    public CanvasGroup fadeGroup;
    public float fadeDuration = 0.8f;

    void Awake()
    {
        // Asegura estado inicial
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f;             // Arranca negro para hacer fade in
            fadeGroup.interactable = true;    // Solo al inicio para cubrir transición
            fadeGroup.blocksRaycasts = true;  // Bloquea clics durante el fade inicial
        }
        DontDestroyOnLoad(gameObject);
        // Escucha la carga para ejecutar el fade in al entrar a cada escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Fade in al iniciar la primera escena
        FadeIn();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Al cargar una escena nueva, empezamos desde negro y hacemos fade in
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f;
            fadeGroup.interactable = true;
            fadeGroup.blocksRaycasts = true;
            FadeIn();
        }
    }

    void FadeIn()
    {
        if (fadeGroup == null) return;

        fadeGroup.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            // Al terminar el fade in, el fader no debe bloquear la UI
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
        });
    }

    public void FadeToScene(string sceneName)
    {
        if (fadeGroup == null) return;

        // Antes del fade out, bloquea raycasts para evitar clics durante la transición
        fadeGroup.interactable = true;
        fadeGroup.blocksRaycasts = true;

        fadeGroup.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            SceneManager.LoadScene(sceneName);
            // No desactivamos aquí los raycasts: el OnSceneLoaded hará el FadeIn y los desactivará al terminar.
        });
    }
}
