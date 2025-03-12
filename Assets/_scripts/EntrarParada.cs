using BNG;
using UnityEngine;

public class EntrarParada : MonoBehaviour
{
    [SerializeField]
     Bus bus;

    [SerializeField]
    VehicleController vehicleController;

    bool envioParada = false;

    private void OnTriggerStay(Collider other)
    {

        if (other.CompareTag("Parada") && vehicleController.CurrentSpeed==0 && !envioParada)
        {
            print("Llegamos");
            Parada parada = other.GetComponent<Parada>();
            if (parada != null)
            {
                bus.ParadaDetectada(parada);
                envioParada=true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Parada"))
        {
            Parada parada = other.GetComponent<Parada>();
            if (parada != null)
            {
                bus.SalirDeParada(parada); //
                envioParada=false;
            }

        }
    }

    
}
