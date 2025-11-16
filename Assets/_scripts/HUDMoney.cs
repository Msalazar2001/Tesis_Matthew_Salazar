using UnityEngine;
using TMPro;

public class HUDMoney : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dineroText;
    [SerializeField] private bool mostrarDecimal = false;
    [SerializeField] private float updateRate = 0.5f; // segundos (tiempo real, no afectado por pausa)
    [SerializeField] private string prefijo = "Dinero: "; // deja vacío si no quieres texto

    private float proximoUpdateUnscaled;

    void Awake()
    {
        // Si no arrastraste nada, busca en este GO y en hijos (aunque estén inactivos)
        if (dineroText == null)
        {
            dineroText = GetComponent<TextMeshProUGUI>();
            if (dineroText == null)
            {
                dineroText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    void OnEnable()
    {
        // Refresco inmediato al activarse (aunque el juego esté en pausa)
        ActualizarDineroUI();
        proximoUpdateUnscaled = Time.unscaledTime + updateRate;
    }

    void Update()
    {
        // Usamos tiempo NO escalado para que funcione en pausa o paneles de fin de partida
        if (Time.unscaledTime >= proximoUpdateUnscaled)
        {
            ActualizarDineroUI();
            proximoUpdateUnscaled = Time.unscaledTime + updateRate;
        }
    }

    public void ForceRefresh() => ActualizarDineroUI();

    private void ActualizarDineroUI()
    {
        if (dineroText == null) return;

        // Si aún no existe el GameManager, muestra 0 y reintenta luego
       // float dineroTotal = (GameManager.Instance != null)
         //   ? GameManager.Instance.ObtenerDineroTotal()
          //  : 0f;

       // dineroText.text = mostrarDecimal
        //    ? $"{prefijo}${dineroTotal:0.00}"
         //   : $"{prefijo}${Mathf.RoundToInt(dineroTotal)}";
    }
}
