using System.Collections.Generic;
using System.Linq;
using BNG;
using UnityEngine;

public class Bus : MonoBehaviour
{
    [Header("Capacidad")]
    [SerializeField] int espacioDisponible = 3;
    [SerializeField] VehicleController vehicleController;

    [Header("Waypoints / Anclajes")]
    [SerializeField] Transform puntoEntradaPasajeros;   // opcional
    [SerializeField] Transform exitAnchor;              // punto en la puerta para bajar
    [SerializeField] Transform[] waypointsEntradaBus;   // pasillo de entrada
    [SerializeField] Transform[] waypointsSalidaBus;    // ruta de salida (en el suelo, no hijos del bus)
    [SerializeField] Transform[] columnas;              // columnas/pasillo interno
    [SerializeField] Transform[] asientos;              // seat anchors (donde se sientan)
    [SerializeField] EndRunPanel endRunPanel;

    Parada parada;

    public bool estoyEnParada = false;

    public int pasajerosActuales = 0;
    public int pasajerosBajando = 0;
    public int totalPasajerosRecogidos = 0;
    private bool[] asientosOcupados;

    void Awake()
    {
        asientosOcupados = new bool[asientos.Length];
    }

    public void ParadaDetectada(Parada parada)
    {
        this.parada = parada;

        pasajerosBajando = Random.Range(0, pasajerosActuales + 1);
        print("Se van a bajar " + pasajerosBajando + " pasajeros.");

        // Bajadas tal como lo tenías
        InvokeRepeating(nameof(BajarPasajeros), 1f, 2f);

        // SUBIDAS **SECUENCIALES**: 1 pasajero por tick, cada 1 segundo, según su índice
        InvokeRepeating(nameof(SubirPasajeroSecuencial), 1f, 1f);

        if (parada.ultimaParada)
        {
            EnviarTotalPasajeros();
            GameManager.Instance.EndRun();
            if (endRunPanel != null) endRunPanel.Show();
            else Debug.LogError("[Bus] EndRunPanel no está asignado en el Inspector.");
        }
    }

    public void SalirDeParada(Parada parada)
    {
        print("El bus ha salido de la parada.");
    }

    private int BuscarPrimerAsientoLibre()
    {
        for (int i = 0; i < asientosOcupados.Length; i++)
        {
            if (!asientosOcupados[i])
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Nuevo: sube exactamente 1 pasajero por segundo, respetando el orden (turno) asignado en la Parada.
    /// </summary>
    private void SubirPasajeroSecuencial()
    {
        if (parada == null) { CancelInvoke(nameof(SubirPasajeroSecuencial)); return; }
        if (espacioDisponible <= 0) { CancelInvoke(nameof(SubirPasajeroSecuencial)); return; }

        var lista = parada.pasajerosEnParada;
        if (lista == null || lista.Count == 0) { CancelInvoke(nameof(SubirPasajeroSecuencial)); return; }

        int asientoLibre = BuscarPrimerAsientoLibre();
        if (asientoLibre == -1)
        {
            // No hay más asientos → detenemos la subida
            CancelInvoke(nameof(SubirPasajeroSecuencial));
            return;
        }

        // Elegimos el pasajero con menor "turno" (orden en la fila)
        Pasajero siguiente = null;
        int minTurno = int.MaxValue;
        int indexEnLista = -1;

        for (int i = 0; i < lista.Count; i++)
        {
            var p = lista[i];
            if (!p) continue;
            if (p.TurnoEnParada < minTurno)
            {
                minTurno = p.TurnoEnParada;
                siguiente = p;
                indexEnLista = i;
            }
        }

        if (siguiente == null)
        {
            // Algo raro: no hay pasajero válido
            CancelInvoke(nameof(SubirPasajeroSecuencial));
            return;
        }

        // Columna según layout (cada 4 asientos una columna)
        int columna = Mathf.Clamp(asientoLibre / 4, 0, Mathf.Max(0, columnas.Length - 1));
        Transform puntoColumna = columnas.Length > 0 ? columnas[columna] : null;
        Transform asientoDestino = asientos[asientoLibre];

        // Ruta de entrada (pasillo -> columna -> asiento)
        siguiente.AsignarRutaConEntrada(waypointsEntradaBus, puntoColumna, asientoDestino);

        // Parent bajo el bus mientras está adentro (sin animaciones)
        siguiente.AdoptarDelBus(transform);

        // Marcar asiento
        asientosOcupados[asientoLibre] = true;

        print($"[Bus] Sube pasajero #{siguiente.TurnoEnParada} → COLUMNA {columna + 1}, ASIENTO {asientoLibre + 1}");

        pasajerosActuales++;
        espacioDisponible--;
        totalPasajerosRecogidos++;
        parada.cantidadPasajeros--;

        // Removerlo de la parada
        if (indexEnLista >= 0 && indexEnLista < lista.Count)
            lista.RemoveAt(indexEnLista);

        // Notificar (1 pasajero subido)
        GameManager.Instance?.PasajeroSubio(1);

        // Si ya no hay pasajeros o no hay espacio, detener
        if (parada.cantidadPasajeros <= 0 || espacioDisponible <= 0)
            CancelInvoke(nameof(SubirPasajeroSecuencial));
    }

    // ————————————————————————————————————————————————
    // Dejo tu lógica de bajada igual
    public void BajarPasajeros()
    {
        if (pasajerosBajando > 0)
        {
            Pasajero pasajero = BuscarPasajeroEnBus();

            if (pasajero != null)
            {
                // liberar asiento correspondiente
                for (int i = 0; i < asientos.Length; i++)
                {
                    if (asientos[i] && pasajero.transform.IsChildOf(asientos[i]))
                    {
                        asientosOcupados[i] = false;
                        break;
                    }
                }

                // Punto de salida (fallback al primer WP si no hay exitAnchor)
                Transform salida = exitAnchor != null
                    ? exitAnchor
                    : (waypointsSalidaBus != null && waypointsSalidaBus.Length > 0 ? waypointsSalidaBus[0] : null);

                // Iniciar bajada SIN animaciones
                pasajero.IniciarBajada(salida, waypointsSalidaBus, null);

                // contadores
                pasajerosActuales--;
                espacioDisponible++;
                pasajerosBajando--;

                print("Bajó un pasajero. Quedan: " + pasajerosActuales + ". Espacio disponible: " + espacioDisponible);
            }
            else
            {
                Debug.LogWarning("[Bus] No encontré pasajero sentado para bajar (¿asientos[] correcto? ¿parenting al SeatAnchor dentro del asiento?)");
            }
        }

        if (pasajerosBajando == 0)
            CancelInvoke(nameof(BajarPasajeros));
    }

    private Pasajero BuscarPasajeroEnBus()
    {
        Pasajero[] todos = FindObjectsByType<Pasajero>(FindObjectsSortMode.None);

        foreach (var p in todos)
        {
            if (!p || !p.transform) continue;

            // debe ser hijo del bus
            if (!p.transform.IsChildOf(transform)) continue;

            // sentado si es hijo de algún asiento
            for (int i = 0; i < asientos.Length; i++)
            {
                if (asientos[i] && p.transform.IsChildOf(asientos[i]))
                    return p;
            }
        }
        return null;
    }

    public int PasajerosRecogidos() => totalPasajerosRecogidos;

    public void EnviarTotalPasajeros()
    {
        GameManager.Instance.RecibirPasajerosRecogidos(totalPasajerosRecogidos);
        print("Total Pasajeros Recogidos: " + totalPasajerosRecogidos);
    }
}
