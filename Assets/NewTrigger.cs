using UnityEngine;

public class NewTrigger : MonoBehaviour
{


    public GameObject banana;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(banana);
        }
    }
}
