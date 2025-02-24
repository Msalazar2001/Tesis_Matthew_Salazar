using BNG;
using UnityEngine;

public class Posicion : MonoBehaviour
{

    [SerializeField]
    BNGPlayerController playerController;

    [SerializeField]
    Transform seatTransform;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController.AttachPlayerToSeat(seatTransform);
        // Invoke("test", 1);
    }

    void test()
    {
        playerController.AttachPlayerToSeat(seatTransform);
    }
    // Update is called once per frame
    void Update()
    {

    }
}