using UnityEngine;
using TMPro;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI tiempoText;
    public TextMeshProUGUI dineroText;
    public TextMeshProUGUI pasajerosText;

    // NUEVO: texto para mostrar el porcentaje de daño
    public TextMeshProUGUI danoText;

    [Header("Refresco (segundos)")]
    public float updateInterval = 0.2f;

    void OnEnable()
    {
        StartCoroutine(UpdateLoop());
    }


    IEnumerator UpdateLoop()
    {
        var wait = new WaitForSeconds(updateInterval);

        while (true)
        {
            if (GameManager.Instance)
            {
                // Tiempo restante
                if (tiempoText)
                {
                    tiempoText.text = "Tiempo restante: " + GameManager.Instance.TiempoRestanteStr();

                    // Color según overtime
                    if (GameManager.Instance.EstaEnOvertime())
                        tiempoText.color = Color.red;
                    else
                        tiempoText.color = Color.white;
                }

                // Dinero
                if (dineroText)
                    dineroText.text = $"Dinero: ${GameManager.Instance.ObtenerDineroGanado():0.0}";

                // Pasajeros
                if (pasajerosText)
                    pasajerosText.text = $"Pasajeros recogidos: {GameManager.Instance.ObtenerTotalPasajeros()}";

                // --- NUEVO: Daño en porcentaje ---
                if (danoText)
                {
                    float pct = GameManager.Instance.ObtenerDanoPorcentaje100();
                    danoText.text = $"Dano: {pct:0}%";

                    // (Opcional) colorear si pasa de cierto umbral
                     danoText.color = pct >= 100f ? Color.red : Color.white;
                }
            }

            yield return wait;
        }
    }
    public void ResetHUD()
    {
        if (tiempoText) tiempoText.text = "Tiempo restante: 00:00";
        if (dineroText) dineroText.text = "Dinero: $0.0";
        if (pasajerosText) pasajerosText.text = "Pasajeros recogidos: 0";
        if (danoText) danoText.text = "Daño: 0%";
    }

}
