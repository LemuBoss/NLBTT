using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string playerTag = "Player"; // Fallback: find by tag
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
    
    [Header("Zoom Transition")]
    [SerializeField] private float zoomTransitionSpeed = 8f;
    
    private bool isZoomedOut = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float currentYRotation = 0f; // Track horizontal rotation angle
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
        // Try finding by tag first
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log("Camera found player by tag: " + playerTag);
        }
    }
    
    private void Update()
    {
        // If player not found yet, try to find it
        if (playerTransform == null && autoFindPlayer)
        {
            FindPlayer();
        }
        
        HandleZoomInput();
        HandleRotationInput();
        UpdateCameraTransform();
    }
    
    private void HandleRotationInput()
    {
        // Only allow rotation when zoomed in and right mouse button is held
        if (!isZoomedOut && enableRotation && Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            currentYRotation += mouseX * rotationSpeed * Time.deltaTime;
            currentXRotation += mouseY * rotationSpeed * Time.deltaTime;
        }
        else if (!isZoomedOut && enableRotation)
        {
            // Smoothly reset rotation to default (0) when not rotating
            currentYRotation = Mathf.Lerp(currentYRotation, 0f, rotationResetSpeed * Time.deltaTime);
            currentXRotation = Mathf.Lerp(currentXRotation, 0f, rotationResetSpeed * Time.deltaTime);
        }
    }
    
    private void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll < 0f && !isZoomedOut)
        {
            // Scroll down - zoom out
            ZoomOut();
        }
        else if (scroll > 0f && isZoomedOut)
        {
            // Scroll up - zoom in
            ZoomIn();
        }
    }
    
    private void ZoomOut()
    {
        isZoomedOut = true;
        targetPosition = zoomedOutPosition;
        targetRotation = Quaternion.Euler(zoomedOutRotation);
    }
    
    private void ZoomIn()
    {
        isZoomedOut = false;
        UpdateZoomedInTarget();
    }
    
    private void UpdateZoomedInTarget()
    {
        if (playerTransform == null) return;
        
        // Get the center point (player position)
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
    
    private void UpdateCameraTransform()
    {
        if (playerTransform == null) return;
        
        // Update target if zoomed in and following player
        if (!isZoomedOut)
        {
            UpdateZoomedInTarget();
        }
        
        // Determine interpolation speed based on zoom state
        float posSpeed = isZoomedOut ? zoomTransitionSpeed : cameraSmoothSpeed;
        float rotSpeed = isZoomedOut ? zoomTransitionSpeed : zoomTransitionSpeed;
        
        // Smoothly move camera to target
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
    
    // Optional: Public method to force camera to specific zoom state
    public void SetZoomState(bool zoomOut)
    {
        if (zoomOut)
            ZoomOut();
        else
            ZoomIn();
    }
    
    // Optional: Check current zoom state
    public bool IsZoomedOut()
    {
        return isZoomedOut;
    }
    
    // Public method to manually set player reference
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
        if (!isZoomedOut)
        {
            UpdateZoomedInTarget();
        }
    }
}