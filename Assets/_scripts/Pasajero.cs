using UnityEngine;
using UnityEngine.AI;

public class Pasajero : MonoBehaviour
{
    NavMeshAgent agente;
    [SerializeField]
    Transform destino;
    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    public void IrHaciaElBus(Vector3 puntoEntrada)
    {
        agente.SetDestination(puntoEntrada);
    }

    private void Update()
    {
        agente.SetDestination(destino.position);
    }

}

