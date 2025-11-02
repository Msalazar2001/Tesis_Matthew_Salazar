using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sonido que se reproducirá al apuntar")]
    [SerializeField] private AudioClip hoverClip;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // sonido 2D
    }

    // Este evento se llama cuando el puntero pasa por encima del botón
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverClip != null && audioSource != null)
            audioSource.PlayOneShot(hoverClip);
    }
}
