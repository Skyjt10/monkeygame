using System.Collections;
using UnityEngine;

public class PooShooter : MonoBehaviour
{
    public float throwForce = 15f;
    public float upwardArc = 4f;
    public float splatLifetime = 15f;
    public float dripSpeed = 0.08f;

    private bool isThrowing = false;

    void Update()
    {
        if (!isThrowing && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(SmoothThrowMotion());
        }
    }

    IEnumerator SmoothThrowMotion()
    {
        isThrowing = true;
        Quaternion baseRot = transform.localRotation;

        // 1. Smoothly wind up backward
        yield return RotateOverTime(baseRot, baseRot * Quaternion.Euler(-25f, 0f, 0f), 0.15f);

        // 2. Whip forward and release halfway through
        StartCoroutine(DelayedSpawn(0.06f));
        yield return RotateOverTime(baseRot * Quaternion.Euler(-25f, 0f, 0f), baseRot * Quaternion.Euler(30f, 0f, 0f), 0.12f);

        // 3. Smoothly settle back to standing posture
        yield return RotateOverTime(baseRot * Quaternion.Euler(30f, 0f, 0f), baseRot, 0.25f);

        transform.localRotation = baseRot;
        isThrowing = false;
    }

    IEnumerator RotateOverTime(Quaternion startRot, Quaternion endRot, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        transform.localRotation = endRot;
    }

    IEnumerator DelayedSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnPoo();
    }

    void SpawnPoo()
    {
        Vector3 pos = transform.position + (transform.forward * 0.9f) + (Vector3.up * 0.5f);

        GameObject poo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        poo.name = "Poo";
        poo.transform.position = pos;
        poo.transform.localScale = Vector3.one * 0.35f;

        // Remove the sphere's solid collider so Unity's physics doesn't bounce it away before our impact check runs
        Destroy(poo.GetComponent<Collider>());

        Renderer r = poo.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = new Color(0.36f, 0.20f, 0.09f);
        }

        Rigidbody rb = poo.AddComponent<Rigidbody>();
        rb.AddForce((transform.forward * throwForce) + (Vector3.up * upwardArc), ForceMode.Impulse);

        StartCoroutine(TrackFlightAndSplat(poo));
    }

    IEnumerator TrackFlightAndSplat(GameObject poo)
    {
        float timer = 0f;
        Vector3 lastPos = poo.transform.position;
        float sphereRadius = 0.175f;

        while (poo != null && timer < 10f)
        {
            timer += Time.deltaTime;
            Vector3 currentPos = poo.transform.position;
            Vector3 moveDir = currentPos - lastPos;
            float dist = moveDir.magnitude;

            // SphereCast checks the exact width of the projectile and ignores invisible trigger zones
            if (dist > 0.0001f && Physics.SphereCast(lastPos, sphereRadius, moveDir.normalized, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                // Ignore the player who threw it
                if (!hit.transform.IsChildOf(transform.root) && hit.transform != transform.root)
                {
                    CreateCartoonSplat(hit.point, hit.normal);
                    Destroy(poo);
                    yield break;
                }
            }

            lastPos = currentPos;
            yield return null;
        }

        if (poo != null)
        {
            Destroy(poo);
        }
    }

    void CreateCartoonSplat(Vector3 hitPoint, Vector3 surfaceNormal)
    {
        GameObject splatRoot = new GameObject("CartoonSplat");
        splatRoot.transform.position = hitPoint + (surfaceNormal * 0.015f);
        splatRoot.transform.rotation = Quaternion.LookRotation(surfaceNormal);

        Color brownColor = new Color(0.36f, 0.20f, 0.09f);

        // Main flat centre blob
        BuildSplatBlob(splatRoot.transform, Vector3.zero, new Vector3(0.55f, 0.55f, 0.04f), brownColor);

        // 6 randomised smaller blobs around the edges
        for (int i = 0; i < 6; i++)
        {
            float angle = (i * 60f) + Random.Range(-20f, 20f);
            float distance = Random.Range(0.22f, 0.38f);
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * distance;
            float blobSize = Random.Range(0.14f, 0.26f);

            BuildSplatBlob(splatRoot.transform, offset, new Vector3(blobSize, blobSize, 0.04f), brownColor);
        }

        // Start dripping if it hit a wall (anything mostly vertical)
        bool isWall = Mathf.Abs(surfaceNormal.y) < 0.5f;
        StartCoroutine(DripAndFadeRoutine(splatRoot, isWall));
    }

    void BuildSplatBlob(Transform parent, Vector3 localOffset, Vector3 scale, Color color)
    {
        GameObject blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(blob.GetComponent<Collider>());

        blob.transform.SetParent(parent);
        blob.transform.localPosition = localOffset;
        blob.transform.localRotation = Quaternion.identity;
        blob.transform.localScale = scale;

        Renderer r = blob.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = color;
        }
    }

    IEnumerator DripAndFadeRoutine(GameObject splatRoot, bool isWall)
    {
        float elapsed = 0f;
        Vector3 startScale = splatRoot.transform.localScale;

        while (splatRoot != null && elapsed < splatLifetime)
        {
            elapsed += Time.deltaTime;

            if (isWall)
            {
                // Slowly slide downward along the wall
                splatRoot.transform.position += Vector3.down * (dripSpeed * Time.deltaTime);

                // Slightly stretch vertically as it drips
                float stretchY = 1f + (elapsed / splatLifetime) * 0.45f;
                splatRoot.transform.localScale = new Vector3(startScale.x, startScale.y * stretchY, startScale.z);
            }

            yield return null;
        }

        if (splatRoot != null)
        {
            Destroy(splatRoot);
        }
    }
}
