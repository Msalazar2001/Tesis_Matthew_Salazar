using UnityEngine;

public class EntrarParada : MonoBehaviour
{
    [SerializeField]
     Bus bus;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Parada"))
        {
            Parada parada = other.GetComponent<Parada>();
            if (parada != null)
            {
                bus.ParadaDetectada(parada);
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
            }
        }
    }
}
