using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages event icons that hover above cards with active events
/// Icons are only visible when:
/// - Card is face-up (not turned around)
/// - Card has an active event (hasEvent && !isEventClosed)
/// - Player is NOT currently standing on the card
/// </summary>
public class EventIconManager : MonoBehaviour
{
    [Header("Icon Settings")]
    [SerializeField] private GameObject eventIconPrefab;
    [SerializeField] private Vector3 iconOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 iconScale = new Vector3(0.3f, 0.3f, 0.3f);
    [SerializeField] private string iconLayerName = "Ignore Raycast"; // Layer for icons to ignore raycasts
    
    [Header("References")]
    private BoardManager boardManager;
    private Player player;
    
    // Dictionary to track icons by grid position
    private Dictionary<Vector2Int, GameObject> activeIcons = new Dictionary<Vector2Int, GameObject>();
    
    // Track last known player position to detect movement
    private Vector2Int lastPlayerPosition;

    private void Awake()
    {
        boardManager = FindObjectOfType<BoardManager>();
        player = FindObjectOfType<Player>();
        
        if (boardManager == null)
        {
            Debug.LogError("[EventIconManager] BoardManager not found!");
        }
        
        if (player == null)
        {
            Debug.LogError("[EventIconManager] Player not found!");
        }
        
        if (eventIconPrefab == null)
        {
            Debug.LogWarning("[EventIconManager] No event icon prefab assigned! Assign a sprite or quad prefab in the Inspector.");
        }
    }

    private void Start()
    {
        if (player != null)
        {
            lastPlayerPosition = player.GetPosition();
        }
        
        // Initial check after board generation
        Invoke(nameof(RefreshAllIcons), 0.2f);
        
        // Subscribe to card flip events
        CardVisual.OnAnyCardFlipped += OnCardFlipped;
    }

    private void Update()
    {
        // Check if player has moved
        if (player != null)
        {
            Vector2Int currentPlayerPos = player.GetPosition();
            
            if (currentPlayerPos != lastPlayerPosition)
            {
                // Player moved - update icons at old and new positions
                UpdateIconAtPosition(lastPlayerPosition);
                UpdateIconAtPosition(currentPlayerPos);
                lastPlayerPosition = currentPlayerPos;
            }
        }
        
        // Periodically check for card state changes (like events being closed)
        // This is a lightweight check that only updates when needed
        if (Time.frameCount % 30 == 0) // Check every 30 frames (~0.5 seconds at 60fps)
        {
            CheckForEventStateChanges();
        }
    }

    /// <summary>
    /// Called whenever any card is flipped face-up
    /// </summary>
    private void OnCardFlipped(CardVisual cardVisual)
    {
        if (boardManager == null) return;
        
        // Find the position of this card
        Vector2Int cardPos = FindCardPosition(cardVisual);
        
        if (cardPos.x >= 0) // Valid position found
        {
            Debug.Log($"[EventIconManager] Card flipped at ({cardPos.x}, {cardPos.y}), checking for event icon");
            UpdateIconAtPosition(cardPos);
        }
    }
    
