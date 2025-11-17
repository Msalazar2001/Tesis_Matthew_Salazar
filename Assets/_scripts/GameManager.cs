using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Economía y reglas")]
    [SerializeField] private float tarifaBase = 2.5f;
    [SerializeField] private float tiempoPropuesto = 180f;
    [SerializeField] private float penalizacionPorSegundo = 1.5f;
    [SerializeField] private float danoMaximo = 200f;

    [Header("UI / Flow")]
    [SerializeField] private GameObject gameOverPanel;

    // ======== ESTADO DE PARTIDA ========
    int totalPasajerosRecogidos = 0;
    float dineroGanado = 0f;
    float dineroFinal = 0f;
    float tiempoTotal = 0f;
    bool contarTiempo = false;
    float danoTotal = 0f;
    float descuentoTiempo = 0f;
    float descuentoDano = 0f;
    bool gameOverActivado = false;

    // ======== SINGLETON (SOLO EN ESTA ESCENA) ========
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ⚠️ IMPORTANTE:
        // YA NO usamos DontDestroyOnLoad.
        // Este GameManager vive solo mientras la escena "Juego" está cargada.
    }

    void Start()
    {
        // Cuando entras a la escena Juego, siempre empezamos desde cero
        ResetRunState();
        BeginRun();
    }

    void Update()
    {
        if (contarTiempo)
            tiempoTotal += Time.deltaTime;
    }

    // ======== CONTROL DE PARTIDA ========
    public void ResetRunState()
    {
        totalPasajerosRecogidos = 0;
        dineroGanado = 0f;
        dineroFinal = 0f;
        tiempoTotal = 0f;
        contarTiempo = false;
        danoTotal = 0f;
        descuentoTiempo = 0f;
        descuentoDano = 0f;
        gameOverActivado = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        //if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void BeginRun()
    {
        contarTiempo = true;
        Debug.Log("[GameManager] Nueva partida iniciada.");
    }

    public void EndRun()
    {
        DetenerCronometro();

        // Descuento por tiempo
        if (tiempoTotal > tiempoPropuesto)
        {
            float diferencia = tiempoTotal - tiempoPropuesto;
            descuentoTiempo = Mathf.Max(0f, diferencia * penalizacionPorSegundo);
        }
        else
        {
            descuentoTiempo = 0f;
        }

        // Descuento por daño
        descuentoDano = danoTotal / 100f;

        // Resultado final de la carrera (nunca negativo)
        dineroFinal = Mathf.Max(0f, dineroGanado - descuentoTiempo - descuentoDano);

        Debug.Log($"[EndRun] Base:{dineroGanado} Tiempo:-{descuentoTiempo} Daño:-{descuentoDano} => Final:{dineroFinal}");
    }

    // ======== CRONÓMETRO ========
    public void IniciarCronometro() => contarTiempo = true;
    public void DetenerCronometro() => contarTiempo = false;

    // ======== PASAJEROS / DINERO ========
    public void PasajeroSubio(int cantidad = 1)
    {
        if (cantidad <= 0) return;
        totalPasajerosRecogidos += cantidad;
        dineroGanado += cantidad * tarifaBase;
    }

    public void RecibirPasajerosRecogidos(int cantidad)
    {
        totalPasajerosRecogidos = Mathf.Max(0, cantidad);
        dineroGanado = totalPasajerosRecogidos * tarifaBase;
    }

    // ======== DAÑO ========
    public void HacerDano(float cantidad)
    {
        if (cantidad <= 0f) return;
        danoTotal += cantidad;
        VerificarGameOver();
    }

    void VerificarGameOver()
    {
        float porcentaje = ObtenerDanoPorcentaje100();
        Debug.Log($"[DANO] Total={danoTotal}, Max={danoMaximo}, %={porcentaje}");
        if (!gameOverActivado && ObtenerDanoPorcentaje100() >= 100f)
        {
            gameOverActivado = true;
            GameOver();
        }
    }

    public void GameOver()
    {
        //Time.timeScale = 0f;
        if (gameOverPanel)
            gameOverPanel.SetActive(true);

        Debug.Log("GAME OVER: daño máximo alcanzado");
    }

    // ======== GETTERS ========
    public int ObtenerTotalPasajeros() => totalPasajerosRecogidos;
    public float ObtenerDineroGanado() => dineroGanado;
    public float ObtenerDineroFinal() => dineroFinal;
    public float ObtenerTiempoTotal() => tiempoTotal;
    public float ObtenerTiempoRestante() => Mathf.Max(0f, tiempoPropuesto - tiempoTotal);
    public bool EstaEnOvertime() => tiempoTotal > tiempoPropuesto;
    public float ObtenerDanoTotal() => danoTotal;
    public float ObtenerDanoMaximo() => danoMaximo;
    public float ObtenerDanoPorcentaje01() => Mathf.Clamp01(danoTotal / Mathf.Max(0.0001f, danoMaximo));
    public float ObtenerDanoPorcentaje100() => ObtenerDanoPorcentaje01() * 100f;
    public float ObtenerDescuentoTiempo() => descuentoTiempo;
    public float ObtenerDescuentoDano() => descuentoDano;

    public string TiempoRestanteStr(bool allowNegative = true)
    {
        float diff = tiempoPropuesto - tiempoTotal;
        if (!allowNegative) diff = Mathf.Max(0f, diff);
        bool overtime = diff < 0f;
        float t = Mathf.Abs(diff);
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        return overtime && allowNegative ? $"+{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }
}
