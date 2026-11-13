using UnityEngine;
using TMPro; // Required for TextMeshPro

public class StartMessage : MonoBehaviour
{
    // Drag your TextMeshPro UI object into this slot in the Unity Inspector
    public TextMeshProUGUI instructionText;

    void Start()
    {
        // Set the text message right when the game starts
        instructionText.text = "SPAM SPACEBAR TO BREAKOUT!";
    }

    void Update()
    {
        // Hide the text the moment they press the spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            instructionText.gameObject.SetActive(false);

            // Optional: Trigger your actual game start logic here
            Debug.Log("start");
        }
    }
}
