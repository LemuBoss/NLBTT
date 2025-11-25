using UnityEngine;

/// <summary>
/// MonoBehaviour component that handles visual representation and interactions for cards
/// Attach this to each card GameObject in the scene
/// Card should be a plane with a material that has front/back texture mapped via UV
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class CardVisual : MonoBehaviour
{
    private Card cardLogic;
    private bool isAdjacentToPlayer = false;
    private BoardManager boardManager;
    private Player player;

    [Header("Outline Settings")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float outlineThickness = 1.05f;

    [Header("Outline Colors")]
    [SerializeField] private Color unturnedOutlineColor = Color.white;
    [SerializeField] private Color walkableOutlineColor = Color.green;
    [SerializeField] private Color unwalkableOutlineColor = Color.red;

    private MeshRenderer meshRenderer;
    private GameObject outlineObject;
    private bool isHovered = false;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        CreateOutlineObject();
        boardManager = Object.FindFirstObjectByType<BoardManager>();
        player = Object.FindFirstObjectByType<Player>();
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                //Debug.Log($"Raycast hitting: {gameObject.name}");
            }
        }
    }

    private void CreateOutlineObject()
    {
        outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * outlineThickness;

        MeshFilter sourceMeshFilter = GetComponent<MeshFilter>();
        MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
        outlineMeshFilter.mesh = sourceMeshFilter.mesh;

        MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
        if (outlineMaterial != null)
        {
            outlineRenderer.material = outlineMaterial;
        }
        else
        {
            Debug.LogWarning($"Outline material not assigned on {gameObject.name}");
        }

        outlineObject.SetActive(false);
    }

    private void OnMouseEnter()
    {
        // Debug.Log($"Mouse entered on {gameObject.name}");

        // Don't show hover effects while paused or during UI events
        if (IsAnyUIBlocking())
            return;

        if (isAdjacentToPlayer)
        {
            isHovered = true;
            UpdateOutline();
        }
    }

    private void OnMouseExit()
    {
        isHovered = false;
        UpdateOutline();
    }

    private void OnMouseDown()
    {
        // Don't process clicks if pointer is over UI
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[CardVisual] Ignoring click - pointer is over UI element");
            return;
        }

        // Don't process clicks if any UI is blocking
        if (IsAnyUIBlocking())
        {
            Debug.Log("[CardVisual] Ignoring click - UI is blocking interaction");
            return;
        }

        // Only process clicks on adjacent cards
        if (!isAdjacentToPlayer || cardLogic == null)
            return;

        // Get this card's position in the grid
        Vector2Int cardPosition = GetGridPosition();
        if (cardPosition.x < 0 || cardPosition.y < 0)
        {
            Debug.LogWarning($"Invalid card position for {gameObject.name}");
            return;
        }

        // Always turn the card over if it's face-down
        if (cardLogic.TurnedAround)
        {
            TurnCardOver();
        }

        // Try to move the player to this card
        if (player != null)
        {
            bool moveSuccessful = player.TryMoveTo(cardPosition);

            if (!moveSuccessful)
            {
                Debug.Log($"[CardVisual] Player attempted to move to ({cardPosition.x}, {cardPosition.y}) but movement was blocked");
            }
        }
        else
        {
            Debug.LogError("Player reference not found in CardVisual");
        }
    }

    /// <summary>
    /// Checks if any UI is currently blocking card interactions
    /// </summary>
    private bool IsAnyUIBlocking()
    {
        // Check pause menu
        if (PauseMenuManager.IsGamePaused())
        {
            return true;
        }

        // Check event UI
        EventUIManager eventUI = Object.FindFirstObjectByType<EventUIManager>();
        if (eventUI != null && eventUI.IsShowingEvent())
        {
            return true;
        }

        // Check bloodpoint UI
        BloodpointUIManager bloodpointUI = Object.FindFirstObjectByType<BloodpointUIManager>();
        if (bloodpointUI != null && bloodpointUI.IsShowingEvent())
        {
            return true;
        }

        // Check game over/victory UI
        GameOverUIManager gameOverUI = Object.FindFirstObjectByType<GameOverUIManager>();
        if (gameOverUI != null && gameOverUI.IsShowingGameEnd())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets whether this card is adjacent to the player
    /// Called by BoardManager when player moves
    /// </summary>
    public void SetAdjacentToPlayer(bool adjacent)
    {
        isAdjacentToPlayer = adjacent;
        UpdateOutline();
    }

    /// <summary>
    /// Instantly turns the card over (180 degree rotation)
    /// </summary>
    public void TurnCardOver()
    {
        if (cardLogic == null) return;
        if (!cardLogic.TurnedAround) return; // Already turned

        // Rotate 180 degrees around X axis
        transform.Rotate(180f, 0f, 0f);

        // Update card logic state
        cardLogic.TurnOver();

        UpdateCardAppearance();
        UpdateOutline();
    }

    /// <summary>
    /// Updates the card's visual appearance based on its state
    /// </summary>
    private void UpdateCardAppearance()
    {
        UpdateOutline();
    }

    /// <summary>
    /// Updates the outline visibility and color based on card state and hover status
    /// </summary>
    private void UpdateOutline()
    {
        if (outlineObject == null || cardLogic == null) return;

        // Don't show outlines while any UI is blocking
        if (IsAnyUIBlocking())
        {
            outlineObject.SetActive(false);
            return;
        }

        // Only show outline when hovered AND adjacent to player
        if (isHovered && isAdjacentToPlayer)
        {
            outlineObject.SetActive(true);
            Color outlineColor = GetOutlineColor();
            if (outlineObject.GetComponent<MeshRenderer>() != null)
            {
                outlineObject.GetComponent<MeshRenderer>().material.color = outlineColor;
            }
        }
        else
        {
            outlineObject.SetActive(false);
        }
    }

    /// <summary>
    /// Determines the appropriate outline color based on card state
    /// </summary>
    private Color GetOutlineColor()
    {
        if (cardLogic.TurnedAround)
        {
            // Card hasn't been turned yet - white outline
            return unturnedOutlineColor;
        }
        else if (cardLogic.CanMoveOnto)
        {
            // Card is walkable - green outline
            return walkableOutlineColor;
        }
        else
        {
            // Card is not walkable - red outline
            return unwalkableOutlineColor;
        }
    }

    /// <summary>
    /// Call this when the player moves onto this card (from game manager or player controller)
    /// </summary>
    public void OnPlayerEnterCard()
    {
        if (cardLogic != null)
        {
            // Turn card over if needed
            if (cardLogic.TurnedAround)
            {
                TurnCardOver();
            }

            // Trigger card logic
            cardLogic.OnPlayerEnter();
        }
    }

    /// <summary>
    /// Gets this card's position in the grid by parsing its name
    /// Returns Vector2Int(-1, -1) if position cannot be determined
    /// </summary>
    private Vector2Int GetGridPosition()
    {
        // Card names are formatted as "Card_X_Y_TypeName"
        string[] parts = gameObject.name.Split('_');
        if (parts.Length >= 3)
        {
            if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
            {
                return new Vector2Int(x, y);
            }
        }

        Debug.LogWarning($"Could not parse grid position from card name: {gameObject.name}");
        return new Vector2Int(-1, -1);
    }

    /// <summary>
    /// Gets the card logic instance
    /// </summary>
    public Card GetCardLogic()
    {
        return cardLogic;
    }

    /// <summary>
    /// Sets the card logic instance for this visual representation
    /// </summary>
    public void SetCardLogic(Card logic)
    {
        cardLogic = logic;

        // If card is already face-up (like StartCard), rotate it immediately
        if (cardLogic != null && !cardLogic.TurnedAround)
        {
            transform.rotation = Quaternion.Euler(270f, 0f, 0f);
        }

        UpdateCardAppearance();
    }

#if UNITY_EDITOR
    // Visualize outline colors in the editor
    private void OnDrawGizmosSelected()
    {
        if (cardLogic == null) return;

        Gizmos.color = GetOutlineColor();
        Gizmos.DrawWireCube(transform.position, transform.localScale * 1.1f);
    }
#endif
}