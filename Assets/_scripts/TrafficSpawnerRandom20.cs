using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class TrafficSpawnerRandom20_MultiLoops_Filtered : MonoBehaviour
{
    [Header("Prefabs de coches")]
    [SerializeField] private List<GameObject> carPrefabs = new List<GameObject>();

    [Header("Referencias")]
    [SerializeField] private Transform roadConstructorRoot;

    [Header("Spawn Settings")]
    [SerializeField, Min(1)] private int spawnCount = 20;
    [SerializeField] private float laneJitter = 0.6f;
    [SerializeField] private Vector2 speedRange = new Vector2(8f, 13f);

    [Header("Filtro de carriles")]
    [Tooltip("Tipos permitidos para coches. Coincide con el valor que veas en el Inspector del Waypoint.")]
    [SerializeField] private List<string> allowedLaneTypes = new List<string> { "Car" };
    [Tooltip("Tipos que se excluirán (peatones, veredas, bicis, etc.).")]
    [SerializeField] private List<string> bannedLaneTypes = new List<string> { "Pedestrian", "Sidewalk", "Footpath", "Walk", "Bike", "Bicycle" };
    [Tooltip("Si es true, el loop se corta si el siguiente waypoint cambia a un tipo no permitido.")]
    [SerializeField] private bool requireConsistentLaneType = true;

    private void Awake()
    {
        if (!roadConstructorRoot) { Debug.LogError("Asigna roadConstructorRoot."); return; }
        if (carPrefabs == null || carPrefabs.Count == 0) { Debug.LogError("Asigna al menos un prefab en 'carPrefabs'."); return; }

        var wps = FindWaypointComponents(roadConstructorRoot);
        if (wps.Count == 0) { Debug.LogError("No hay Waypoints. Crea/Actualiza en el editor."); return; }

        // Descubrir todos los loops SOLO de carriles permitidos
        var loops = BuildAllLoopsByNext_Filtered(wps, allowedLaneTypes, bannedLaneTypes, requireConsistentLaneType);
        if (loops.Count == 0) { Debug.LogError("No se encontraron loops válidos para coches (revisa Allowed/Banned Lane Types)."); return; }

        int totalLoops = loops.Count;
        int remaining = Mathf.Min(spawnCount, TotalWaypoints(loops));
        int perLoop = Mathf.Max(1, Mathf.FloorToInt((float)remaining / totalLoops));

        foreach (var loop in loops)
        {
            if (remaining <= 0) break;
            if (loop == null || loop.Count < 2) continue;

            int n = Mathf.Min(perLoop, loop.Count, remaining);
            var indices = UniqueRandomIndices(loop.Count, n);

            foreach (int idx in indices)
            {
                int nextIdx = (idx + 1) % loop.Count;
                Vector3 pos = loop[idx].position;
                Vector3 fwd = (loop[nextIdx].position - loop[idx].position).normalized;

                if (laneJitter > 0f)
                {
                    Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                    pos += right * Random.Range(-laneJitter, laneJitter);
                }

                var prefab = carPrefabs[Random.Range(0, carPrefabs.Count)];
                var go = Instantiate(prefab, pos, Quaternion.LookRotation(fwd, Vector3.up));
                var ai = go.GetComponent<TrafficCarAI>() ?? go.AddComponent<TrafficCarAI>();
                ai.SetPath(loop, startIndex: idx, snapToStart: false);
                ai.SetLoop(true);
                ai.SetSpeed(Random.Range(speedRange.x, speedRange.y));

                remaining--;
                if (remaining <= 0) break;
            }
        }

        Debug.Log($"Spawns: {spawnCount} autos distribuidos en {loops.Count} loops de tipo permitido.");
    }

    // ---------------- Loops filtrados por LaneType ----------------

    private List<List<Transform>> BuildAllLoopsByNext_Filtered(
        List<Component> all,
        List<string> allow,
        List<string> ban,
        bool requireConsistent)
    {
        var loops = new List<List<Transform>>();
        var globalVisited = new HashSet<int>();

        // Intentar empezar por StartPoints si existen, si no, por todos
        var starts = new List<Component>();
        foreach (var wp in all)
            if (GetBool(wp, "StartPoint", "Start Point", "IsStart", "Start", "m_StartPoint")) starts.Add(wp);
        if (starts.Count == 0) starts.AddRange(all);

        foreach (var start in starts)
        {
            if (start == null) continue;
            if (globalVisited.Contains(start.GetInstanceID())) continue;

            // Filtra el inicio
            if (!IsWaypointAllowed(start, allow, ban)) continue;

            var loop = FollowLoopByNext_Filtered(start, globalVisited, allow, ban, requireConsistent);
            if (loop != null && loop.Count >= 2)
                loops.Add(loop);
        }
        return loops;
    }

    private List<Transform> FollowLoopByNext_Filtered(
        Component start,
        HashSet<int> globalVisited,
        List<string> allow,
        List<string> ban,
        bool requireConsistent,
        int guardMax = 50000)
    {
        var loop = new List<Transform>();
        var localVisited = new HashSet<int>();

        var current = start;
        int startId = start.GetInstanceID();
        int guard = 0;

        // LaneType de referencia para mantener consistencia (si se requiere)
        string baseLane = GetLaneType(current);

        while (current != null && guard++ < guardMax)
        {
            int id = current.GetInstanceID();
            if (localVisited.Contains(id)) break; // ciclo
            localVisited.Add(id);
            globalVisited.Add(id);

            if (!IsWaypointAllowed(current, allow, ban)) break;
            if (requireConsistent && !LaneTypeMatches(baseLane, GetLaneType(current), allow)) break;

            if (current.transform) loop.Add(current.transform);

            var nextList = GetIList(current, "Next", "NextPoints", "NextWaypoints", "Nexts", "next", "m_Next");
            if (nextList == null || nextList.Count == 0) break;

            var next = nextList[0] as Component;
            if (next == null) break;
            if (next.GetInstanceID() == startId) break; // cerró ciclo
            if (globalVisited.Contains(next.GetInstanceID())) break; // ya pertenece a otro loop

            // Validar siguiente según filtro
            if (!IsWaypointAllowed(next, allow, ban)) break;
            if (requireConsistent && !LaneTypeMatches(baseLane, GetLaneType(next), allow)) break;

            current = next;
        }

        return loop.Count >= 2 ? loop : null;
    }

    // ---------------- Helpers de LaneType / reflexión ----------------

    private static bool IsWaypointAllowed(Component wp, List<string> allow, List<string> ban)
    {
        // Si el asset expone un bool explícito de peatón, respétalo
        if (GetBool(wp, "IsPedestrian", "Pedestrian", "m_IsPedestrian"))
            return false;

        string lane = GetLaneType(wp);

        // Ban tiene prioridad
        if (!string.IsNullOrEmpty(lane))
        {
            foreach (var b in ban)
                if (!string.IsNullOrEmpty(b) && lane == b) return false;
        }

        // Si hay allow definido, exigir coincidencia
        if (allow != null && allow.Count > 0)
        {
            foreach (var a in allow)
                if (!string.IsNullOrEmpty(a) && lane == a) return true;
            // si no coincidió con ninguno permitido, no permitir
            return false;
        }

        // Si no hay listas, permitir por defecto
        return true;
    }

    private static bool LaneTypeMatches(string baseLane, string candidateLane, List<string> allow)
    {
        if (string.IsNullOrEmpty(baseLane) || string.IsNullOrEmpty(candidateLane)) return true;
        if (baseLane == candidateLane) return true;
        // Si ambos están en la lista allow, considerarlos compatibles
        if (allow != null && allow.Count > 0)
            return allow.Contains(baseLane) && allow.Contains(candidateLane);
        return false;
    }

    private static string GetLaneType(object obj)
    {
        return GetString(obj, "LaneType", "Lane Type", "Type", "laneType", "m_LaneType");
    }

    private List<Component> FindWaypointComponents(Transform root)
    {
        var results = new List<Component>();
        foreach (var c in root.GetComponentsInChildren<Component>(true))
            if (c != null && c.GetType().Name == "Waypoint") results.Add(c);
        return results;
    }

    private static int TotalWaypoints(List<List<Transform>> loops)
    {
        int s = 0; foreach (var l in loops) if (l != null) s += l.Count; return s;
    }

    private static List<int> UniqueRandomIndices(int maxExclusive, int count)
    {
        var list = new List<int>(maxExclusive);
        for (int i = 0; i < maxExclusive; i++) list.Add(i);
        for (int i = 0; i < count; i++)
        {
            int j = Random.Range(i, maxExclusive);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list.GetRange(0, count);
    }

    private static bool GetBool(object obj, params string[] names)
    {
        if (obj == null) return false;
        var t = obj.GetType();
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;
        foreach (var n in names)
        {
            var p = t.GetProperty(n, F); if (p != null && p.GetValue(obj) is bool pb) return pb;
            var f = t.GetField(n, F); if (f != null && f.GetValue(obj) is bool fb) return fb;
        }
        return false;
    }

    private static string GetString(object obj, params string[] names)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;
        foreach (var n in names)
        {
            var p = t.GetProperty(n, F); if (p != null) { var v = p.GetValue(obj); if (v != null) return v.ToString(); }
            var f = t.GetField(n, F); if (f != null) { var v = f.GetValue(obj); if (v != null) return v.ToString(); }
        }
        return null;
    }

    private static IList GetIList(object obj, params string[] names)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;
        foreach (var n in names)
        {
            var p = t.GetProperty(n, F); if (p != null) { var v = p.GetValue(obj) as IList; if (v != null) return v; }
            var f = t.GetField(n, F); if (f != null) { var v = f.GetValue(obj) as IList; if (v != null) return v; }
        }
        return null;
    }
}
