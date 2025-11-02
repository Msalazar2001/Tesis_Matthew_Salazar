using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pasajero sin animaciones:
/// - Puede recibir rutas (waypoints) y caminar hacia ellos.
/// - Puede sentarse (parent al seatAnchor y alinear posición/rotación).
/// - Puede bajarse: reparent al mundo, posicionar en ExitAnchor y seguir waypoints de salida.
/// </summary>
public class Pasajero : MonoBehaviour
{
    [Header("Movimiento (sin animaciones)")]
    [SerializeField] float velocidad = 2.6f;
    [SerializeField] float giroSuave = 10f;
    [SerializeField] float distanciaArribo = 0.12f;
    [SerializeField] Animator animator;  // arrástralo en el prefab

    // ——— NUEVO: índice/turno en la fila de la parada ———
    [SerializeField, Tooltip("Orden en la fila de la parada (0,1,2,...)")]
    int turnoEnParada = int.MaxValue;
    public int TurnoEnParada => turnoEnParada;
    public void SetTurno(int t) { turnoEnParada = t; }

    bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (!animator) return false;
        foreach (var p in animator.parameters) if (p.type == type && p.name == name) return true;
        return false;
    }
    void SetSpeed(float v)
    {
        if (HasParam("speed", AnimatorControllerParameterType.Float)) animator.SetFloat("speed", v);
    }
    void Fire(string triggerName)
    {
        if (HasParam(triggerName, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(triggerName);
    }

    // Ruta actual
    Queue<Transform> ruta = new Queue<Transform>();
    Transform objetivo;

    // Estado simple
    bool moviendo = false;
    bool sentado = false;

    // Refs opcionales
    Transform seatAnchorActual;    // dónde está sentado
    Transform busRootActual;       // raíz del bus (si quieres mantener parenting dentro del bus)

    // =============== API PÚBLICA ===============

    /// <summary> Define una ruta de puntos a seguir (entrada, pasillo, etc.). </summary>
    public void SetRuta(params Transform[] puntos)
    {
        ruta.Clear();
        if (puntos != null)
            foreach (var p in puntos)
                if (p) ruta.Enqueue(p);

        objetivo = ruta.Count > 0 ? ruta.Dequeue() : null;
        moviendo = objetivo != null;
    }

    /// <summary> Compatibilidad: ruta de entrada (p. ej. waypoints + columna + asiento). </summary>
    public void AsignarRutaConEntrada(Transform[] waypointsEntrada, Transform columna, Transform asiento)
    {
        List<Transform> puntos = new List<Transform>();
        if (waypointsEntrada != null) puntos.AddRange(waypointsEntrada);
        if (columna) puntos.Add(columna);
        if (asiento) puntos.Add(asiento);
        SetRuta(puntos.ToArray());
    }

    /// <summary> Compatibilidad: asigna ruta de salida (camina hacia afuera). </summary>
    public void AsignarRutaDeSalida(Transform[] waypointsSalida)
    {
        ruta.Clear();
        if (waypointsSalida != null)
            foreach (var p in waypointsSalida)
                if (p) ruta.Enqueue(p);

        objetivo = ruta.Count > 0 ? ruta.Dequeue() : null;
        moviendo = objetivo != null;
        sentado = false; // ya no está sentado si va a salir
    }

    /// <summary> Coloca al pasajero sentado en un seatAnchor exacto (sin animación). </summary>
    public void AdoptarDelAsiento(Transform seatAnchor)
    {
        if (!seatAnchor) return;
        seatAnchorActual = seatAnchor;

        // Parent al asiento y alinear exacto
        transform.SetParent(seatAnchor, true);
        transform.position = seatAnchor.position;
        transform.rotation = seatAnchor.rotation;

        // Estado
        moviendo = false;
        sentado = true;
    }

    /// <summary> (Opcional) Indica el root del bus para mantener parenting interno. </summary>
    public void AdoptarDelBus(Transform busRoot)
    {
        busRootActual = busRoot;
        if (busRoot) transform.SetParent(busRoot, true);
    }

    /// <summary>
    /// Inicia la bajada: suelta del asiento/bus, lo coloca en ExitAnchor y le asigna la ruta de salida.
    /// </summary>
    public void IniciarBajada(Transform exitAnchor, Transform[] waypointsSalida, Transform nuevoPadreFuera = null)
    {
        // Soltar del asiento y (opcional) del bus
        seatAnchorActual = null;

        if (nuevoPadreFuera != null)
            transform.SetParent(nuevoPadreFuera, true);
        else
            transform.SetParent(null, true); // sin padre

        // Colocar en el punto de salida de la puerta
        if (exitAnchor)
        {
            transform.position = exitAnchor.position;
            transform.rotation = exitAnchor.rotation;
        }

        // Asignar ruta de salida
        AsignarRutaDeSalida(waypointsSalida);
    }

    void OnEnable()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            SetSpeed(0f);
            animator.Play("Idle", 0, 0f);  // nombre exacto del estado Idle
        }
    }

    // =============== LOOP DE MOVIMIENTO ===============

    void Update()
    {
        if (!moviendo || objetivo == null) return;

        // Dirección en plano
        Vector3 dir = objetivo.position - transform.position;
        Vector3 plano = new Vector3(dir.x, 0f, dir.z);
        float targetSpeed = (moviendo && objetivo != null) ? velocidad : 0f;
        SetSpeed(targetSpeed);

        // Rotar suave hacia el objetivo
        if (plano.sqrMagnitude > 0.0001f)
        {
            var rot = Quaternion.LookRotation(plano.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, giroSuave * Time.deltaTime);
        }

        // Avanzar
        float step = velocidad * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, objetivo.position, step);

        // Arribo
        if (Vector3.Distance(transform.position, objetivo.position) <= distanciaArribo)
        {
            OnArrive(objetivo);

            if (ruta.Count > 0)
            {
                objetivo = ruta.Dequeue();
            }
            else
            {
                objetivo = null;
                moviendo = false;
            }
        }
    }

    // =============== LLEGADA A WAYPOINT ===============

    void OnArrive(Transform wp)
    {
        // Si el waypoint tiene una acción de alineación, la respetamos.
        var act = wp.GetComponent<WaypointAction>();
        if (act)
        {
            // Intentamos usar alignTarget si existe; si no, el propio WP
            Transform t = act.alignTarget ? act.alignTarget : wp;

            // Alinear posición/rotación exacta
            transform.position = t.position;
            transform.rotation = Quaternion.LookRotation(t.forward, Vector3.up);

            // Animaciones “opcionales” y asiento
            bool esSentarse = (act && (act.TryIs("SitDown") || act.TryIs("Sitting")));
            if (esSentarse)
            {
                Fire("SitDown");
                AdoptarDelAsiento(t);
            }
            bool esLevantarse = (act && act.TryIs("StandUp"));
            if (esLevantarse)
            {
                Fire("StandUp");
                // tu flujo ya se encarga del resto
            }
        }
    }
}

// ================== EXTENSIÓN PEQUEÑA PARA WaypointAction ==================
public static class WaypointActionExtensions
{
    public static bool TryIs(this Component act, string friendlyName)
    {
        if (!act) return false;
        var t = act.GetType();

        // field "action" (enum/string)
        var fAction = t.GetField("action");
        if (fAction != null)
        {
            var v = fAction.GetValue(act);
            if (v != null && v.ToString().ToLower().Contains(friendlyName.ToLower()))
                return true;
        }

        // field "tipo" (enum/string)
        var fTipo = t.GetField("tipo");
        if (fTipo != null)
        {
            var v = fTipo.GetValue(act);
            if (v != null && v.ToString().ToLower().Contains(friendlyName.ToLower()))
                return true;
        }

        return false;
    }
}
