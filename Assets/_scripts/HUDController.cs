using UnityEngine;
using TMPro;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI tiempoText;
    public TextMeshProUGUI dineroText;
    public TextMeshProUGUI pasajerosText;

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
                // Tiempo restante en formato mm:ss
                if (tiempoText)
                {
                    tiempoText.text = "Tiempo restante: " + GameManager.Instance.TiempoRestanteStr();

                    //  Cambiar color según overtime
                    if (GameManager.Instance.EstaEnOvertime())
                    {
                        tiempoText.color = Color.red;   // si se pasó del tiempo
                    }
                    else
                    {
                        tiempoText.color = Color.white; // tiempo normal
                    }
                }

                // Dinero
                if (dineroText) dineroText.text = $"Dinero: ${GameManager.Instance.ObtenerDineroGanado():0.0}";

                // Pasajeros
                if (pasajerosText) pasajerosText.text = $"Pasajeros recogidos: {GameManager.Instance.ObtenerTotalPasajeros()}";
            }

            yield return wait;
        }
    }
}
