using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameCamera : MonoBehaviour
{
    public event System.Action<Camera> gameCameraUpdateComplete;
    public enum ViewMode { TopDown, LookingForward, LookingBehind, MainMenu }
    public ViewMode activeView = ViewMode.LookingForward;

    public float fovSlow = 60;
    public float fovFast = 65;
    public float fovBoost = 75;
    public float fovSmoothTime = 0.2f;

    [Header("Top-Down Settings")]
    public float distanceAbove = 15f;
    public float turnSpeed = 40f;
    public float startAngle;

    float angle;

    [Header("Orbit / Third-Person Settings")]
    public float followDistance = 8f;
    public float followHeight = 2.5f;
    public float orbitSensitivity = 0.15f;
    public float minPitchAngle = -15f;
    public float maxPitchAngle = 60f;
    public float positionSmoothTime = 0.15f;
    public float rotationSmoothTime = 0.15f;

    [Header("Alternate View Settings")]
    public ViewSettings lookingAheadView;
    public ViewSettings lookingBehindView;
    public ViewSettings menuView;

    [Header("References")]
    public Camera cam;
    public Player player;

    Transform target;
    float smoothFovVelocity;
    float menuToGameViewTransitionT;

    // Orbit coordinates
    private float yaw = 0f;
    private float pitch = 15f;

    private Vector3 currentVelocity;
    private Quaternion currentRotationVelocity;

    void Start()
    {
        InitView();
    }

    public void SetActiveView(ViewMode viewMode)
    {
        activeView = viewMode;
    }

    public void InitView()
    {
        target = player.transform;
        currentCamForward = target.forward;
        UpdateView();
        cam.fieldOfView = CalculateFOV();
    }

    void LateUpdate()
    {
        if (!GameController.IsState(GameState.Paused))
        {
            UpdateView();
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, CalculateFOV(), ref smoothFovVelocity, fovSmoothTime);
            gameCameraUpdateComplete?.Invoke(cam);
        }
    }

    void UpdateView()
    {
        if (GameController.CurrentState == GameState.InMainMenu)
        {
            menuToGameViewTransitionT = 0;
            activeView = ViewMode.MainMenu;
        }

        // Handle mouse orbit input if left/right click is dragged or automatically during movement
        if (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            yaw += mouseDelta.x * orbitSensitivity;
            pitch -= mouseDelta.y * orbitSensitivity;
            pitch = Mathf.Clamp(pitch, minPitchAngle, maxPitchAngle);
        }

        switch (activeView)
        {
            case ViewMode.TopDown:
                UpdateTopDownView();
                break;
            case ViewMode.LookingForward:
                UpdateOrbitView();
                break;
            case ViewMode.LookingBehind:
                UpdateOrbitView(behind: true);
                break;
            case ViewMode.MainMenu:
                UpdateOrbitView();
                break;
        }
    }

    float CalculateFOV()
    {
        return (player.IsBoosting) ? fovBoost : Mathf.Lerp(fovSlow, fovFast, player.SpeedT);
    }

    void UpdateTopDownView()
    {
        Vector3 gravityUp = player.GravityUp;
        Vector3 targetPos = target.position + gravityUp * distanceAbove;
        
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, positionSmoothTime);
        Quaternion targetRot = Quaternion.LookRotation(-gravityUp, target.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    }

    private Vector3 currentCamForward = Vector3.forward;

    void UpdateOrbitView(bool behind = false)
    {
        Vector3 gravityUp = player.GravityUp;

        // Project current camera forward onto the tangent plane
        currentCamForward = Vector3.ProjectOnPlane(currentCamForward, gravityUp).normalized;
        if (currentCamForward.sqrMagnitude < 0.001f) currentCamForward = target.forward;

        // The base rotation aligned with gravity and our persistent forward
        Quaternion baseRot = Quaternion.LookRotation(currentCamForward, gravityUp);

        // Apply yaw from mouse input locally
        baseRot = baseRot * Quaternion.Euler(0, yaw, 0);
        // Reset yaw so we just accumulate it into currentCamForward
        currentCamForward = baseRot * Vector3.forward;
        yaw = 0;

        float targetPitch = pitch;
        // Pitch is applied relative to the new base
        Quaternion targetRotation = baseRot * Quaternion.Euler(targetPitch, (behind ? 180f : 0f), 0f);

        Vector3 targetPos = target.position + targetRotation * new Vector3(0, followHeight, -followDistance);

        // Smooth position and rotation (tighter smooth time for better responsiveness)
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, 0.05f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
    }

    public bool topDownMode
    {
        get
        {
            return activeView == ViewMode.TopDown;
        }
    }

    [System.Serializable]
    public struct ViewSettings
    {
        public Vector3 offset;
        public Vector3 lookTargetOffset;

        public ViewSettings(Vector3 offset, Vector3 lookTargetOffset)
        {
            this.offset = offset;
            this.lookTargetOffset = lookTargetOffset;
        }

        public static ViewSettings Lerp(ViewSettings a, ViewSettings b, float t)
        {
            Vector3 offset = Vector3.Lerp(a.offset, b.offset, t);
            Vector3 lookTargetOffset = Vector3.Lerp(a.lookTargetOffset, b.lookTargetOffset, t);
            return new ViewSettings(offset, lookTargetOffset);
        }
    }
}