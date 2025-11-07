using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Camera controller matching SDK's Viewport3d.ts behavior.
/// Controls a Cinemachine 3.x camera through transform hierarchy.
/// SDK Reference: Viewport3d.ts
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player to follow")]
    public Player player;

    [Tooltip("The Cinemachine camera we're controlling")]
    public CinemachineCamera cinemachineCamera;

    [Header("Camera Objects")]
    [Tooltip("Child object for yaw rotation")]
    public Transform yawObject;

    [Tooltip("Child object for pitch rotation")]
    public Transform pitchObject;

    [Header("Camera Settings")]
    [Tooltip("Show UI adjustment (for future UI implementation)")]
    public bool adjustForUI = false;

    [Header("SDK Constants")]
    // SDK Reference: Viewport3d.ts lines 21-28
    private const float ROTATE_MULT = 0.006f;  // SDK value
    private const float ZOOM_MULT = 0.005f;    // SDK value
    private const float TOUCH_MULT = 2f;
    private const float MIN_PITCH = -Mathf.PI / 2f;  // -90 degrees
    private const float MAX_PITCH = 0.1f;             // ~5.7 degrees
    private const float MIN_ZOOM = 2f;
    private const float MAX_ZOOM = 20f;

    // SDK follow lerp constant
    private const float FOLLOW_LERP = 0.1f;

    [Header("Movement")]
    [Tooltip("Offset from player position")]
    private Vector3 followOffset = new Vector3(0.5f, 0f, -0.5f);

    // Keyboard rotation speeds (radians per second from SDK)
    private float yawDelta = 0f;
    private float pitchDelta = 0f;
    private const float YAW_SPEED = 2f;    // 2 radians/sec from SDK
    private const float PITCH_SPEED = 1f;  // 1 radian/sec from SDK

    // Current zoom distance
    private float currentZoom = 12f;

    // For mouse rotation
    private bool isRotating = false;

    [Header("Raycasting")]
    [Tooltip("Layer mask for ground/clickable objects")]
    public LayerMask clickableLayers = -1;

    [Header("Debug")]
    public bool showDebugInfo = false;
    private Vector2Int? lastClickedTile = null;

    void Start()
    {
        // Auto-find references if not set
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
            if (player == null)
            {
                Debug.LogError("CameraController: No Player found!");
            }
        }

        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
        }

        if (yawObject == null || pitchObject == null)
        {
            Debug.LogError("CameraController: Yaw or Pitch objects not assigned!");
        }

        // Set initial camera facing
        // FIXED: Use SetCameraDirection for proper state sync
        // Start facing north (true = south based on our changes)
        SetCameraDirection(false); // false = face north

        // Set initial zoom
        SetZoom(currentZoom);

        // Face camera toward player (backward from its local forward)
        cinemachineCamera.transform.localRotation = Quaternion.Euler(0, 180, 0);

        // IMPORTANT: Ensure mouse rotation state is clean
        isRotating = false;
    }

    void Update()
    {
        if (player == null || GridManager.Instance == null)
            return;

        HandleMouseInput();
        HandleKeyboardInput();
        HandleScrollWheel();
        //HandleRaycasting();
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        FollowPlayer();
        UpdateRotation(Time.deltaTime);
    }

    /// <summary>
    /// Initialize camera rotation (face north or south).
    /// SDK Reference: Viewport3d.ts line 51-54
    /// </summary>
    public void InitializeCameraRotation(bool faceSouth)  // CHANGED: Parameter name for clarity
    {
        if (!faceSouth)  // North
        {
            yawObject.rotation = Quaternion.identity; // 0 degrees = north
        }
        else  // South
        {
            yawObject.rotation = Quaternion.Euler(0, 180, 0); // 180 degrees = south
        }

        // Set initial pitch (looking down slightly)
        // SDK Reference: Viewport3d.ts line 56
        pitchObject.rotation = Quaternion.Euler(Mathf.Rad2Deg * -0.7f, 0, 0);

        // Reset rotation deltas for keyboard input
        yawDelta = 0f;
        pitchDelta = 0f;
    }

    /// <summary>
    /// Set camera to face a specific direction immediately.
    /// Properly syncs all rotation states to prevent snapping.
    /// </summary>
    public void SetCameraDirection(bool faceSouth)
    {
        // Set the yaw object rotation
        if (!faceSouth)  // North
        {
            yawObject.rotation = Quaternion.identity; // 0 degrees = north
        }
        else  // South
        {
            yawObject.rotation = Quaternion.Euler(0, 180, 0); // 180 degrees = south
        }

        // Keep current pitch
        // (Don't change pitch when clicking compass)

        // Reset keyboard rotation deltas
        yawDelta = 0f;
        pitchDelta = 0f;

        // IMPORTANT: Stop any active rotation
        // This prevents the mouse drag from continuing with old values
        isRotating = false;
    }

    /// <summary>
    /// Handle mouse rotation (middle mouse button drag).
    /// SDK Reference: Viewport3d.ts onDocumentMouseMove()
    /// Uses raw pixel movement, independent of window size
    /// </summary>
    private void HandleMouseInput()
    {
        // Middle mouse button = button 2
        if (Input.GetMouseButtonDown(2))
        {
            isRotating = true;
            // DON'T lock cursor or hide it - SDK keeps it visible
        }

        if (Input.GetMouseButtonUp(2))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            // Use mousePositionDelta for raw pixel movement (window-size independent)
            Vector2 mouseDelta = Input.mousePositionDelta;

            // Apply rotation with SDK multiplier (SDK works in RADIANS)
            // But Unity's Rotate() expects DEGREES, so convert!
            float yawChange = mouseDelta.x * ROTATE_MULT * Mathf.Rad2Deg;
            yawObject.Rotate(0, yawChange, 0, Space.Self);

            float pitchChange = mouseDelta.y * ROTATE_MULT * Mathf.Rad2Deg;
            float currentPitch = pitchObject.localRotation.eulerAngles.x;
            if (currentPitch > 180) currentPitch -= 360;

            float newPitch = currentPitch + pitchChange;
            newPitch = Mathf.Clamp(newPitch, MIN_PITCH * Mathf.Rad2Deg, MAX_PITCH * Mathf.Rad2Deg);

            pitchObject.localRotation = Quaternion.Euler(newPitch, 0, 0);
        }
    }

    /// <summary>
    /// Handle keyboard rotation.
    /// SDK Reference: Viewport3d.ts onKeyDown/onKeyUp
    /// </summary>
    private void HandleKeyboardInput()
    {
        // Reset deltas
        yawDelta = 0f;
        pitchDelta = 0f;

        // Check for arrow keys (always enabled)
        if (Input.GetKey(KeyCode.LeftArrow))
            yawDelta = YAW_SPEED;
        if (Input.GetKey(KeyCode.RightArrow))
            yawDelta = -YAW_SPEED;
        if (Input.GetKey(KeyCode.UpArrow))
            pitchDelta = -PITCH_SPEED;
        if (Input.GetKey(KeyCode.DownArrow))
            pitchDelta = PITCH_SPEED;

        // Check for WASD (if enabled)
        if (UISettings.Instance != null && UISettings.Instance.wasdCamera)
        {
            if (Input.GetKey(KeyCode.A))
                yawDelta = YAW_SPEED;
            if (Input.GetKey(KeyCode.D))
                yawDelta = -YAW_SPEED;
            if (Input.GetKey(KeyCode.W))
                pitchDelta = -PITCH_SPEED;
            if (Input.GetKey(KeyCode.S))
                pitchDelta = PITCH_SPEED;
        }
    }

    /// <summary>
    /// Handle scroll wheel zoom.
    /// SDK Reference: Viewport3d.ts onDocumentMouseWheel
    /// Browser deltaY is ~100-120 per notch, Unity is ~0.1
    /// </summary>
    private void HandleScrollWheel()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            // SDK uses e.deltaY * ZOOM_MULT where deltaY is ~100 per notch
            // Unity scroll is ~0.1 per notch, so multiply by 1000 to match
            float zoomChange = -scroll * ZOOM_MULT * 1000f;
            currentZoom = Mathf.Clamp(currentZoom + zoomChange, MIN_ZOOM, MAX_ZOOM);
            SetZoom(currentZoom);
        }
    }

    /// <summary>
    /// Apply keyboard rotation.
    /// SDK Reference: Viewport3d.ts updateCamera
    /// </summary>
    private void UpdateRotation(float deltaTime)
    {
        if (Mathf.Abs(yawDelta) > 0.001f)
        {
            yawObject.Rotate(0, yawDelta * deltaTime * Mathf.Rad2Deg, 0, Space.Self);
        }

        if (Mathf.Abs(pitchDelta) > 0.001f)
        {
            float currentPitch = pitchObject.localRotation.eulerAngles.x;
            if (currentPitch > 180) currentPitch -= 360;

            float newPitch = currentPitch + (pitchDelta * deltaTime * Mathf.Rad2Deg);
            newPitch = Mathf.Clamp(newPitch, MIN_PITCH * Mathf.Rad2Deg, MAX_PITCH * Mathf.Rad2Deg);

            pitchObject.localRotation = Quaternion.Euler(newPitch, 0, 0);
        }
    }

    /// <summary>
    /// Follow player with lerping.
    /// SDK Reference: Viewport3d.ts line 314-318
    /// SDK uses fixed 0.1 lerp factor
    /// </summary>
    private void FollowPlayer()
    {
        // Get player's perceived location (for smooth following)
        Vector3 targetPosition = player.transform.position + followOffset;

        // Lerp to target position
        // SDK Reference: "this.pivot.position.lerp(v, 0.1);"
        transform.position = Vector3.Lerp(transform.position, targetPosition, FOLLOW_LERP);
    }

    /// <summary>
    /// Set camera zoom distance.
    /// </summary>
    private void SetZoom(float distance)
    {
        cinemachineCamera.transform.localPosition = new Vector3(0, 0, distance);
    }

    /// <summary>
    /// Handle raycasting for world clicks.
    /// SDK Reference: Viewport3d.ts translateClick
    /// </summary>
    private void HandleRaycasting()
    {
        // Left click for movement
        if (Input.GetMouseButtonDown(0))
        {
            // IMPORTANT: Set up the clickable layer mask properly
            // Make sure "Clickable" layer is assigned in inspector
            if (clickableLayers == -1 || clickableLayers == 0)
            {
                Debug.LogWarning("CameraController: clickableLayers not set! Please assign the Clickable layer in the inspector.");
                return;
            }

            // In Cinemachine 3, we need to get the actual Camera component
            Camera cam = Camera.main; // CinemachineBrain renders through main camera
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f, clickableLayers))
            {
                // Convert to grid position
                Vector2Int gridPos = GridManager.Instance.WorldToGrid(hit.point);

                // Check if valid tile
                Tile tile = GridManager.Instance.GetTileAt(gridPos);
                if (tile != null)
                {
                    // Move player
                    player.MoveTo(gridPos.x, gridPos.y);
                    lastClickedTile = gridPos;

                    if (showDebugInfo)
                    {
                        Debug.Log($"Camera: Clicked tile {gridPos}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get current camera rotation for debugging.
    /// </summary>
    public float GetYawDegrees()
    {
        return yawObject.eulerAngles.y;
    }

    public float GetPitchDegrees()
    {
        float pitch = pitchObject.localRotation.eulerAngles.x;
        if (pitch > 180) pitch -= 360;
        return pitch;
    }

    public float GetCurrentZoom()
    {
        return currentZoom;
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying)
            return;

        // Draw last clicked tile
        if (lastClickedTile.HasValue && GridManager.Instance != null)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(lastClickedTile.Value);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldPos + Vector3.up * 0.1f, Vector3.one);
        }
    }
}

