using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;

    private const string KEY_DINERO_TOTAL = "DINERO_TOTAL";

    public float DineroTotal { get; private set; } = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cargar dinero guardado (si no hay, empieza en 0)
        DineroTotal = PlayerPrefs.GetFloat(KEY_DINERO_TOTAL, 0f);
    }

    public void AddDinero(float cantidad)
    {
        if (cantidad <= 0f) return;

        DineroTotal += cantidad;
        PlayerPrefs.SetFloat(KEY_DINERO_TOTAL, DineroTotal);
        PlayerPrefs.Save();
    }

    public void ResetDinero()
    {
        DineroTotal = 0f;
        PlayerPrefs.SetFloat(KEY_DINERO_TOTAL, DineroTotal);
        PlayerPrefs.Save();
    }
}
