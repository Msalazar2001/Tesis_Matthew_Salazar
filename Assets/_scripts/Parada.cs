using UnityEngine;
using System.Collections.Generic;

public class Parada : MonoBehaviour
{
    [SerializeField]
     GameObject pasajeroPrefab;
    [SerializeField]
     Transform puntoSpawn;
    [SerializeField]
     float separacionEntrePasajeros = 1.5f;
    [SerializeField]
     int minPasajeros = 1;
    [SerializeField]
     int maxPasajeros = 5;

    private List<GameObject> pasajerosEnParada = new List<GameObject>();

    void Start()
    {
        int cantidadPasajeros = Random.Range(minPasajeros, maxPasajeros + 1);
        GenerarPasajeros(cantidadPasajeros);
    }

    private void GenerarPasajeros(int cantidad)
    {
        print($"Generando {cantidad} pasajeros en la parada.");

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 offset = new Vector3(i * separacionEntrePasajeros, 0, 0);
            GameObject pasajero = Instantiate(pasajeroPrefab, puntoSpawn.position + offset, Quaternion.identity, transform);
            pasajerosEnParada.Add(pasajero);
        }
    }

    public void RecibirBus(int espacioDisponible)
    {
        int pasajerosASubir = Mathf.Min(espacioDisponible, pasajerosEnParada.Count);
        print($"Subiendo {pasajerosASubir} pasajeros al bus.");

        for (int i = 0; i < pasajerosASubir; i++)
        {
            GameObject pasajero = pasajerosEnParada[0];
            pasajerosEnParada.RemoveAt(0);
            Destroy(pasajero);
        }
    }
}
