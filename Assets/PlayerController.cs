using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float proneSpeed = 1.25f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public int maxAirJumps = 1;

    [Header("Stance Heights")]
    public float standHeight = 2.0f;
    public float crouchHeight = 1.0f;
    public float proneHeight = 0.5f;
    public float stanceTransitionSpeed = 8f;

    [Header("Dash")]
    public float dashDistance = 6f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;

    [Header("Field of View")]
    public float sprintFovIncrease = 13f;
    public float dashFovIncrease = 20f;
    public float fovChangeSpeed = 8f;

    [Header("References")]
    public Transform cameraTransform;

    private enum Stance { Standing, Crouching, Prone }
    private Stance currentStance = Stance.Standing;
    private float targetHeight;

    private CharacterController controller;
    private Camera fovCamera;
    private float startingFov;
    private Vector3 velocity;
    private Vector3 dashDirection;
    private int jumpCount;
    private float dashTimer;
    private float dashCooldownTimer;

    // --- NEW: QTE & ANIMATION VARIABLES ---
    private Animator anim;
    private bool isTrappedInQTE = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>(); // Cache the animator component
        ResolveFovCamera();

        if (controller == null)
        {
            Debug.LogError("CharacterController not found on " + name, this);
        }

        targetHeight = standHeight;
    }

    private void Update()
    {
        if (controller == null)
            return;

        if (fovCamera == null)
            ResolveFovCamera();

        // --- NEW: Freeze movement if trapped in the box ---
        if (isTrappedInQTE)
        {
            // Apply gravity so the player doesn't float if the box is in mid-air
            if (!controller.isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
                controller.Move(Vector3.up * velocity.y * Time.deltaTime);
            }

            // Reset FOV smoothly back to normal while stuck
            if (fovCamera != null)
            {
                fovCamera.fieldOfView = Mathf.Lerp(fovCamera.fieldOfView, startingFov, fovChangeSpeed * Time.deltaTime);
            }
            return;
        }

        Keyboard keyboard = Keyboard.current;

        // --- Handle Crouch / Lay Down Input ---
        if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
        {
            HandleStanceInput();
        }

        // Smoothly adjust character height & center offset
        HandleHeightTransition();

        bool wasGrounded = controller.isGrounded;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = (cameraTransform != null) ? cameraTransform.forward : transform.forward;
        Vector3 right = (cameraTransform != null) ? cameraTransform.right : transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

        bool sprintPressed = keyboard != null &&
            (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);

        // Disable sprinting if crouching or laying down
        bool isSprinting = sprintPressed && moveDirection.sqrMagnitude > 0f && currentStance == Stance.Standing;

        // Determine baseline speed depending on stance
        float currentSpeed = moveSpeed;
        if (currentStance == Stance.Standing)
        {
            currentSpeed = sprintPressed ? sprintSpeed : moveSpeed;
        }
        else if (currentStance == Stance.Crouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (currentStance == Stance.Prone)
        {
            currentSpeed = proneSpeed;
        }

        if (wasGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            jumpCount = 0;
        }

        // Disable jumping unless the player is fully standing up
        if (Input.GetButtonDown("Jump") && currentStance == Stance.Standing)
        {
            if (wasGrounded)
            {
                PerformJump();
                jumpCount = 1;
            }
            else if (jumpCount < maxAirJumps + 1)
            {
                PerformJump();
                jumpCount++;
            }
        }

        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);
        if (keyboard != null && keyboard.qKey.wasPressedThisFrame && dashCooldownTimer <= 0f && dashDuration > 0f)
        {
            dashDirection = moveDirection.sqrMagnitude > 0f ? moveDirection : forward;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

        velocity.y += gravity * Time.deltaTime;
        Vector3 movement = moveDirection * currentSpeed + Vector3.up * velocity.y;
        float dashStep = Mathf.Min(Time.deltaTime, dashTimer);
        bool isDashing = dashTimer > 0f;
        if (dashStep > 0f)
        {
            movement += dashDirection * (dashDistance / dashDuration) * (dashStep / Time.deltaTime);
            dashTimer -= dashStep;
        }

        controller.Move(movement * Time.deltaTime);

        if (fovCamera != null)
        {
            float fovIncrease = isDashing
                ? dashFovIncrease
                : isSprinting ? sprintFovIncrease : 0f;
            float targetFov = startingFov + fovIncrease;
            fovCamera.fieldOfView = Mathf.Lerp(
                fovCamera.fieldOfView,
                targetFov,
                fovChangeSpeed * Time.deltaTime);
        }
    }

    private void HandleStanceInput()
    {
        switch (currentStance)
        {
            case Stance.Standing:
                currentStance = Stance.Crouching;
                targetHeight = crouchHeight;
                break;

            case Stance.Crouching:
                currentStance = Stance.Prone;
                targetHeight = proneHeight;
                break;

            case Stance.Prone:
                if (HasCeilingClearance(standHeight))
                {
                    currentStance = Stance.Standing;
                    targetHeight = standHeight;
                }
                else if (HasCeilingClearance(crouchHeight))
                {
                    currentStance = Stance.Crouching;
                    targetHeight = crouchHeight;
                }
                break;
        }
    }

    private void HandleHeightTransition()
    {
        if (!Mathf.Approximately(controller.height, targetHeight))
        {
            float lastHeight = controller.height;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * stanceTransitionSpeed);

            // Keeps player grounded during shrink/growth transitions by altering the center offset
            Vector3 center = controller.center;
            center.y += (controller.height - lastHeight) / 2.0f;
            controller.center = center;
        }
    }

    private bool HasCeilingClearance(float checkHeight)
    {
        float radius = controller.radius;

        // Offset the bottom point slightly upwards so it doesn't clip into the ground floor
        Vector3 bottomPoint = transform.position + Vector3.up * (radius + 0.1f);
        Vector3 topPoint = transform.position + Vector3.up * (checkHeight - radius);

        // Create a layer mask that ignores the Player's own layer so you don't hit yourself
        int layerMask = ~(1 << gameObject.layer);

        // Perform capsule check to see if anything is genuinely blocking your head
        return !Physics.CheckCapsule(bottomPoint, topPoint, radius, layerMask, QueryTriggerInteraction.Ignore);
    }


    private void PerformJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void ResolveFovCamera()
    {
        fovCamera = cameraTransform != null
            ? cameraTransform.GetComponent<Camera>()
            : null;

        if (fovCamera == null && cameraTransform != null)
            fovCamera = cameraTransform.GetComponentInChildren<Camera>();

        if (fovCamera == null)
            fovCamera = Camera.main;

        if (fovCamera != null)
        {

            startingFov = fovCamera.fieldOfView;
        }
    }

    // =================================================================
    // --- NEW: QTE BREAKOUT METHODS ---
    // =================================================================

    /// <summary>
    /// Call this when the player gets trapped in the box to disable movement.
    /// </summary>
    public void LockPlayerForQTE()
    {
        isTrappedInQTE = true;
        dashTimer = 0f; // Cancel any active dash

        // Force player into standing height boundaries so they match the box scale
        currentStance = Stance.Standing;
        targetHeight = standHeight;
        if (controller != null)
        {
            controller.height = standHeight;
        }
    }

    /// <summary>
    /// Called by QTEManager when the player successfully mashes out.
    /// </summary>
    public void PlayBreakoutAnimation()
    {
        isTrappedInQTE = false; // Restore movement capabilities
        Debug.Log("monkeys explodes the cage");

        if (anim != null)
        {
            anim.SetTrigger("Breakout"); // Requires a "Breakout" Trigger parameter in your Animator
        }
    }

    /// <summary>
    /// Called by QTEManager if the timer runs out.
    /// </summary>
    public void PlayGameOverOrTrapped()
    {
        Debug.Log("Character failed to escape and is trapped!");

        if (anim == null)
        {

        }

    }
}