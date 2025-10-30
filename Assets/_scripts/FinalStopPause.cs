// FinalStopPause.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinalStopPause : MonoBehaviour
{
    [Header("Detección")]
    public string busTag = "Bus";   // Tag del GameObject raíz del bus
    public bool soloUnaVez = true;

    bool _hecho;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true; // Asegura que sea Trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (soloUnaVez && _hecho) return;
        if (!other.CompareTag(busTag)) return;

        Time.timeScale = 0f;           // Pausa total
        //   // (opcional) pausar audio global

        _hecho = true;
    }

    // (opcional) para reanudar desde un botón u otro script
    public void Reanudar()
    {
        Time.timeScale = 1f;
        // AudioListener.pause = false;
        _hecho = false;
    }
}
