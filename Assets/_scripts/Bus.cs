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
            Time.timeScale = 0f;
            AudioListener.pause = true;
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
        int boarded = 0; // <-- NUEVO: contador de abordados en esta tanda

        for (int i = 0; i < cantidadASubir; i++)
        {
            int asientoLibre = BuscarPrimerAsientoLibre();
            if (asientoLibre == -1) break;

            Pasajero pasajero = pasajeros[i];

            int columna = asientoLibre / 4;
            Transform puntoColumna = columnas[columna];
            Transform asientoDestino = asientos[asientoLibre];

            pasajero.AsignarRutaConEntrada(waypointsEntradaBus, puntoColumna, asientoDestino);
            pasajero.ForzarCaminar();

            pasajero.AdoptarDelBus(transform);

            asientosOcupados[asientoLibre] = true;

            print($"Pasajero ocupa COLUMNA {columna + 1}, ASIENTO {asientoLibre + 1}");

            pasajerosActuales++;
            espacioDisponible--;
            totalPasajerosRecogidos++;
            parada.cantidadPasajeros--;
            boarded++; // <-- NUEVO
        }

        // Avisar al GameManager cuántos subieron en esta pasada
        if (boarded > 0)
        {
            GameManager.Instance?.PasajeroSubio(boarded); // <-- NUEVO
        }

        // Limpieza de la cola de la parada
        if (cantidadASubir > 0)
        {
            parada.pasajerosEnParada.RemoveRange(0, cantidadASubir);
        }

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
                // liberar asiento
                for (int i = 0; i < asientos.Length; i++)
                {
                    if (asientos[i] && pasajero.transform.IsChildOf(asientos[i]))
                    {
                        asientosOcupados[i] = false;
                        break;
                    }
                }

                // disparar bajada
                Transform busRoot = transform;
                Transform padreFuera = null; // o la Parada destino si quieres
                pasajero.PrepararBajada(waypointsSalidaBus, busRoot, padreFuera);

                // contadores como ya haces
                pasajerosActuales--;
                espacioDisponible++;
                pasajerosBajando--;

                print("Bajó un pasajero. Quedan: " + pasajerosActuales + ". Espacio disponible: " + espacioDisponible);
            }
            else
            {
                Debug.LogWarning("[Bus] No encontré pasajero sentado para bajar (¿asientos[] correcto? ¿parenting al SeatAnchor dentro del asiento?)");
            }
        }

        if (pasajerosBajando == 0)
            CancelInvoke("BajarPasajeros");
    }


    private Pasajero BuscarPasajeroEnBus()
    {
        Pasajero[] todos = FindObjectsByType<Pasajero>(FindObjectsSortMode.None);

        foreach (var p in todos)
        {
            if (!p || !p.transform) continue;

            // Debe ser hijo del BUS en cualquier profundidad
            if (!p.transform.IsChildOf(transform)) continue;

            // Si está sentado: su transform está dentro de alguno de los asientos[]
            for (int i = 0; i < asientos.Length; i++)
            {
                if (asientos[i] && p.transform.IsChildOf(asientos[i]))
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
