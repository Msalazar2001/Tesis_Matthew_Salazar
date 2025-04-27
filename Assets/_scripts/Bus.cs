using System.Collections.Generic;
using BNG;
using UnityEngine;

public class Bus : MonoBehaviour
{
    [SerializeField] int espacioDisponible = 3;
    [SerializeField] VehicleController vehicleController;

    [SerializeField] Transform puntoEntradaPasajeros;
    [SerializeField] Transform[] waypointsEntradaBus;
    [SerializeField] Transform[] waypointsSalidaBus;
    [SerializeField] Transform[] columnas;
    [SerializeField] Transform[] asientos;

    Parada parada;

    public bool estoyEnParada = false;

    public int pasajerosActuales = 0;
    public int pasajerosBajando = 0;
    public int totalPasajerosRecogidos = 0;
    private bool[] asientosOcupados;

    void Awake()
    {
        asientosOcupados = new bool[asientos.Length];
    }

    public void ParadaDetectada(Parada parada)
    {
        this.parada = parada;

        pasajerosBajando = Random.Range(0, pasajerosActuales + 1);
        print("Se van a bajar " + pasajerosBajando + " pasajeros.");

        InvokeRepeating("BajarPasajeros", 1, 2);
        InvokeRepeating("SubirPasajeros", 1, 2);

        if (parada.ultimaParada)
        {
            EnviarTotalPasajeros();
            GameManager.Instance.CalcularValores();
            GameManager.Instance.DetenerCronometro();
        }
    }

    public void SalirDeParada(Parada parada)
    {
        print("El bus ha salido de la parada.");
    }

    private int BuscarPrimerAsientoLibre()
    {
        for (int i = 0; i < asientosOcupados.Length; i++)
        {
            if (!asientosOcupados[i])
                return i;
        }
        return -1;
    }

    public void SubirPasajeros()
    {
        List<Pasajero> pasajeros = parada.pasajerosEnParada;

        int cantidadASubir = Mathf.Min(pasajeros.Count, espacioDisponible);

        for (int i = 0; i < cantidadASubir; i++)
        {
            int asientoLibre = BuscarPrimerAsientoLibre();
            if (asientoLibre == -1) break;

            Pasajero pasajero = pasajeros[i];

            int columna = asientoLibre / 4;
            Transform puntoColumna = columnas[columna];
            Transform asientoDestino = asientos[asientoLibre];

            pasajero.AsignarRutaConEntrada(waypointsEntradaBus, puntoColumna, asientoDestino);

            asientosOcupados[asientoLibre] = true;

            print($"Pasajero ocupa COLUMNA {columna + 1}, ASIENTO {asientoLibre + 1}");

            pasajerosActuales++;
            espacioDisponible--;
            totalPasajerosRecogidos++;
            parada.cantidadPasajeros--;
        }

        parada.pasajerosEnParada.RemoveRange(0, cantidadASubir);

        if (parada.cantidadPasajeros == 0 || espacioDisponible == 0)
        {
            CancelInvoke("SubirPasajeros");
        }
    }

    public void BajarPasajeros()
    {
        if (pasajerosBajando > 0)
        {
            Pasajero pasajero = BuscarPasajeroEnBus();

            if (pasajero != null)
            {
                pasajero.AsignarRutaDeSalida(waypointsSalidaBus);

                for (int i = 0; i < asientos.Length; i++)
                {
                    if (pasajero.transform.parent == asientos[i])
                    {
                        asientosOcupados[i] = false;
                        break;
                    }
                }

                pasajerosActuales--;
                espacioDisponible++;
                pasajerosBajando--;

                print("Bajó un pasajero. Quedan: " + pasajerosActuales + ". Espacio disponible: " + espacioDisponible);
            }
        }

        if (pasajerosBajando == 0)
        {
            CancelInvoke("BajarPasajeros");
        }
    }

    private Pasajero BuscarPasajeroEnBus()
    {
        Pasajero[] pasajeros = FindObjectsByType<Pasajero>(FindObjectsSortMode.None);

        foreach (Pasajero p in pasajeros)
        {
            if (p.transform.parent != null && p.transform.parent.name.Contains("Asiento"))
            {
                return p;
            }
        }
        return null;
    }

    public int PasajerosRecogidos()
    {
        return totalPasajerosRecogidos;
    }

    public void EnviarTotalPasajeros()
    {
        GameManager.Instance.RecibirPasajerosRecogidos(totalPasajerosRecogidos);
        print("Total Pasajeros Recogidos: " + totalPasajerosRecogidos);
    }
}
