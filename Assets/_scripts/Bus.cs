using UnityEngine;

public class Bus : MonoBehaviour
{
    [SerializeField]
    int espacioDisponible = 3;

    public void ParadaDetectada(Parada parada)
    {
        print("El bus detect� una parada. Avisando cu�ntos pueden subir...");
        parada.RecibirBus(espacioDisponible);
    }

    public void SalirDeParada(Parada parada)
    {
        print("El bus ha salido de la parada.");
    }
}
