using System.Collections;
using UnityEngine;

public class TreeCutscene : MonoBehaviour
{
    [Header("Tree Growth Settings")]
    public GameObject treeObject;          // Drag your Tree model here
    public Transform treeTargetPosition;   // Drag your empty Target position here
    public float growthDuration = 5.0f;    // Time in seconds for the tree to grow

    [Header("The 3 Manual Cameras")]
    public Camera introCamera;             // Drag your Opening Cutscene Camera here
    public Camera playerCamera;            // Drag your Normal Gameplay Follow Camera here
    public Camera treeCamera;              // Drag your Tree-focus Camera here

    [Header("Tree Cam Follow Offset")]
    public Vector3 treeCameraOffset = new Vector3(0f, 5f, -10f); // Where tree camera sits relative to tree

    private bool isTriggered = false;

    private void Start()
    {
        // SAFETY CHECK AT GAME START:
        // Ensure ONLY the intro camera is active when the game boots up!
        if (introCamera != null) introCamera.gameObject.SetActive(true);
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        if (treeCamera != null) treeCamera.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger if the object is the player and we haven't run this cutscene yet
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;

            // Find your movement script to freeze the player
            // Change "PlayerMovement" to match the exact name of your movement script component
            if (other.TryGetComponent(out MonoBehaviour playerMovement))
            {
                StartCoroutine(RunTreeCutscene(playerMovement, other.transform));
            }
            else
            {
                StartCoroutine(RunTreeCutscene(null, other.transform));
            }
        }
    }

    private IEnumerator RunTreeCutscene(MonoBehaviour playerMovement, Transform playerTransform)
    {
        // 1. FREEZE THE PLAYER
        if (playerMovement != null) playerMovement.enabled = false;

        if (playerTransform.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero; // Stops player from sliding
        }

        // 2. SWITCH TO TREE CAMERA
        playerCamera.gameObject.SetActive(false); // Turn off gameplay camera
        treeCamera.gameObject.SetActive(true);    // Turn on tree camera

        Vector3 startTreePos = treeObject.transform.position;
        Vector3 endTreePos = treeTargetPosition.position;
        float elapsedTime = 0f;

        Transform treeCamTransform = treeCamera.transform;

        // 3. TREE GROWTH ANIMATION LOOP
        while (elapsedTime < growthDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / growthDuration);

            // Move the physical tree upward
            treeObject.transform.position = Vector3.Lerp(startTreePos, endTreePos, t);

            // Move the camera up with the tree and look at it
            treeCamTransform.position = treeObject.transform.position + treeCameraOffset;
            treeCamTransform.LookAt(treeObject.transform.position);

            yield return null; // Wait for next frame
        }

        // Snap tree exactly to final target
        treeObject.transform.position = endTreePos;

        // 4. SWITCH BACK TO PLAYER CAMERA
        treeCamera.gameObject.SetActive(false);   // Turn off tree camera
        playerCamera.gameObject.SetActive(true);  // Turn on gameplay follow camera

        // 5. UNFREEZE PLAYER
        if (playerMovement != null) playerMovement.enabled = true;

        Debug.Log("Tree cutscene finished! Control returned to player.");
    }
}
