using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class CardVisual : MonoBehaviour
{
    public static event Action<CardVisual> OnAnyCardFlipped;
    
    private Card cardLogic;
    private bool isAdjacentToPlayer = false;
    private bool isPlayerOnCard = false; // NEW: Track if player is standing on this card
    private BoardManager boardManager;
    private Player player;

    [Header("Outline Settings")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float outlineThickness = 1.05f;

    [Header("Outline Colors")]
    [SerializeField] private Color unturnedOutlineColor = Color.white;
    [SerializeField] private Color walkableOutlineColor = Color.green;
    [SerializeField] private Color unwalkableOutlineColor = Color.red;
    [SerializeField] private Color eventOutlineColor = Color.yellow; // NEW: Yellow for active events

    [Header("Flip Animation Settings")]
    [SerializeField] private float flipHeight = 0.3f;
    [SerializeField] private float flipDuration = 0.5f;
    [SerializeField] private AnimationCurve liftCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private MeshRenderer meshRenderer;
    private GameObject outlineObject;
    private bool isHovered = false;
    private bool isAnimating = false;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        CreateOutlineObject();
        boardManager = UnityEngine.Object.FindFirstObjectByType<BoardManager>();
        player = UnityEngine.Object.FindFirstObjectByType<Player>();
    }

    void Update()
    {
        // Update whether player is on this card
        if (player != null && cardLogic != null)
        {
            Vector2Int playerPos = player.GetPosition();
            Vector2Int cardPos = GetGridPosition();
            isPlayerOnCard = (playerPos == cardPos);
        }

        // Update outline based on player position
        UpdateOutline();

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
        if (IsAnyUIBlocking() || isAnimating)
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
        if (isAnimating)
        {
            Debug.Log("[CardVisual] Ignoring click - card is animating");
            return;
        }

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[CardVisual] Ignoring click - pointer is over UI element");
            return;
        }

        if (IsAnyUIBlocking())
        {
            Debug.Log("[CardVisual] Ignoring click - UI is blocking interaction");
            return;
        }

        if (!isAdjacentToPlayer || cardLogic == null)
            return;

        Vector2Int cardPosition = GetGridPosition();
        if (cardPosition.x < 0 || cardPosition.y < 0)
        {
            Debug.LogWarning($"Invalid card position for {gameObject.name}");
            return;
        }

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

    private bool IsAnyUIBlocking()
    {
        if (PauseMenuManager.IsGamePaused())
        {
            return true;
        }

        EventUIManager eventUI = UnityEngine.Object.FindFirstObjectByType<EventUIManager>();
        if (eventUI != null && eventUI.IsShowingEvent())
        {
            return true;
        }

        BloodpointUIManager bloodpointUI = UnityEngine.Object.FindFirstObjectByType<BloodpointUIManager>();
        if (bloodpointUI != null && bloodpointUI.IsShowingEvent())
        {
            return true;
        }

        GameOverUIManager gameOverUI = UnityEngine.Object.FindFirstObjectByType<GameOverUIManager>();
        if (gameOverUI != null && gameOverUI.IsShowingGameEnd())
        {
            return true;
        }

        return false;
    }

    public void SetAdjacentToPlayer(bool adjacent)
    {
        isAdjacentToPlayer = adjacent;
        UpdateOutline();
    }

    public void TurnCardOver()
    {
        if (cardLogic == null) return;
        if (!cardLogic.TurnedAround) return;

        transform.Rotate(180f, 0f, 0f);
        cardLogic.TurnOver();

        UpdateCardAppearance();
        UpdateOutline();
    }

    public IEnumerator FlipCardAnimation()
    {
        if (cardLogic == null || !cardLogic.TurnedAround)
        {
            yield break;
        }

        isAnimating = true;

        Vector3 startPosition = transform.position;
        Vector3 liftedPosition = startPosition + Vector3.up * flipHeight;

        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(180f, 0f, 0f);

        float elapsed = 0f;

        float liftTime = flipDuration * 0.25f;
        while (elapsed < liftTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / liftTime;
            float curveValue = liftCurve.Evaluate(t);
            
            transform.position = Vector3.Lerp(startPosition, liftedPosition, curveValue);
            
            yield return null;
        }

        elapsed = 0f;
        float rotateTime = flipDuration * 0.5f;
        while (elapsed < rotateTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateTime;
            
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            
            yield return null;
        }

        transform.rotation = endRotation;
        cardLogic.TurnOver();

        elapsed = 0f;
        float lowerTime = flipDuration * 0.25f;
        while (elapsed < lowerTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lowerTime;
            float curveValue = liftCurve.Evaluate(1f - t);
            
            transform.position = Vector3.Lerp(startPosition, liftedPosition, curveValue);
            
            yield return null;
        }

        transform.position = startPosition;

        UpdateCardAppearance();
        UpdateOutline();
        OnAnyCardFlipped?.Invoke(this);

        isAnimating = false;
    }

    private void UpdateCardAppearance()
    {
        UpdateOutline();
    }

    private void UpdateOutline()
    {
        if (outlineObject == null || cardLogic == null) return;

        if (IsAnyUIBlocking() || isAnimating)
        {
            outlineObject.SetActive(false);
            return;
        }

        // NEW: Show yellow outline if player is standing on card with active event
        if (isPlayerOnCard && cardLogic.HasActiveEvent)
        {
            outlineObject.SetActive(true);
            if (outlineObject.GetComponent<MeshRenderer>() != null)
            {
                outlineObject.GetComponent<MeshRenderer>().material.color = eventOutlineColor;
            }
            return;
        }

        // Show outline when hovering over adjacent card
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

    private Color GetOutlineColor()
    {
        if (cardLogic.TurnedAround)
        {
            return unturnedOutlineColor;
        }
        else if (cardLogic.CanMoveOnto)
        {
            return walkableOutlineColor;
        }
        else
        {
            // NEW: Check if it's a RockCard and player has ClimbingRope
            if (cardLogic is RockCard)
            {
                Player player = UnityEngine.Object.FindFirstObjectByType<Player>();
                if (player != null)
                {
                    ItemManager itemManager = player.GetItemManager();
                    if (itemManager != null && itemManager.HasItem(ItemManager.ItemType.ClimbingRope))
                    {
                        // Show green outline - player CAN climb
                        return walkableOutlineColor;
                    }
                }
            }
        
            // Default: unwalkable (red)
            return unwalkableOutlineColor;
        }
    }

    public void OnPlayerEnterCard()
    {
        if (cardLogic != null)
        {
            if (cardLogic.TurnedAround)
            {
                TurnCardOver();
            }

            cardLogic.OnPlayerEnter();
        }
    }

    private Vector2Int GetGridPosition()
    {
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

    public Card GetCardLogic()
    {
        return cardLogic;
    }

    public void SetCardLogic(Card logic)
    {
        cardLogic = logic;

        if (cardLogic != null && !cardLogic.TurnedAround)
        {
            transform.rotation = Quaternion.Euler(270f, 0f, 0f);
        }

        UpdateCardAppearance();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (cardLogic == null) return;

        Gizmos.color = GetOutlineColor();
        Gizmos.DrawWireCube(transform.position, transform.localScale * 1.1f);
    }
#endif
}

