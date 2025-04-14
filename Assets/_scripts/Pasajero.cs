using UnityEngine;
using UnityEngine.AI;

public class Pasajero : MonoBehaviour
{
    NavMeshAgent agente;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    public void IrHaciaElBus(Vector3 puntoEntrada)
    {
        agente.SetDestination(puntoEntrada);
    }
}