    /// <summary>
    /// Finds the grid position of a CardVisual
    /// </summary>
    private Vector2Int FindCardPosition(CardVisual visual)
    {
        int width = boardManager.GetGridWidth();
        int height = boardManager.GetGridHeight();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (boardManager.GetCardVisualAt(x, y) == visual)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1); // Not found
    }

    /// <summary>
    /// Refreshes all icons on the board
    /// Call this after board generation or major changes
    /// </summary>
    public void RefreshAllIcons()
    {
        if (boardManager == null) return;
        
        Debug.Log("[EventIconManager] Refreshing all event icons");
        
        // Clear existing icons
        ClearAllIcons();
        
        // Check every position on the board
        int width = boardManager.GetGridWidth();
        int height = boardManager.GetGridHeight();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                UpdateIconAtPosition(pos);
            }
        }
    }

    /// <summary>
    /// Updates the icon at a specific position based on card state
    /// </summary>
    private void UpdateIconAtPosition(Vector2Int position)
    {
        if (boardManager == null) return;
        
        Card card = boardManager.GetCardAt(position.x, position.y);
        
        if (card == null) return;
        
        bool shouldShowIcon = ShouldShowIconForCard(card, position);
        
        // If icon should be shown but doesn't exist, create it
        if (shouldShowIcon && !activeIcons.ContainsKey(position))
        {
            CreateIconAt(position);
        }
        // If icon exists but shouldn't be shown, destroy it
        else if (!shouldShowIcon && activeIcons.ContainsKey(position))
        {
            DestroyIconAt(position);
        }
    }

    /// <summary>
    /// Determines if an icon should be shown for a card
    /// </summary>
    private bool ShouldShowIconForCard(Card card, Vector2Int position)
    {
        // Check if card is face-up
        if (card.TurnedAround)
            return false;
        
        // Check if card has an active event
        if (!card.HasActiveEvent)
            return false;
        
        // Check if player is NOT on this card
        if (player != null && player.GetPosition() == position)
            return false;
        
        return true;
    }

    /// <summary>
    /// Creates an icon at the specified grid position
    /// </summary>
    private void CreateIconAt(Vector2Int gridPosition)
    {
        if (eventIconPrefab == null)
        {
            Debug.LogWarning("[EventIconManager] Cannot create icon - no prefab assigned!");
            return;
        }
        
        CardVisual cardVisual = boardManager.GetCardVisualAt(gridPosition.x, gridPosition.y);
        
        if (cardVisual == null)
        {
            Debug.LogWarning($"[EventIconManager] Cannot create icon at ({gridPosition.x}, {gridPosition.y}) - no CardVisual found");
            return;
        }
        
        Vector3 cardWorldPosition = cardVisual.transform.position;
        Vector3 iconPosition = cardWorldPosition + iconOffset;
        
        // Create icon with no rotation (sits upright in world space, not facing camera)
        GameObject icon = Instantiate(eventIconPrefab, iconPosition, Quaternion.identity, transform);
        icon.name = $"EventIcon_{gridPosition.x}_{gridPosition.y}";
        icon.transform.localScale = iconScale;
        
        // Set icon to "Ignore Raycast" layer so it doesn't block mouse clicks
        int layer = LayerMask.NameToLayer(iconLayerName);
        if (layer == -1)
        {
            Debug.LogWarning($"[EventIconManager] Layer '{iconLayerName}' not found! Icon may block raycasts. Using default layer.");
        }
        else
        {
            SetLayerRecursively(icon, layer);
        }
        
        // Store in dictionary
        activeIcons[gridPosition] = icon;
        
        Debug.Log($"[EventIconManager] Created icon at grid ({gridPosition.x}, {gridPosition.y}), world position {iconPosition}");
    }

    /// <summary>
    /// Recursively sets the layer for a GameObject and all its children
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Destroys the icon at the specified grid position
    /// </summary>
    private void DestroyIconAt(Vector2Int gridPosition)
    {
        if (activeIcons.TryGetValue(gridPosition, out GameObject icon))
        {
            Destroy(icon);
            activeIcons.Remove(gridPosition);
            Debug.Log($"[EventIconManager] Destroyed icon at ({gridPosition.x}, {gridPosition.y})");
        }
    }

    /// <summary>
    /// Clears all active icons from the board
    /// </summary>
    private void ClearAllIcons()
    {
        foreach (var icon in activeIcons.Values)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        
        activeIcons.Clear();
        Debug.Log("[EventIconManager] Cleared all event icons");
    }

    /// <summary>
    /// Checks all active icons to see if their card states have changed
    /// </summary>
    private void CheckForEventStateChanges()
    {
        if (boardManager == null) return;
        
        List<Vector2Int> positionsToUpdate = new List<Vector2Int>(activeIcons.Keys);
        
        foreach (Vector2Int pos in positionsToUpdate)
        {
            UpdateIconAtPosition(pos);
        }
    }

    /// <summary>
    /// Call this when a card's event is closed to immediately update its icon
    /// </summary>
    public void OnEventClosed(Vector2Int position)
    {
        Debug.Log($"[EventIconManager] Event closed at ({position.x}, {position.y}), removing icon");
        UpdateIconAtPosition(position);
    }

    /// <summary>
    /// Call this when a card is flipped face-up to show its icon if it has an event
    /// </summary>
    public void OnCardRevealed(Vector2Int position)
    {
        Debug.Log($"[EventIconManager] Card revealed at ({position.x}, {position.y}), checking for event icon");
        UpdateIconAtPosition(position);
    }

    /// <summary>
    /// Call this when the board is regenerated to clear and refresh all icons
    /// </summary>
    public void OnBoardRegenerated()
    {
        Debug.Log("[EventIconManager] Board regenerated, refreshing icons");
        RefreshAllIcons();
    }

    private void OnDestroy()
    {
        ClearAllIcons();
        
        // Unsubscribe from events
        CardVisual.OnAnyCardFlipped -= OnCardFlipped;
    }
}
