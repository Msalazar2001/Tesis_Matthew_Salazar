using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    int totalPasajerosRecogidos = 0;
    private void Awake()
    {
        Instance = this;
    }

    public void RecibirPasajerosRecogidos(int cantidad)
    {
        totalPasajerosRecogidos = cantidad;
        print("GameManager recibio:" + cantidad + "pasajeros");
    }
}
