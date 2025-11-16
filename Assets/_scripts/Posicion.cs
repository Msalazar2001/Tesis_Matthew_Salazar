using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using BNG;

public class Posicion : MonoBehaviour
{
    [Header("Solo anclar en esta escena")]
    [SerializeField] string gameplaySceneName = "Juego";

    [Header("Cómo encontrar el asiento")]
    [SerializeField] string seatObjectName = "SeatAnchor";
    [SerializeField] string seatTag = "SeatAnchor";

    [Header("Opcional")]
    [SerializeField] BNGPlayerController playerController;

    const float TIMEOUT_SECONDS = 5f;
    Coroutine attachRoutine;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Solo si ya estás dentro de "Juego" al darle Play en el editor
        if (SceneManager.GetActiveScene().name == gameplaySceneName)
            RestartAttachRoutine(SceneManager.GetActiveScene().name);
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Solo anclar en "Juego"
        if (s.name != gameplaySceneName)
        {
            // Si venías adjuntando y saliste de Juego, cancela sin ruido
            if (attachRoutine != null) { StopCoroutine(attachRoutine); attachRoutine = null; }
            return;
        }
        RestartAttachRoutine(s.name);
    }

    void RestartAttachRoutine(string sceneName)
    {
        if (attachRoutine != null) StopCoroutine(attachRoutine);
        attachRoutine = StartCoroutine(AttachWhenReady(sceneName));
    }

    IEnumerator AttachWhenReady(string sceneNameAtStart)
    {
        // Seguridad: si no estamos en "Juego", salir sin warnings.
        if (SceneManager.GetActiveScene().name != gameplaySceneName) yield break;

        // Esperar Player (BNG) con cancelación si cambia la escena
        float end = Time.unscaledTime + TIMEOUT_SECONDS;
        while (playerController == null && Time.unscaledTime < end)
        {
            if (SceneManager.GetActiveScene().name != sceneNameAtStart) yield break;
            playerController = FindObjectOfType<BNGPlayerController>(true);
            yield return null;
        }
        if (playerController == null)
        {
            // Solo warn si SIGUE siendo Juego (evita ruido en Oficina)
            if (SceneManager.GetActiveScene().name == gameplaySceneName)
                Debug.LogWarning("[Posicion] No se encontró BNGPlayerController a tiempo en 'Juego'.");
            yield break;
        }

        // Buscar SeatAnchor en "Juego"
        Transform seat = null;
        end = Time.unscaledTime + TIMEOUT_SECONDS;
        while (seat == null && Time.unscaledTime < end)
        {
            if (SceneManager.GetActiveScene().name != sceneNameAtStart) yield break;

            GameObject seatGO = null;
            if (!string.IsNullOrEmpty(seatTag))
                seatGO = GameObject.FindGameObjectWithTag(seatTag);
            if (seatGO == null && !string.IsNullOrEmpty(seatObjectName))
                seatGO = GameObject.Find(seatObjectName);

            if (seatGO) seat = seatGO.transform;
            yield return null;
        }
        if (seat == null)
        {
            if (SceneManager.GetActiveScene().name == gameplaySceneName)
                Debug.LogWarning("[Posicion] No se encontró SeatAnchor en la escena 'Juego'.");
            yield break;
        }

        // Un frame extra para rigs/cámaras
        yield return null;

        // Anclaje BNG + fallback
        try
        {
            playerController.AttachPlayerToSeat(seat);
            Debug.Log("[Posicion] Player anclado al asiento en 'Juego'.");
        }
        catch (System.Exception e)
        {
            var root = playerController.transform;
            var cc = root.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            root.SetPositionAndRotation(seat.position, seat.rotation);
            if (cc) cc.enabled = true;
            Debug.LogWarning("[Posicion] Fallback de teletransporte. Detalle: " + e.Message);
        }
    }
}
