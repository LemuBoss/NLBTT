using UnityEngine;

/// <summary>
/// Camera controller with three modes:
/// 1. Zoomed In (Player Focus) - W to enter from Zoomed Out
/// 2. Zoomed Out (Board Overview) - S to enter from Zoomed In
/// 3. Item View (Fixed Perspective) - D to enter from Zoomed In, A to exit back to Zoomed In
/// </summary>
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        ZoomedIn,    // Following player
        ZoomedOut,   // Board overview
        ItemView     // Fixed perspective for item display
    }

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool autoFindPlayer = true;
    
    [Header("Zoomed In Settings (Following Player)")]
    [SerializeField] private float zoomedInHeight = 10f;
    [SerializeField] private float zoomedInRotationX = 45f;
    [SerializeField] private Vector3 zoomedInOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private float cameraSmoothSpeed = 5f;
    
    [Header("Camera Rotation (Right Mouse Button)")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationResetSpeed = 10f;
    
    [Header("Zoomed Out Settings (Overview)")]
    [SerializeField] private Vector3 zoomedOutPosition = new Vector3(0f, 50f, 0f);
    [SerializeField] private Vector3 zoomedOutRotation = new Vector3(90f, 0f, 0f);
    
    [Header("Item View Settings (Fixed Perspective)")]
    [SerializeField] private Vector3 itemViewPosition = new Vector3(5f, 3f, -5f);
    [SerializeField] private Vector3 itemViewRotation = new Vector3(30f, -45f, 0f);
    [SerializeField] private float itemViewTransitionSpeed = 15f; // Fast and snappy!
    
    [Header("Zoom Transition")]
    [SerializeField] private float zoomTransitionSpeed = 8f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private CameraMode currentMode = CameraMode.ZoomedIn;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float currentYRotation = 0f;
    private float currentXRotation = 0f;
    
    private void Start()
    {
        if (playerTransform == null && autoFindPlayer)
        {
            FindPlayer();
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("Player Transform not assigned. Camera will search for player each frame.");
        }
        else
        {
            // Start in zoomed-in mode
            UpdateZoomedInTarget();
        }
    }
    
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            LogDebug("Camera found player by tag: " + playerTag);
        }
    }
    
    private void Update()
    {
        if (playerTransform == null && autoFindPlayer)
        {
            FindPlayer();
        }
        
        HandleCameraModeInput();
        HandleRotationInput();
        UpdateCameraTransform();
    }
    
    /// <summary>
    /// Handles keyboard input for switching camera modes
    /// W/S for Zoom In/Out, D for Item View, A to exit Item View
    /// </summary>
    private void HandleCameraModeInput()
    {
        // W: Zoom In (only from Zoomed Out)
        if (Input.GetKeyDown(KeyCode.W) && currentMode == CameraMode.ZoomedOut)
        {
            SwitchToZoomedIn();
        }
        // S: Zoom Out (only from Zoomed In)
        else if (Input.GetKeyDown(KeyCode.S) && currentMode == CameraMode.ZoomedIn)
        {
            SwitchToZoomedOut();
        }
        // D: Item View (only from Zoomed In)
        else if (Input.GetKeyDown(KeyCode.D) && currentMode == CameraMode.ZoomedIn)
        {
            SwitchToItemView();
        }
        // A: Exit Item View back to Zoomed In (only from Item View)
        else if (Input.GetKeyDown(KeyCode.A) && currentMode == CameraMode.ItemView)
        {
            SwitchToZoomedIn();
        }
    }
    
    /// <summary>
    /// Handles right-mouse button camera rotation (only in Zoomed In mode)
    /// </summary>
    private void HandleRotationInput()
    {
        // Only allow rotation when zoomed in and right mouse button is held
        if (currentMode == CameraMode.ZoomedIn && enableRotation && Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            currentYRotation += mouseX * rotationSpeed * Time.deltaTime;
            currentXRotation += mouseY * rotationSpeed * Time.deltaTime;
        }
        else if (currentMode == CameraMode.ZoomedIn && enableRotation)
        {
            // Smoothly reset rotation to default when not rotating
            currentYRotation = Mathf.Lerp(currentYRotation, 0f, rotationResetSpeed * Time.deltaTime);
            currentXRotation = Mathf.Lerp(currentXRotation, 0f, rotationResetSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Switch to Zoomed Out mode (board overview)
    /// </summary>
    private void SwitchToZoomedOut()
    {
        currentMode = CameraMode.ZoomedOut;
        targetPosition = zoomedOutPosition;
        targetRotation = Quaternion.Euler(zoomedOutRotation);
        LogDebug("Switched to ZOOMED OUT mode");
    }
    
    /// <summary>
    /// Switch to Zoomed In mode (following player)
    /// </summary>
    private void SwitchToZoomedIn()
    {
        currentMode = CameraMode.ZoomedIn;
        UpdateZoomedInTarget();
        LogDebug("Switched to ZOOMED IN mode");
    }
    
    /// <summary>
    /// Switch to Item View mode (fixed perspective for items)
    /// </summary>
    private void SwitchToItemView()
    {
        currentMode = CameraMode.ItemView;
        targetPosition = itemViewPosition;
        targetRotation = Quaternion.Euler(itemViewRotation);
        LogDebug("Switched to ITEM VIEW mode");
    }
    
    /// <summary>
    /// Updates target position/rotation for Zoomed In mode (following player)
    /// </summary>
    private void UpdateZoomedInTarget()
    {
        if (playerTransform == null) return;
        
        Vector3 centerPoint = playerTransform.position;
        
        // Calculate rotated offset based on current Y rotation
        Quaternion rotation = Quaternion.Euler(0f, currentYRotation, 0f);
        Vector3 rotatedOffset = rotation * zoomedInOffset;
        
        // Camera orbits around player with the rotated offset
        targetPosition = new Vector3(
            centerPoint.x + rotatedOffset.x,
            zoomedInHeight,
            centerPoint.z + rotatedOffset.z
        );
        
        // Camera looks down at the same angle but rotates horizontally
        targetRotation = Quaternion.Euler(zoomedInRotationX, currentYRotation, 0f);
    }
    
    /// <summary>
    /// Updates camera transform with smooth interpolation
    /// Uses linear interpolation (no easing) for snappy transitions
    /// </summary>
    private void UpdateCameraTransform()
    {
        if (playerTransform == null) return;
        
        // Update target if in zoomed in mode (following player)
        if (currentMode == CameraMode.ZoomedIn)
        {
            UpdateZoomedInTarget();
        }
        
        // Determine interpolation speed based on mode
        float posSpeed;
        float rotSpeed;
        
        switch (currentMode)
        {
            case CameraMode.ZoomedIn:
                posSpeed = cameraSmoothSpeed;
                rotSpeed = zoomTransitionSpeed;
                break;
            case CameraMode.ZoomedOut:
                posSpeed = zoomTransitionSpeed;
                rotSpeed = zoomTransitionSpeed;
                break;
            case CameraMode.ItemView:
                posSpeed = itemViewTransitionSpeed; // Fast and snappy!
                rotSpeed = itemViewTransitionSpeed;
                break;
            default:
                posSpeed = cameraSmoothSpeed;
                rotSpeed = zoomTransitionSpeed;
                break;
        }
        
        // LINEAR interpolation (no easing) using Lerp/Slerp
        // This creates smooth but constant-speed transitions
        transform.position = Vector3.Lerp(
            transform.position, 
            targetPosition, 
            posSpeed * Time.deltaTime
        );
        
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            rotSpeed * Time.deltaTime
        );
    }
    
    #region Public API
    
    /// <summary>
    /// Gets the current camera mode
    /// </summary>
    public CameraMode GetCurrentMode()
    {
        return currentMode;
    }
    
    /// <summary>
    /// Force camera to a specific mode (for external control)
    /// </summary>
    public void SetCameraMode(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.ZoomedIn:
                SwitchToZoomedIn();
                break;
            case CameraMode.ZoomedOut:
                SwitchToZoomedOut();
                break;
            case CameraMode.ItemView:
                SwitchToItemView();
                break;
        }
    }
    
    /// <summary>
    /// Check if currently in Item View
    /// </summary>
    public bool IsInItemView()
    {
        return currentMode == CameraMode.ItemView;
    }
    
    /// <summary>
    /// Check if currently zoomed out
    /// </summary>
    public bool IsZoomedOut()
    {
        return currentMode == CameraMode.ZoomedOut;
    }
    
    /// <summary>
    /// Manually set player reference
    /// </summary>
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
        if (currentMode == CameraMode.ZoomedIn)
        {
            UpdateZoomedInTarget();
        }
    }
    
    /// <summary>
    /// Manually set Item View position and rotation (for runtime adjustment)
    /// </summary>
    public void SetItemViewTransform(Vector3 position, Vector3 rotation)
    {
        itemViewPosition = position;
        itemViewRotation = rotation;
        
        // If already in Item View, update immediately
        if (currentMode == CameraMode.ItemView)
        {
            targetPosition = itemViewPosition;
            targetRotation = Quaternion.Euler(itemViewRotation);
        }
    }
    
    #endregion
    
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[CameraController] {message}");
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Visualize camera positions in Scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw Zoomed Out position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(zoomedOutPosition, 1f);
        
        // Draw Item View position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(itemViewPosition, 1f);
        
        // Draw direction indicators
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(itemViewPosition, Quaternion.Euler(itemViewRotation) * Vector3.forward * 3f);
    }
    #endif
}


