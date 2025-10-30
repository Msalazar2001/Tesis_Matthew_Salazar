using UnityEngine;

public class WaypointAction : MonoBehaviour
{
    public enum ActionType { None, Stairs, SitDown, SittingIdle, Talking, StandUp, AlignOnly }
    public ActionType action = ActionType.None;

    [Header("Alineación opcional (silla/escalón/pivote)")]
    public Transform alignTarget; // si no se asigna, usa este mismo transform
    public Vector3 localOffset = new Vector3(0f, -0.6f, 0f); // baja ~6 cm por defecto
}
