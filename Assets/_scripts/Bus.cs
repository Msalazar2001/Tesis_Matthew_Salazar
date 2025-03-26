using BNG;
using UnityEngine;

public class Bus : MonoBehaviour
{
    [SerializeField]
    int espacioDisponible = 3;

    [SerializeField]
    VehicleController vehicleController;

    Parada parada;

    public bool estoyEnParada = false;

    public int pasajerosActuales = 0;
    public int pasajerosBajando = 0;
    public int totalPasajerosRecogidos = 0;
    public void ParadaDetectada(Parada parada)
    {
        this.parada = parada;

        // Elegir cuántos pasajeros se van a bajar en esta parada
        pasajerosBajando = Random.Range(0, pasajerosActuales + 1);
        print("Se van a bajar " + pasajerosBajando + " pasajeros.");

        InvokeRepeating("BajarPasajeros", 1, 2); // bajar de a uno
        InvokeRepeating("SubirPasajeros", 1, 2); // subir de a uno

        if(parada.ultimaParada)
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
        if (parada.cantidadPasajeros > 0 && espacioDisponible > 0)
        {
            parada.cantidadPasajeros--;
            espacioDisponible--;
            pasajerosActuales++;
            totalPasajerosRecogidos++;
            print("Subió un pasajero. Total pasajeros: " + pasajerosActuales + ". Espacio disponible: " + espacioDisponible);
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
        print("Total Pasajeros Recogidos" + totalPasajerosRecogidos);
    }
}
