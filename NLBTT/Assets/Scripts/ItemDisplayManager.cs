using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages visual display of player's items in Item View
/// Spawns item prefabs at fixed positions, handles hover effects and destruction
/// </summary>
public class ItemDisplayManager : MonoBehaviour
{
    [System.Serializable]
    public class ItemPrefabMapping
    {
        public ItemManager.ItemType itemType;
        public GameObject prefab;
    }

    [Header("Item Prefab Mappings")]
    [SerializeField] private ItemPrefabMapping[] itemPrefabMappings;

    [Header("Item Spawn Positions")]
    [SerializeField] private Transform[] itemSpawnPositions = new Transform[3];
    
    [Header("Hover Animation")]
    [SerializeField] private float hoverHeight = 0.3f;
    [SerializeField] private float hoverAnimationSpeed = 5f;
    
    [Header("Destruction")]
    [SerializeField] private float destroyHoldTime = 1.5f; // Seconds to hold mouse button
    
    [Header("References")]
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private ItemTooltipUI tooltipUI; // Will create this next
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Internal state
    private List<ItemDisplayObject> activeDisplayObjects = new List<ItemDisplayObject>();
    private ItemDisplayObject currentHoveredItem = null;
    private float destroyProgress = 0f;
    private bool isHoldingForDestroy = false;

    private void Awake()
    {
        if (itemManager == null)
        {
            itemManager = Object.FindFirstObjectByType<ItemManager>();
        }
        
        if (cameraController == null)
        {
            cameraController = Object.FindFirstObjectByType<CameraController>();
        }

        if (tooltipUI == null)
        {
            tooltipUI = Object.FindFirstObjectByType<ItemTooltipUI>();
        }

        ValidateSpawnPositions();
    }

    private void Update()
    {
        // Only active when in Item View
        if (cameraController != null && !cameraController.IsInItemView())
        {
            return;
        }

        HandleMouseHover();
        HandleDestruction();
    }

    /// <summary>
    /// Called when entering Item View - spawns all current items
    /// </summary>
    public void ShowItems()
    {
        if (itemManager == null)
        {
            Debug.LogError("[ItemDisplayManager] ItemManager not found!");
            return;
        }

        // Clear existing items
        ClearItems();

        // Get current inventory
        List<ItemManager.ItemType> inventory = itemManager.GetInventory();

        LogDebug($"Showing {inventory.Count} items");

        // Spawn item prefabs at designated positions
        for (int i = 0; i < inventory.Count && i < itemSpawnPositions.Length; i++)
        {
            SpawnItemAt(inventory[i], i);
        }
    }

    /// <summary>
    /// Called when exiting Item View - removes all item displays
    /// </summary>
    public void HideItems()
    {
        ClearItems();
        
        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }

        currentHoveredItem = null;
        destroyProgress = 0f;
        isHoldingForDestroy = false;

