using System.Collections.Generic;
using UnityEngine;

public class TrafficCarAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;            // velocidad de crucero (objetivo)
    [SerializeField] private float turnSpeed = 6f;         // rapidez de giro
    [SerializeField] private float reachRadius = 1.5f;     // radio para pasar al siguiente waypoint
    [SerializeField] private bool loop = true;             // repetir recorrido

    [Header("BUS detection (follow logic)")]
    [SerializeField] private string busTag = "Bus";        // tag exacto del bus
    [SerializeField] private float lookAhead = 50f;        // distancia de detección delante
    [SerializeField] private float castRadius = 0.7f;      // “ancho” del vehículo para el SphereCast
    [SerializeField] private float minSpeedNearBus = 0f; // velocidad mínima cuando hay bus muy cerca
    [SerializeField] private float accel = 4.5f;           // aceleración al subir a crucero
    [SerializeField] private float brakeAccel = 7.5f;      // desaceleración al frenar por el bus
    [SerializeField] private LayerMask busLayerMask = ~0;  // opcional: capa del bus; si no, usa Default

    private List<Transform> path;
    private int index;

    // control interno de velocidad (para suavidad)
    private float cruiseSpeed;    // = speed (objetivo)
    private float currentSpeed;   // velocidad real interpolada

    // -------------- API --------------
    public void SetPath(List<Transform> waypoints) => SetPath(waypoints, 0, true);

    public void SetPath(List<Transform> waypoints, int startIndex, bool snapToStart)
    {
        path = waypoints;
        if (path == null || path.Count == 0)
        {
            Debug.LogError($"{name}: SetPath recibió una lista vacía.");
            enabled = false;
            return;
        }

        index = Mathf.Clamp(startIndex, 0, path.Count - 1);

        if (snapToStart)
        {
            transform.position = path[index].position;
            Vector3 fwd = (path[(index + 1) % path.Count].position - path[index].position).normalized;
            if (fwd.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        cruiseSpeed = Mathf.Max(0f, speed);
        currentSpeed = cruiseSpeed; // arranca a crucero
    }

    public void SetLoop(bool shouldLoop) => loop = shouldLoop;

    public void SetSpeed(float v)
    {
        speed = Mathf.Max(0f, v);
        cruiseSpeed = speed; // actualiza crucero; currentSpeed subirá/bajará suavemente
    }

    // -------------- Update --------------
    private void Update()
    {
        if (path == null || path.Count == 0) return;

        // 1) Apuntar al waypoint actual
        Vector3 target = path[index].position;
        Vector3 to = target - transform.position;
        Vector3 dir = to.normalized;

        if (dir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // 2) Calcular velocidad deseada SOLO por presencia de BUS
        float desired = cruiseSpeed;
        if (DetectBusAhead(out float busDist))
        {
            // Escala lineal según distancia: más cerca => más freno (hasta minSpeedNearBus)
            float t = Mathf.Clamp01(busDist / Mathf.Max(0.1f, lookAhead));
            desired = Mathf.Lerp(minSpeedNearBus, cruiseSpeed, t);
        }

        // 3) Suavizar velocidad actual hacia desired
        float a = (desired < currentSpeed) ? brakeAccel : accel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desired, a * Time.deltaTime);

        // 4) Avanzar
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // 5) Consumir waypoints
        int safety = 0;
        while (to.magnitude <= reachRadius && safety++ < 20)
        {
            index++;
            if (index >= path.Count)
            {
                if (loop) index = 0;
                else { enabled = false; }
                break;
            }
            target = path[index].position;
            to = target - transform.position;
        }
    }

    // -------------- BUS detection --------------
    private bool DetectBusAhead(out float dist)
    {
        dist = lookAhead;

        // Empuja el origen un poco hacia adelante para no tocarnos a nosotros mismos
        float forwardOffset = 0.5f;
        if (TryGetComponent<Collider>(out var myCol))
            forwardOffset = Mathf.Max(0.5f, myCol.bounds.extents.z + 0.2f);

        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * forwardOffset;

        // Usamos SphereCastAll para filtrar manualmente
        var hits = Physics.SphereCastAll(
            origin,
            castRadius,
            transform.forward,
            lookAhead,
            busLayerMask, // si no usas una capa específica, deja "Everything" (default ~0)
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0) return false;

        float best = float.PositiveInfinity;

        foreach (var h in hits)
        {
            var go = h.collider.attachedRigidbody ? h.collider.attachedRigidbody.gameObject : h.collider.gameObject;

            // ignora self y sus hijos
            if (go == gameObject || go.transform.IsChildOf(transform)) continue;

            // exige tag BUS
            if (!go.CompareTag(busTag)) continue;

            // confirmar que está realmente enfrente (no lateral/atrás)
            Vector3 toHit = (h.point - transform.position).normalized;
            if (Vector3.Dot(transform.forward, toHit) < 0.3f) continue;

            // mantener la distancia mínima
            if (h.distance < best) best = h.distance;
        }

        if (float.IsInfinity(best)) return false;

        dist = best;
        return true;
    }

    // -------------- Gizmos --------------
    private void OnDrawGizmosSelected()
    {
        if (path != null && path.Count >= 2)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(path[i].position, path[i + 1].position);
            Gizmos.DrawLine(path[^1].position, path[0].position);
        }

        // Visual del detector
        Gizmos.color = Color.cyan;
        float forwardOffset = 0.5f;
        if (TryGetComponent<Collider>(out var myCol))
            forwardOffset = Mathf.Max(0.5f, myCol.bounds.extents.z + 0.2f);
        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * forwardOffset;
        Gizmos.DrawWireSphere(origin + transform.forward * lookAhead, castRadius);
        Gizmos.DrawLine(origin, origin + transform.forward * lookAhead);
    }
}
