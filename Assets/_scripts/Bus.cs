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

    public void ParadaDetectada(Parada parada)
    {
        this.parada = parada;
        print("El bus detecto una parada y hay: " +parada.cantidadPasajeros);
        parada.RecibirBus(espacioDisponible);

        InvokeRepeating("SubirPasajeros", 1,2);

    }

    public void SalirDeParada(Parada parada)
    {
        print("El bus ha salido de la parada.");
    }
    private void Update()
    {
        //print(vehicleController.CurrentSpeed);
    }
    public void SubirPasajeros()
    {
        parada.cantidadPasajeros--;
        espacioDisponible--;
        if(parada.cantidadPasajeros==0 || espacioDisponible==0)
        {
            CancelInvoke("SubirPasajeros");
        }
    }

}
