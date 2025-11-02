using System.Collections.Generic;
using UnityEngine;

public class TrafficCarAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;            // velocidad crucero
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float reachRadius = 1.5f;
    [SerializeField] private bool loop = true;

    [Header("Detection (genérica: bus + autos)")]
    [SerializeField] private float lookAhead = 50f;        // cuánto mira hacia adelante
    [SerializeField] private float castRadius = 0.5f;      // ancho del "cono" de detección
    [SerializeField] private LayerMask vehicleLayerMask;   // capa Vehicle (bus + autos)
    [SerializeField] private string busTag = "Bus";
    [SerializeField] private string carTag = "Car";

    [Header("Acceleration/Braking")]
    [SerializeField] private float accel = 4.5f;
    [SerializeField] private float brakeAccel = 7.5f;

    [Header("Stop / Gap Tuning")]
    [Tooltip("Comienza a frenar fuerte si el líder está dentro de esta distancia.")]
    [SerializeField] private float stopDistance = 20f;

    [Tooltip("Si el líder está más cerca que esto, detención total (0 m/s).")]
    [SerializeField] private float hardStopDistance = 6.0f;

    [Tooltip("Tiempo a colisión umbral: si TTC < este valor, frenar (seguridad dinámica).")]
    [SerializeField] private float ttcThreshold = 1.8f; // segundos

    [Tooltip("Velocidad objetivo mínima cuando hay líder cercano pero no en hard stop.")]
    [SerializeField] private float minCrawlSpeed = 0.5f;

    [Tooltip("Umbral para considerar detenido.")]
    [SerializeField] private float standStillEpsilon = 0.05f;

    private List<Transform> path;
    private int index;

    private float cruiseSpeed;
    private float currentSpeed;

    // -------------- API --------------
    public void SetPath(List<Transform> waypoints) => SetPath(waypoints, 0, true);

    public void SetPath(List<Transform> waypoints, int startIndex, bool snapToStart)
    {
        path = waypoints;
        if (path == null || path.Count == 0)
        {
            Debug.LogError($"{name}: SetPath recibió lista vacía.");
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
        currentSpeed = cruiseSpeed;
    }

    public void SetLoop(bool shouldLoop) => loop = shouldLoop;

    public void SetSpeed(float v)
    {
        speed = Mathf.Max(0f, v);
        cruiseSpeed = speed;
    }

    // -------------- Update --------------
    private void Update()
    {
        if (path == null || path.Count == 0) return;

        // 1) Girar hacia el waypoint actual
        Vector3 target = path[index].position;
        Vector3 to = target - transform.position;
        Vector3 dir = to.normalized;

        if (dir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // 2) Calcular velocidad deseada considerando LÍDER (bus o auto) delante
        float desired = cruiseSpeed;

        bool obstacleAhead = DetectObstacleAhead(
            out float dist,
            out Vector3 hitPoint,
            out Rigidbody leadRb,
            out GameObject leadObj
        );

        if (obstacleAhead)
        {
            // Velocidad del líder (si tiene Rigidbody)
            float leadSpeed = 0f;
            if (leadRb) leadSpeed = leadRb.linearVelocity.magnitude;

            // Relativa: (nuestra proyección forward) - (líder proyección hacia nuestro forward)
            float relSpeed = currentSpeed - Vector3.Dot(leadRb ? leadRb.linearVelocity : Vector3.zero, transform.forward);

            // 2.a) Zona crítica → parar
            if (dist <= hardStopDistance)
            {
                desired = 0f;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, (brakeAccel * 2f) * Time.deltaTime);
                if (currentSpeed <= standStillEpsilon || dist <= hardStopDistance * 0.9f)
                    currentSpeed = 0f;
            }
            else
            {
                // 2.b) Freno por distancia (interp 0..crucero)
                if (dist <= stopDistance)
                {
                    float t = Mathf.InverseLerp(0f, stopDistance, dist);
                    float distBased = Mathf.Lerp(minCrawlSpeed, cruiseSpeed, t);

                    // 2.c) Freno por TTC (si nos aproximamos rápido)
                    // ttc = distancia / velocidad_relativa_hacia_el_líder (solo si nos acercamos)
                    float ttc = Mathf.Infinity;
                    if (relSpeed > 0.05f) ttc = dist / relSpeed;

                    float ttcBased = (ttc < ttcThreshold) ? 0f : cruiseSpeed; // simple: bajo TTC, queremos 0

                    // 2.d) Seguir velocidad del líder si está cerca (evita empujones)
                    float followBased = Mathf.Max(leadSpeed, minCrawlSpeed);

                    // Toma el mínimo de los tres "limitadores"
                    desired = Mathf.Min(distBased, ttcBased, followBased);
                }
                else
                {
                    // fuera de zona de stop, pero con líder a la vista: da un colchón
                    desired = Mathf.Min(cruiseSpeed, Mathf.Max(leadSpeed * 1.05f, cruiseSpeed * 0.9f));
                }
            }
        }

        // 3) Suavizar velocidad (si no estamos en hard stop)
        if (!(obstacleAhead && dist <= hardStopDistance))
        {
            float a = (desired < currentSpeed) ? brakeAccel : accel;
            currentSpeed = Mathf.MoveTowards(currentSpeed, desired, a * Time.deltaTime);
        }

        // 4) Avanzar
        if (currentSpeed > 0f)
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

    // -------------- Obstacle detection (bus + autos) --------------
    // -------------- Obstacle detection (bus + autos) --------------
    private bool DetectObstacleAhead(out float dist, out Vector3 hitPoint, out Rigidbody leadRb, out GameObject leadObj)
    {
        dist = lookAhead;
        hitPoint = transform.position;
        leadRb = null;
        leadObj = null;

        // Origen adelantado para no autocolisionarse
        float forwardOffset = 0.5f;
        if (TryGetComponent<Collider>(out var myCol))
            forwardOffset = Mathf.Max(0.5f, myCol.bounds.extents.z + 0.2f);

        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * forwardOffset;

        // ---- Primera pasada: detectar vehículos en layer Vehicle ----
        var hits = Physics.SphereCastAll(
            origin,
            castRadius,
            transform.forward,
            lookAhead,
            vehicleLayerMask,
            QueryTriggerInteraction.Ignore
        );

        // ---- Segunda pasada: detectar bus en SU PROPIA LAYER ----
        var busHits = Physics.SphereCastAll(
            origin,
            castRadius,
            transform.forward,
            lookAhead,
            ~0, // Everything
            QueryTriggerInteraction.Ignore
        );

        // Combinamos ambas listas
        List<RaycastHit> allHits = new List<RaycastHit>();
        if (hits != null && hits.Length > 0) allHits.AddRange(hits);
        if (busHits != null && busHits.Length > 0)
        {
            foreach (var h in busHits)
            {
                var go = h.collider.attachedRigidbody ? h.collider.attachedRigidbody.gameObject : h.collider.gameObject;
                if (go.CompareTag(busTag)) allHits.Add(h);
            }
        }

        if (allHits.Count == 0) return false;

        // ---- Filtramos el más cercano realmente adelante ----
        float best = float.PositiveInfinity;
        RaycastHit bestHit = default;

        foreach (var h in allHits)
        {
            var go = h.collider.attachedRigidbody ? h.collider.attachedRigidbody.gameObject : h.collider.gameObject;

            // ignorar self
            if (go == gameObject || go.transform.IsChildOf(transform)) continue;

            // Solo considerar vehículos o bus (por tag)
            bool isVehicle = go.CompareTag(carTag) || go.CompareTag(busTag);
            if (!isVehicle) continue;

            // Solo si está adelante
            Vector3 toHit = (h.point - transform.position).normalized;
            if (Vector3.Dot(transform.forward, toHit) < 0.3f) continue;

            if (h.distance < best)
            {
                best = h.distance;
                bestHit = h;
            }
        }

        if (float.IsInfinity(best)) return false;

        dist = best;
        hitPoint = bestHit.point;
        leadObj = bestHit.collider.attachedRigidbody ? bestHit.collider.attachedRigidbody.gameObject : bestHit.collider.gameObject;
        leadRb = bestHit.rigidbody;

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

        Gizmos.color = Color.cyan;
        float forwardOffset = 0.5f;
        if (TryGetComponent<Collider>(out var myCol))
            forwardOffset = Mathf.Max(0.5f, myCol.bounds.extents.z + 0.2f);
        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * forwardOffset;
        Gizmos.DrawWireSphere(origin + transform.forward * lookAhead, castRadius);
        Gizmos.DrawLine(origin, origin + transform.forward * lookAhead);
    }
}
