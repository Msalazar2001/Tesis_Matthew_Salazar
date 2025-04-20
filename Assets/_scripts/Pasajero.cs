using UnityEngine;
using System.Collections.Generic;

public class Pasajero : MonoBehaviour
{
    float velocidad = 3f;
    Queue<Transform> ruta = new Queue<Transform>();
    Transform objetivoActual;
    bool enMovimiento = false;
    public Parada paradaOrigen;


    // Llama esta función desde el bus con: pasajero.AsignarRutaConEntrada(waypoints, columna, asiento);
    public void AsignarRutaConEntrada(Transform[] waypointsEntrada, Transform columna, Transform asiento)
    {
        ruta.Clear();

        // Añadir los 3 waypoints de entrada al bus
        foreach (Transform punto in waypointsEntrada)
        {
            ruta.Enqueue(punto);
        }

        // Luego, ir al transform de la columna
        ruta.Enqueue(columna);

        // Y por último, al asiento final
        ruta.Enqueue(asiento);

        if (ruta.Count > 0)
        {
            objetivoActual = ruta.Dequeue();
            enMovimiento = true;
        }
    }

    void Update()
    {
        if (!enMovimiento || objetivoActual == null) return;

        transform.position = Vector3.MoveTowards(transform.position, objetivoActual.position, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, objetivoActual.position) < 0.1f)
        {
            if (ruta.Count > 0)
            {
                objetivoActual = ruta.Dequeue();
            }
            else
            {
                enMovimiento = false;
                transform.SetParent(objetivoActual); // opcional: se "adhiere" al asiento
            }
        }
    }
}