using UnityEngine;

public class StationZone : MonoBehaviour
{
    public string stationName = "Station";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Debug.Log($"Entered {stationName}");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Debug.Log($"Exited {stationName}");
    }
}
