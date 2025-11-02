using UnityEngine;
using System.Collections.Generic;

public class Parada : MonoBehaviour
{
    [Header("Prefabs de pasajeros (varios modelos posibles)")]
    [SerializeField] List<GameObject> pasajeroPrefabs = new List<GameObject>();

    [Header("Configuración de spawn")]
    [SerializeField] Transform puntoSpawn;
    [Tooltip("Distancia entre pasajeros en la fila (sobre la dirección local elegida).")]
    [SerializeField] float separacionEntrePasajeros = 1.5f;

    [Tooltip("Mínimo y máximo de pasajeros a generar (si no es la última parada).")]
    [SerializeField] int minPasajeros = 1;
    [SerializeField] int maxPasajeros = 5;

    [Header("Direcciones (RELATIVAS a puntoSpawn)")]
    [Tooltip("Dirección LOCAL en la que se formará la fila. Por defecto 'Back' = hacia las bancas.")]
    [SerializeField] Vector3 direccionLocalFila = Vector3.back;   // hacia atrás del puntoSpawn

    [Tooltip("Dirección LOCAL hacia la que mirarán los pasajeros al spawnear. Por defecto 'Right' = hacia la carretera.")]
    [SerializeField] Vector3 direccionLocalMirada = Vector3.right; // hacia la calle

    [Header("Opcionales")]
    [Tooltip("Pequeño jitter lateral para que no queden milimétricamente alineados (0 = desactivado).")]
    [SerializeField] float jitterLateral = 0.0f;

    public int cantidadPasajeros;
    public bool ultimaParada = false;

    // Lista de pasajeros que están esperando en ESTA parada
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
        if (!puntoSpawn)
        {
            Debug.LogError("[Parada] No hay puntoSpawn asignado.");
            return;
        }

        if (pasajeroPrefabs == null || pasajeroPrefabs.Count == 0)
        {
            Debug.LogError("[Parada] No hay prefabs de pasajero en la lista.");
            return;
        }

        // Direcciones en ESPACIO MUNDO derivadas de las locales del puntoSpawn
        Vector3 dirFilaWorld = puntoSpawn.TransformDirection(direccionLocalFila).normalized;
        Vector3 dirMiradaWorld = puntoSpawn.TransformDirection(direccionLocalMirada).normalized;

        for (int i = 0; i < cantidad; i++)
        {
            // Prefab aleatorio
            GameObject prefab = pasajeroPrefabs[Random.Range(0, pasajeroPrefabs.Count)];

            // Posición base: puntoSpawn + i * separacion en la dirección "atrás" (local)
            Vector3 pos = puntoSpawn.position + dirFilaWorld * (i * separacionEntrePasajeros);

            // Jitter lateral opcional (perpendicular en el plano XZ)
            if (jitterLateral > 0f)
            {
                // Usamos la derecha local del puntoSpawn para un pequeño desplazamiento lateral
                Vector3 lateral = puntoSpawn.right;
                pos += lateral * Random.Range(-jitterLateral, jitterLateral);
            }

            // La rotación hace que miren hacia la carretera (dirección local configurada)
            Vector3 lookDir = dirMiradaWorld;
            lookDir.y = 0f; // evitamos inclinaciones
            if (lookDir.sqrMagnitude < 0.0001f) lookDir = Vector3.forward;
            Quaternion rot = Quaternion.LookRotation(lookDir, Vector3.up);

            GameObject pasajeroGO = Instantiate(prefab, pos, rot, transform);
            Pasajero pasajero = pasajeroGO.GetComponent<Pasajero>();
            if (pasajero != null)
            {
                // **ENUMERACIÓN**: 0,1,2,... según el orden de generación
                pasajero.SetTurno(i);
                pasajerosEnParada.Add(pasajero);
            }
        }
    }

    /// <summary>
    /// (Opcional / legacy) El bus llama a esta función indicando cuántos espacios libres tiene.
    /// Si sigues usando SubirPasajeroSecuencial ya no es necesario llamar a esto.
    /// </summary>
    public void RecibirBus(int espacioDisponible)
    {
        int pasajerosASubir = Mathf.Min(espacioDisponible, pasajerosEnParada.Count);

        for (int i = 0; i < pasajerosASubir; i++)
        {
            Pasajero p = pasajerosEnParada[0];
            pasajerosEnParada.RemoveAt(0);
            Destroy(p.gameObject);
        }
    }
}
