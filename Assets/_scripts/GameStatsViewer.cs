using UnityEngine;
using TMPro;

public class GameStatsViewer : MonoBehaviour
{
    [Header("Textos del HUD o menú")]
    [SerializeField] private TextMeshProUGUI dineroTotalText;

    private const string KEY_DINERO_TOTAL = "DINERO_TOTAL";

    void Start()
    {
        ActualizarUI();
    }

    public void ActualizarUI()
    {
        if (dineroTotalText == null) return;

        float dineroTotal = PlayerPrefs.GetFloat(KEY_DINERO_TOTAL, 0f);
        dineroTotalText.text = $" ${dineroTotal:0.00}";
    }

    // opcional: si agregas más variables globales en el futuro
    public float ObtenerDineroTotal() => PlayerPrefs.GetFloat(KEY_DINERO_TOTAL, 0f);
}
