using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CageTrap : MonoBehaviour
{
    [Header("Cage Settings")]
    [Tooltip("Drag the Cage GameObject with the Rigidbody here.")]
    public Rigidbody cageRigidbody;

    [Tooltip("How high in the sky the cage starts above the ground.")]
    public float skyHeight = 25f;

    [Tooltip("The Y height where the cage stops and locks in place.")]
    public float groundYPosition = 0f;

    [Tooltip("How many seconds the cage takes to fall from the sky to the ground.")]
    public float slowFallDuration = 4.5f;

    [Header("Cutscene Camera Settings")]
    [Tooltip("Drag your normal Player Camera here.")]
    public Camera playerCamera;

    [Tooltip("Drag your separate Cutscene Camera here.")]
    public Camera cutsceneCamera;

    [Tooltip("How many seconds the camera takes to slowly look up at the sky.")]
    public float lookUpDuration = 3.0f;

    [Tooltip("Angle to tilt the camera up (-75 looks almost straight up).")]
    public float lookUpAngle = -75f;

    [Header("Player & Outcome")]
    public string targetTag = "Player";

    [Tooltip("Drag your Player's movement/look scripts here so they freeze during the cutscene.")]
    public MonoBehaviour[] scriptsToDisable;

    [Tooltip("Check this to restart the scene after being trapped. Uncheck to stay trapped inside.")]
    public bool restartSceneAfterTrap = false;

    [Tooltip("Seconds to hold the view after the cage lands before finishing the cutscene.")]
    public float holdCutsceneTime = 2.5f;

    private bool hasTriggered = false;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(false);
        }

        if (cageRigidbody != null)
        {
            cageRigidbody.isKinematic = true;
            cageRigidbody.constraints = RigidbodyConstraints.FreezeAll;

            // Place the cage high in the sky directly above its landing spot
            Vector3 skyPos = cageRigidbody.transform.position;
            skyPos.y = groundYPosition + skyHeight;
            cageRigidbody.transform.position = skyPos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag(targetTag))
        {
            StartCoroutine(CageCutsceneRoutine(other.gameObject));
        }
    }

    private IEnumerator CageCutsceneRoutine(GameObject player)
    {
        hasTriggered = true;

        // 1. Freeze player movement and mouse-look scripts
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }

        // Centre the player neatly beneath the cage
        if (cageRigidbody != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = new Vector3(
                cageRigidbody.transform.position.x,
                player.transform.position.y,
                cageRigidbody.transform.position.z
            );

            if (cc != null) cc.enabled = true;
        }

        // 2. Switch from Player Camera to Cutscene Camera
        Quaternion startCamRot = Quaternion.identity;
        if (cutsceneCamera != null)
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            cutsceneCamera.gameObject.SetActive(true);
            startCamRot = cutsceneCamera.transform.rotation;
        }

        // 3. Slowly tilt the cutscene camera upward toward the sky
        if (cutsceneCamera != null)
        {
            Vector3 currentEuler = startCamRot.eulerAngles;
            Quaternion lookUpRot = Quaternion.Euler(lookUpAngle, currentEuler.y, 0f);

            float elapsed = 0f;
            while (elapsed < lookUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / lookUpDuration);
                cutsceneCamera.transform.rotation = Quaternion.Slerp(startCamRot, lookUpRot, t);
                yield return null;
            }
            cutsceneCamera.transform.rotation = lookUpRot;
        }

        // Dramatic pause while staring up at the sky
        yield return new WaitForSeconds(0.5f);

        // 4. Slowly lower the cage from the sky down to the ground using Transform
        if (cageRigidbody != null)
        {
            Vector3 startPos = cageRigidbody.transform.position;
            Vector3 endPos = new Vector3(startPos.x, groundYPosition, startPos.z);

            float elapsed = 0f;
            while (elapsed < slowFallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slowFallDuration);
                cageRigidbody.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            cageRigidbody.transform.position = endPos;
        }

        // 5. Slowly tilt the cutscene camera back down to eye level
        if (cutsceneCamera != null)
        {
            Quaternion currentRot = cutsceneCamera.transform.rotation;
            Quaternion eyeLevelRot = Quaternion.Euler(0f, startCamRot.eulerAngles.y, 0f);

            float elapsed = 0f;
            while (elapsed < lookUpDuration * 0.75f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / (lookUpDuration * 0.75f));
                cutsceneCamera.transform.rotation = Quaternion.Slerp(currentRot, eyeLevelRot, t);
                yield return null;
            }
            cutsceneCamera.transform.rotation = eyeLevelRot;
        }

        yield return new WaitForSeconds(holdCutsceneTime);

        // 6. Restart scene OR switch back to Player Camera while trapped inside
        if (restartSceneAfterTrap)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            if (cutsceneCamera != null) cutsceneCamera.gameObject.SetActive(false);
            if (playerCamera != null) playerCamera.gameObject.SetActive(true);

            foreach (MonoBehaviour script in scriptsToDisable)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
        }
    }
}
