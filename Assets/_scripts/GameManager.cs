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
            float descuento = diferencia * penalizacionPorSegundo;

            print("El jugador se demoró " + diferencia + " segundos más de lo propuesto.");
            print("Penalización aplicada: $" + descuento);

            dineroFinal = dineroGanado - descuento;

            if (dineroFinal < 0)
            {
                dineroFinal = 0;
            }

            print("Dinero final con penalización: $" + dineroFinal);
        }
        else
        {
            dineroFinal = dineroGanado;
            print("El jugador completó el recorrido dentro del tiempo propuesto.");
            print("Dinero final: $" + dineroFinal);
        }
    }


    public void RecibirPasajerosRecogidos(int cantidad)
    {
        totalPasajerosRecogidos = cantidad;
        dineroGanado = totalPasajerosRecogidos * 2.5f;
        dineroFinal = dineroGanado;

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

    public void HacerDano(float t)
    {
        print(t);
    }
}
