using UnityEngine;

public class QTEManager : MonoBehaviour
{
    [Header("QTE Settings")]
    public KeyCode mashkey = KeyCode.Space;
    public int requiredpresses = 15;
    public float timelimit = 5.0f;

    [Header("Camera Swapping")]
    public Camera qtecinematiccamera;
    public GameObject maintrackingcamera;

    [Header("References")]
    public BoxBreakout boxscript;
    public PlayerController playerscript;

    private int currentpresses = 0;
    private float qtetimer = 0f; // Renamed from timer to qtetimer to stop the conflict!
    private bool isqteactive = false;

    private void Awake()
    {
        triggerboxtrap();
    }

    public void triggerboxtrap()
    {
        currentpresses = 0;
        qtetimer = timelimit;
        isqteactive = true;

        if (qtecinematiccamera != null) qtecinematiccamera.gameObject.SetActive(true);
        if (maintrackingcamera != null) maintrackingcamera.SetActive(false);

        if (playerscript != null)
        {
            playerscript.LockPlayerForQTE();
        }

        Debug.Log("click space now");
    }

    private void Update()
    {
        if (!isqteactive) return;

        qtetimer -= Time.deltaTime;
        if (qtetimer <= 0)
        {
            qtefailed();
            return;
        }

        if (Input.GetKeyDown(mashkey))
        {
            currentpresses++;

            if (boxscript != null)
            {
                boxscript.Wiggle();
            }

            if (currentpresses >= requiredpresses)
            {
                qtesuccess();
            }
        }
    }

    private void qtesuccess()
    {
        isqteactive = false;
        Debug.Log("SUCCESS!");

        if (maintrackingcamera != null) maintrackingcamera.SetActive(true);
        if (qtecinematiccamera != null) qtecinematiccamera.gameObject.SetActive(false);

        if (boxscript != null)
        {
            boxscript.BreakOpen();
        }

        if (playerscript != null)
        {
            playerscript.PlayBreakoutAnimation();
        }
    }

    private void qtefailed()
    {
        isqteactive = false;
        Debug.Log("FAILED!");

        if (maintrackingcamera != null) maintrackingcamera.SetActive(true);
        if (qtecinematiccamera != null) qtecinematiccamera.gameObject.SetActive(false);

        if (playerscript != null)
        {
            playerscript.PlayGameOverOrTrapped();
        }
    }
}
