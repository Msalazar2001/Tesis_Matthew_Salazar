using UnityEngine;
using System.Collections.Generic;

public class Parada : MonoBehaviour
{
    [SerializeField] GameObject pasajeroPrefab;
    [SerializeField] Transform puntoSpawn;
    [SerializeField] float separacionEntrePasajeros = 1.5f;
    [SerializeField] int minPasajeros = 1;
    [SerializeField] int maxPasajeros = 5;
    public int cantidadPasajeros;

    public bool ultimaParada = false;

    public List<Pasajero> pasajerosEnParada = new List<Pasajero>();

    void Start()
    {
        if (!ultimaParada)
        {
            cantidadPasajeros = Random.Range(minPasajeros, maxPasajeros + 1);
            GenerarPasajeros(cantidadPasajeros);
        }
        else
        {
            cantidadPasajeros = 0;
        }
    }

    private void GenerarPasajeros(int cantidad)
    {
        print($"Generando {cantidad} pasajeros en la parada.");

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 offset = new Vector3(i * separacionEntrePasajeros, 0, 0);
            GameObject pasajeroGO = Instantiate(pasajeroPrefab, puntoSpawn.position + offset, Quaternion.identity, transform);
            Pasajero pasajero = pasajeroGO.GetComponent<Pasajero>();

            if (pasajero != null)
            {
                pasajerosEnParada.Add(pasajero);
            }
        }
    }

    public void RecibirBus(int espacioDisponible)
    {
        int pasajerosASubir = Mathf.Min(espacioDisponible, pasajerosEnParada.Count);
        print($"Subiendo {pasajerosASubir} pasajeros al bus.");

        for (int i = 0; i < pasajerosASubir; i++)
        {
            Pasajero pasajero = pasajerosEnParada[0];
            pasajerosEnParada.RemoveAt(0);
            Destroy(pasajero.gameObject);
        }
    }
}