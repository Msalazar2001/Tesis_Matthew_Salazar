using System.Collections.Generic;
using BNG;
using UnityEngine;

public class Bus : MonoBehaviour
{
    [SerializeField] int espacioDisponible = 3;
    [SerializeField] VehicleController vehicleController;

    [SerializeField] Transform puntoEntradaPasajeros; // entrada general al bus
    [SerializeField] Transform[] waypointsEntradaBus;
    [SerializeField] Transform[] columnas;               // 3 columnas visuales
    [SerializeField] Transform[] asientos;               // 12 asientos en orden

    Parada parada;

    public bool estoyEnParada = false;

    public int pasajerosActuales = 0;
    public int pasajerosBajando = 0;
    public int totalPasajerosRecogidos = 0;
    int indexAsiento = 0;

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

    public void SubirPasajeros()
    {
        List<Pasajero> pasajeros = parada.pasajerosEnParada;

        int cantidadASubir = Mathf.Min(pasajeros.Count, espacioDisponible);

        for (int i = 0; i < cantidadASubir && indexAsiento < asientos.Length; i++)
        {
            Pasajero pasajero = pasajeros[i];

            int columna = indexAsiento / 4;
            Transform puntoColumna = columnas[columna];
            Transform asientoDestino = asientos[indexAsiento];

            pasajero.AsignarRutaConEntrada(waypointsEntradaBus, puntoColumna, asientoDestino);

            print($"Pasajero {indexAsiento} → COLUMNA {columna + 1}, ASIENTO {indexAsiento + 1}");

            indexAsiento++;
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
            pasajerosActuales--;
            espacioDisponible++;
            pasajerosBajando--;
            print("Bajó un pasajero. Quedan: " + pasajerosActuales + ". Espacio disponible: " + espacioDisponible);
        }

        if (pasajerosBajando == 0)
        {
            CancelInvoke("BajarPasajeros");
        }
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
