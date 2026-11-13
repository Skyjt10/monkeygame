using UnityEngine;

public class BoxBreakout : MonoBehaviour
{
    [Header("Cage Breaking Settings")]
    public float explosionForce = 300f;
    public float explosionRadius = 2f;

    public void Wiggle()
    {
        // Jiggles the entire cage slightly every time you mash Space
        transform.position += Random.insideUnitSphere * 0.02f;
    }

    public void BreakOpen()
    {
        Debug.Log("The cage explodes boi");

        // 1. Find every single child part inside this cage
        Rigidbody[] childParts = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody part in childParts)
        {
            // 2. Disconnect the parts from the cage so they can fly free
            part.transform.SetParent(null);

            // 3. Make them physical objects so they fall and roll
            part.isKinematic = false;
            part.useGravity = true;

            // 4. Blast them outward from the center of the cage
            part.AddExplosionForce(explosionForce, transform.position, explosionRadius);

            // 5. Clean up the broken debris after 5 seconds so they don't clutter your game
            Destroy(part.gameObject, 5f);
        }

        // 6. Delete the empty parent cage object left behind
        Destroy(gameObject);
    }
}
