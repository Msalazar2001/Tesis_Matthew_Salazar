using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Pasajeros y dinero
    int totalPasajerosRecogidos = 0;
    float dineroGanado = 0f;
    float dineroFinal = 0f;

    // Cronómetro (tiempo total desde que inicia el juego)
    float tiempoTotal = 0f;
    bool contarTiempo = false;
    [SerializeField]
    float tiempoPropuesto;

    float penalizacionPorSegundo = 1.5f;

    [SerializeField]
    float danoTotal=0;

    float descuento = 0;

    [SerializeField]
    float descuentoDano = 0;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        IniciarCronometro();
    }

    private void Update()
    {
        if (contarTiempo)
        {
            tiempoTotal += Time.deltaTime;
        }
    }

    public void IniciarCronometro()
    {
        tiempoTotal = 0f;
        contarTiempo = true;
    }

    public void DetenerCronometro()
    {
        contarTiempo = false;
        print("Cronómetro detenido.");
        print("Tiempo total del jugador: " + tiempoTotal + " segundos.");

        if (tiempoTotal > tiempoPropuesto)
        {
            float diferencia = tiempoTotal - tiempoPropuesto;
            descuento = diferencia * penalizacionPorSegundo;

            print("El jugador se demoró " + diferencia + " segundos más de lo propuesto.");
            print("Penalización aplicada: $" + descuento);

            if (dineroFinal < 0)
            {
                dineroFinal = 0;
            }

            //print("Dinero final con penalización: $" + dineroFinal);
        }
        else
        {
            dineroFinal = dineroGanado;
            print("El jugador completó el recorrido dentro del tiempo propuesto.");
        }
    }

    [SerializeField] float tarifaBase = 2.5f;

 
    public void PasajeroSubio(int cantidad = 1)
    {
        if (cantidad <= 0) return;
        totalPasajerosRecogidos += cantidad;
        dineroGanado += cantidad * tarifaBase;
    }
    public void RecibirPasajerosRecogidos(int cantidad)
    {
        totalPasajerosRecogidos = cantidad;
        dineroGanado = totalPasajerosRecogidos * 2.5f;
        //dineroFinal = dineroGanado;

        print("GameManager recibió: " + cantidad + " pasajeros.");
        print("Dinero final: $" + dineroFinal);
    }

    public int ObtenerTotalPasajeros()
    {
        return totalPasajerosRecogidos;
    }

    public float ObtenerDineroGanado()
    {
        return dineroGanado;
    }

    public float ObtenerDineroFinal()
    {
        return dineroFinal;
    }

    public float ObtenerTiempoTotal()
    {
        return tiempoTotal;
    }

    // Devuelve el tiempo restante (si es negativo, retorna 0)
    public float ObtenerTiempoRestante()
    {
        return Mathf.Max(0f, tiempoPropuesto - tiempoTotal);
    }

    // Devuelve true si ya se pasó del tiempo propuesto
    public bool EstaEnOvertime()
    {
        return tiempoTotal > tiempoPropuesto;
    }

    // Devuelve el tiempo restante en formato mm:ss
    // Si allowNegative = true, cuando estés en overtime devuelve "+mm:ss"
    public string TiempoRestanteStr(bool allowNegative = true)
    {
        float diff = tiempoPropuesto - tiempoTotal;

        if (!allowNegative)
        {
            diff = Mathf.Max(0f, diff);
        }

        bool overtime = diff < 0f;
        float t = Mathf.Abs(diff);

        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);

        if (overtime && allowNegative)
        {
            return $"+{m:00}:{s:00}";
        }
        else
        {
            return $"{m:00}:{s:00}";
        }
    }


    public void HacerDano(float t)
    {
       
        danoTotal += t;
        print(t);
        descuentoDano = danoTotal / 100;
        //dineroFinal = dineroGanado - danoTotal;
    }

    public float ObtenerDescuentoTiempo() { return descuento; }
    public float ObtenerDescuentoDano() { return descuentoDano; }

    public float CalcularValores()
    {
        float resultadoFinal = 0;
        resultadoFinal = dineroGanado-descuento-descuentoDano;

        print("----- RESUMEN FINAL -----");
        print("Pasajeros recogidos: " + totalPasajerosRecogidos);
        print("Dinero base ganado: " + dineroGanado);
        print("Descuento por tiempo: " + descuento);
        print("Daño total acumulado: " + descuentoDano);
        print("Resultado final: $" + resultadoFinal);

        return resultadoFinal;

    }

    // GameManager.cs (agrega al final de la clase)
    public void EndRun()
    {
        // Ya tienes estas llamadas repartidas; aquí las centralizamos si quieres llamarlo desde varios sitios
        DetenerCronometro();
        dineroFinal = CalcularValores(); // guarda el resultado final
        Debug.Log($"[EndRun] Dinero final guardado: ${dineroFinal:0.00}");
    }


}