        LogDebug("Hidden all items");
    }

    /// <summary>
    /// Spawns a single item prefab at the specified position index
    /// </summary>
    private void SpawnItemAt(ItemManager.ItemType itemType, int positionIndex)
    {
        if (positionIndex >= itemSpawnPositions.Length)
        {
            Debug.LogError($"[ItemDisplayManager] Position index {positionIndex} out of range!");
            return;
        }

        GameObject prefab = GetPrefabForItem(itemType);
        if (prefab == null)
        {
            Debug.LogError($"[ItemDisplayManager] No prefab found for {itemType}");
            return;
        }

        Transform spawnPos = itemSpawnPositions[positionIndex];
        GameObject itemObj = Instantiate(prefab, spawnPos.position, spawnPos.rotation, transform);
        itemObj.name = $"Item_{itemType}_{positionIndex}";

        // Add ItemDisplayObject component to handle interactions
        ItemDisplayObject displayObj = itemObj.AddComponent<ItemDisplayObject>();
        displayObj.Initialize(itemType, positionIndex, this);

        activeDisplayObjects.Add(displayObj);

        LogDebug($"Spawned {itemType} at position {positionIndex}");
    }

    /// <summary>
    /// Gets the prefab for a specific item type
    /// </summary>
    private GameObject GetPrefabForItem(ItemManager.ItemType itemType)
    {
        foreach (var mapping in itemPrefabMappings)
        {
            if (mapping.itemType == itemType)
            {
                return mapping.prefab;
            }
        }
        return null;
    }

    /// <summary>
    /// Clears all spawned item displays
    /// </summary>
    private void ClearItems()
    {
        foreach (var displayObj in activeDisplayObjects)
        {
            if (displayObj != null)
            {
                Destroy(displayObj.gameObject);
            }
        }
        activeDisplayObjects.Clear();
    }

    /// <summary>
    /// Handles mouse hover detection using raycasts
    /// </summary>
    private void HandleMouseHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            ItemDisplayObject displayObj = hit.collider.GetComponent<ItemDisplayObject>();

            if (displayObj != null)
            {
                // Mouse is over an item
                if (currentHoveredItem != displayObj)
                {
                    // New item hovered
                    OnItemHoverEnter(displayObj);
                }
            }
            else
            {
                // Mouse is not over any item
                if (currentHoveredItem != null)
                {
                    OnItemHoverExit();
                }
            }
        }
        else
        {
            // No hit at all
            if (currentHoveredItem != null)
            {
                OnItemHoverExit();
            }
        }
    }

    /// <summary>
    /// Called when mouse enters an item
    /// </summary>
    private void OnItemHoverEnter(ItemDisplayObject item)
    {
        // Exit previous item if any
        if (currentHoveredItem != null)
        {
            currentHoveredItem.SetHovered(false);
        }

        currentHoveredItem = item;
        currentHoveredItem.SetHovered(true);

        // Show tooltip
        if (tooltipUI != null)
        {
            tooltipUI.Show(currentHoveredItem.ItemType);
        }

        // 🔊 SOUND: Play hover sound here
        // AudioManager.Instance.PlaySound("ItemHover");

        LogDebug($"Hover entered: {item.ItemType}");
    }

    /// <summary>
    /// Called when mouse exits an item
    /// </summary>
    private void OnItemHoverExit()
    {
        if (currentHoveredItem != null)
        {
            currentHoveredItem.SetHovered(false);
            currentHoveredItem = null;
        }

        // Hide tooltip
        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }

        // Reset destruction progress
        destroyProgress = 0f;
        isHoldingForDestroy = false;

        LogDebug("Hover exited");
    }

    /// <summary>
    /// Handles item destruction when holding mouse button
    /// </summary>
    private void HandleDestruction()
    {
        if (currentHoveredItem == null)
        {
            destroyProgress = 0f;
            isHoldingForDestroy = false;
            return;
        }

        // Check if left mouse button is held down
        if (Input.GetMouseButton(0)) // Left mouse button
        {
            if (!isHoldingForDestroy)
            {
                isHoldingForDestroy = true;
                // 🔊 SOUND: Play destruction start sound here
                // AudioManager.Instance.PlaySound("ItemDestroyStart");
            }

            // Increment progress
            destroyProgress += Time.deltaTime / destroyHoldTime;
            destroyProgress = Mathf.Clamp01(destroyProgress);

            // Update tooltip with progress
            if (tooltipUI != null)
            {
                tooltipUI.UpdateDestroyProgress(destroyProgress);
            }

            // Complete destruction
            if (destroyProgress >= 1f)
            {
                DestroyCurrentItem();
            }
        }
        else
        {
            // Mouse button released - reset progress
            if (isHoldingForDestroy)
            {
                // 🔊 SOUND: Play destruction cancel sound here
                // AudioManager.Instance.PlaySound("ItemDestroyCancel");
            }

            destroyProgress = 0f;
            isHoldingForDestroy = false;

            // Update tooltip
            if (tooltipUI != null)
            {
                tooltipUI.UpdateDestroyProgress(0f);
            }
        }
    }

    /// <summary>
    /// Destroys the currently hovered item
    /// </summary>
    private void DestroyCurrentItem()
    {
        if (currentHoveredItem == null || itemManager == null)
            return;

        ItemManager.ItemType itemType = currentHoveredItem.ItemType;

        LogDebug($"Destroying item: {itemType}");

        // 🔊 SOUND: Play destruction complete sound here
        // AudioManager.Instance.PlaySound("ItemDestroyComplete");

        // Destroy in ItemManager (triggers item effects)
        itemManager.DestroyItem(itemType);

        // Remove visual
        activeDisplayObjects.Remove(currentHoveredItem);
        Destroy(currentHoveredItem.gameObject);

        // Reset state
        currentHoveredItem = null;
        destroyProgress = 0f;
        isHoldingForDestroy = false;

        // Hide tooltip
        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }

        // Refresh display (items might have shifted positions)
        ShowItems();
    }

    /// <summary>
    /// Gets hover height for animation
    /// </summary>
    public float GetHoverHeight() => hoverHeight;

    /// <summary>
    /// Gets hover animation speed
    /// </summary>
    public float GetHoverSpeed() => hoverAnimationSpeed;

    private void ValidateSpawnPositions()
    {
        if (itemSpawnPositions.Length != 3)
        {
            Debug.LogWarning($"[ItemDisplayManager] Expected 3 spawn positions, got {itemSpawnPositions.Length}");
        }

        for (int i = 0; i < itemSpawnPositions.Length; i++)
        {
            if (itemSpawnPositions[i] == null)
            {
                Debug.LogError($"[ItemDisplayManager] Spawn position {i} is null!");
            }
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ItemDisplayManager] {message}");
        }
    }
}

/// <summary>
/// Component attached to each spawned item prefab
/// Handles individual item behavior (hover animation, collider, etc.)
/// </summary>
public class ItemDisplayObject : MonoBehaviour
{
    public ItemManager.ItemType ItemType { get; private set; }
    public int PositionIndex { get; private set; }
    
    private ItemDisplayManager manager;
    private Vector3 basePosition;
    private bool isHovered = false;
    private float currentHoverOffset = 0f;

    public void Initialize(ItemManager.ItemType itemType, int positionIndex, ItemDisplayManager manager)
    {
        this.ItemType = itemType;
        this.PositionIndex = positionIndex;
        this.manager = manager;
        this.basePosition = transform.position;

        // Ensure collider exists
        if (GetComponent<Collider>() == null)
        {
            // Add a box collider if none exists
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning($"[ItemDisplayObject] Added BoxCollider to {itemType} - adjust bounds in prefab!");
        }
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
    }

    private void Update()
    {
        AnimateHover();
    }

    /// <summary>
    /// Smoothly animates item up/down when hovered
    /// </summary>
    private void AnimateHover()
    {
        if (manager == null) return;

        float targetOffset = isHovered ? manager.GetHoverHeight() : 0f;
        currentHoverOffset = Mathf.Lerp(
            currentHoverOffset, 
            targetOffset, 
            manager.GetHoverSpeed() * Time.deltaTime
        );

        transform.position = basePosition + Vector3.up * currentHoverOffset;
    }
}

