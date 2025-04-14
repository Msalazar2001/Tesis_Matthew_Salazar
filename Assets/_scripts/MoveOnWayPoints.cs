using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveOnWayPoints : MonoBehaviour
{
    public List<GameObject> waypoints;
    public float speed = 2;
    int index = 0;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        Vector3 destination = waypoints[index].transform.position;
        Vector3 newPos=Vector3.MoveTowards(transform.position, waypoints[index].transform.position, speed * Time.deltaTime);
        transform.position = newPos;

        float distance= Vector3.Distance(transform.position, destination);

        if (distance <= 0.5)
        {
            index++;
        }
    }
}
