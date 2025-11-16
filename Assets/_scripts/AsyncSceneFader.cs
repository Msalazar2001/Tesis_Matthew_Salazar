using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class AsyncSceneFader : MonoBehaviour
{
    public static AsyncSceneFader Instance;

    [Header("Refs")]
    [SerializeField] private Image fadeImage;   // Image negra a pantalla completa
    [SerializeField] private Canvas canvas;     // Canvas del fade (opcional, se auto-busca)

    [Header("Times (s)")]
    [SerializeField] private float fadeOutTime = 1.2f;
    [SerializeField] private float fadeInTime = 1.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        { Destroy(gameObject); return; }
        Instance = this;

        //  AHORA: solo este objeto se mantiene, no todo el root.
        DontDestroyOnLoad(gameObject);

        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            canvas.sortingOrder = 32767;

        if (fadeImage)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = true;
        }
    }



    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // se llama tras activar escena nueva
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// Llama esto para hacer fade out, cargar y luego fade in.
    public void FadeAndLoadScene(string sceneName)
    {
        if (!fadeImage) return;

        fadeImage.DOKill(true);

        // Empieza desde transparente y sube a negro
        EnsureAlpha(0f);
        fadeImage
            .DOFade(1f, fadeOutTime)
            .SetEase(Ease.Linear)
            .SetUpdate(true) // corre con timeScale=0
            .OnComplete(() => StartCoroutine(LoadSceneAsync(sceneName)));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

       
        while (op.progress < 0.9f)
            yield return null;

        // Activa la escena. El fade-in NO se hace aquí,
        // se hace en OnSceneLoaded (cuando la nueva escena ya está viva).
        op.allowSceneActivation = true;
    }

    // Se dispara cuando la NUEVA escena ya está activada
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si el Canvas es World Space, re-asigna la cámara
        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;

        // Asegura que seguimos en negro (por si algo tocó el alpha)
        EnsureAlpha(1f);
        StartCoroutine(FadeInNextFrame());
    }

    private IEnumerator FadeInNextFrame()
    {
        // Espera un frame + fin de frame para que la nueva escena
        // haya dibujado ya su primer frame antes del fade-in
        yield return null;
        yield return new WaitForEndOfFrame();

        if (!fadeImage) yield break;

        fadeImage.DOKill(true);
        fadeImage
            .DOFade(0f, fadeInTime)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    private void EnsureAlpha(float a)
    {
        if (!fadeImage) return;
        var c = fadeImage.color; c.a = a; fadeImage.color = c;
    }
}
