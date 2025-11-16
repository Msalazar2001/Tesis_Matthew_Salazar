using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class FadeOut : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image fadeImage;

    [Header("Tweens")]
    [SerializeField] private float fadeDuration = 1.2f;

    private void Awake()
    {
        // Empezar transparente y persistir entre escenas para evitar parpadeos
        if (fadeImage != null)
        {
            var c = fadeImage.color; c.a = 0f; fadeImage.color = c;
        }
        DontDestroyOnLoad(gameObject); // mantiene el panel durante la carga
    }

    /// <summary>
    /// Llama esto para hacer fade a negro y recién ahí cargar escena.
    /// </summary>
    public void FadeOutThenLoad(string sceneName)
    {
        if (fadeImage == null) return;

        // Limpia tweens previos por si acaso
        fadeImage.DOKill();

        // Fade out completo (independiente del timescale)
        fadeImage
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true) // corre incluso si Time.timeScale = 0
            .OnComplete(() =>
            {
                // Cuando termina el negro completo, recién carga asíncrono
                StartCoroutine(LoadAsync(sceneName));
            });
    }

    private System.Collections.IEnumerator LoadAsync(string sceneName)
    {
        // Carga asíncrona SIN activar hasta estar lista (opcional)
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        // si quisieras controlar el momento exacto, pon false y luego true.

        // Espera a que termine la carga
        while (!op.isDone)
            yield return null;

        // Si más adelante quieres hacer fade in, ya estás en negro.
        // Aquí podrías llamar a otro método para hacer DOFade(0f, t);
        // Por ahora solo pediste el fade out.
    }
}
