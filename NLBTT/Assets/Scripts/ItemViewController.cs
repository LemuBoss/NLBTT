using UnityEngine;

/// <summary>
/// Integrates camera and item display systems
/// Automatically shows/hides items when entering/exiting Item View
/// </summary>
public class ItemViewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private ItemDisplayManager itemDisplayManager;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private CameraController.CameraMode previousMode;

    private void Awake()
    {
        if (cameraController == null)
        {
            cameraController = Object.FindFirstObjectByType<CameraController>();
        }

        if (itemDisplayManager == null)
        {
            itemDisplayManager = Object.FindFirstObjectByType<ItemDisplayManager>();
        }

        if (cameraController == null)
        {
            Debug.LogError("[ItemViewController] CameraController not found!");
        }

        if (itemDisplayManager == null)
        {
            Debug.LogError("[ItemViewController] ItemDisplayManager not found!");
        }

        previousMode = CameraController.CameraMode.ZoomedIn;
    }

    private void Update()
    {
        if (cameraController == null) return;

        CameraController.CameraMode currentMode = cameraController.GetCurrentMode();

        // Detect mode changes
        if (currentMode != previousMode)
        {
            OnCameraModeChanged(previousMode, currentMode);
            previousMode = currentMode;
        }
    }

    /// <summary>
    /// Called when camera mode changes
    /// </summary>
    private void OnCameraModeChanged(CameraController.CameraMode oldMode, CameraController.CameraMode newMode)
    {
        LogDebug($"Camera mode changed: {oldMode} → {newMode}");

        // Entering Item View
        if (newMode == CameraController.CameraMode.ItemView)
        {
            OnEnterItemView();
        }
        // Exiting Item View
        else if (oldMode == CameraController.CameraMode.ItemView)
        {
            OnExitItemView();
        }
    }

    /// <summary>
    /// Called when entering Item View
    /// </summary>
    private void OnEnterItemView()
    {
        LogDebug("Entering Item View - showing items");

        if (itemDisplayManager != null)
        {
            itemDisplayManager.ShowItems();
        }

        // 🔊 SOUND: Play Item View open sound here
        // AudioManager.Instance.PlaySound("ItemViewOpen");
    }

    /// <summary>
    /// Called when exiting Item View
    /// </summary>
    private void OnExitItemView()
    {
        LogDebug("Exiting Item View - hiding items");

        if (itemDisplayManager != null)
        {
            itemDisplayManager.HideItems();
        }

        // 🔊 SOUND: Play Item View close sound here
        // AudioManager.Instance.PlaySound("ItemViewClose");
    }

    /// <summary>
    /// Public method to manually trigger Item View (alternative to D key)
    /// </summary>
    public void OpenItemView()
    {
        if (cameraController != null && cameraController.GetCurrentMode() == CameraController.CameraMode.ZoomedIn)
        {
            cameraController.SetCameraMode(CameraController.CameraMode.ItemView);
        }
    }

    /// <summary>
    /// Public method to manually exit Item View (alternative to A key)
    /// </summary>
    public void CloseItemView()
    {
        if (cameraController != null && cameraController.GetCurrentMode() == CameraController.CameraMode.ItemView)
        {
            cameraController.SetCameraMode(CameraController.CameraMode.ZoomedIn);
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ItemViewController] {message}");
        }
    }
}

