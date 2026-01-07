using UnityEngine;

/// <summary>
/// Base class for all cards in the game
/// By default, all cards restore 1 stamina when entered (representing rest)
/// Subclasses can override staminaModifier to change this behavior
/// </summary>
public abstract class Card
{
    protected string title;
    protected bool canMoveOnto = true;
    protected bool blocksLineOfSight = false;
    protected bool turnedAround = true;
    protected bool allowedForShuffle = true;

    protected int staminaModifier = 1; // Default: +1 stamina (rest)

    // Event tracking
    protected bool hasEvent = false;
    protected bool isEventClosed = false;

    public string Title => title;
    public bool CanMoveOnto => canMoveOnto;
    public bool BlocksLineOfSight => blocksLineOfSight;
    public bool TurnedAround => turnedAround;
    public bool HasEvent => hasEvent;
    public bool IsEventClosed => isEventClosed;

    // Track which entities are on this card
    private bool hasPlayer = false;
    private Wolf wolfOnCard = null;

    // NEW: Property to check if card has an active event
    public bool HasActiveEvent => hasEvent && !isEventClosed;

    // Called when player moves onto this card
    public virtual void OnPlayerEnter()
    {
        hasPlayer = true;

        // Apply stamina modifier
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null && staminaModifier != 0)
        {
            player.modifyStamina(staminaModifier);

            if (staminaModifier > 0)
            {
                Debug.Log($"[{this.GetType().Name}] Player rested, gained {staminaModifier} stamina. Current: {player.GetStamina()}/{player.GetStaminaCap()}");
            }
            else
            {
                Debug.Log($"[{this.GetType().Name}] Difficult terrain, lost {Mathf.Abs(staminaModifier)} stamina. Current: {player.GetStamina()}/{player.GetStaminaCap()}");
            }
        }

        // Check if wolf is already here
        CheckForWolfPlayerEncounter();

        // REMOVED: Automatic event triggering
        // Events are now triggered manually via spacebar in Player.cs
    }

    // Called when player leaves this card
    public virtual void OnPlayerExit()
    {
        hasPlayer = false;
    }

    // NEW: Method to trigger event manually (called by spacebar press)
    public virtual void TriggerEvent()
    {
        // Override this in subclasses that have events
        // Base implementation does nothing
        Debug.Log($"[{this.GetType().Name}] TriggerEvent called, but no event implementation");
    }

    // Called when wolf moves onto this card
    public virtual void OnWolfEnter(Wolf wolf)
    {
        wolfOnCard = wolf;

        // Check if player is already here
        CheckForWolfPlayerEncounter();
    }

    // Called when wolf leaves this card
    public virtual void OnWolfExit(Wolf wolf)
    {
        if (wolfOnCard == wolf)
        {
            wolfOnCard = null;
        }
    }

    /// <summary>
    /// Checks if both wolf and player are on this card, triggers encounter if so
    /// </summary>
    private void CheckForWolfPlayerEncounter()
    {
        if (hasPlayer && wolfOnCard != null)
        {
            Debug.Log($"[{this.GetType().Name}] Wolf and Player encounter detected!");
            TriggerWolfEncounter();
        }
    }

    /// <summary>
    /// Triggers a wolf encounter event
    /// Creates and displays a WolfCard event
    /// </summary>
    private void TriggerWolfEncounter()
    {
        WolfCard wolfCard = new WolfCard();

        // Queue the wolf event through the UI system
        if (UIQueueManager.Instance != null)
        {
            UIQueueManager.Instance.QueueComplexEvent(wolfCard);
            Debug.Log("[Card] Wolf encounter queued through UIQueueManager");
        }
        else
        {
            // Fallback: trigger directly if UIQueueManager not available
            Debug.LogWarning("[Card] UIQueueManager not found, triggering wolf card directly");
            wolfCard.OnPlayerEnter();
        }
    }

    public virtual void TurnOver()
    {
        turnedAround = false;
    }

    /// <summary>
    /// Closes the event on this card, preventing it from being retriggered
    /// Should be called by subclasses when appropriate
    /// </summary>
    protected void CloseEvent()
    {
        if (hasEvent)
        {
            isEventClosed = true;
            Debug.Log($"[{this.GetType().Name}] Event has been closed");
        }
    }

    /// <summary>
    /// Reopens a closed event, allowing it to be triggered again
    /// Useful for special cases or debugging
    /// </summary>
    public void ReopenEvent()
    {
        if (hasEvent)
        {
            isEventClosed = false;
            Debug.Log($"[{this.GetType().Name}] Event has been reopened");
        }
    }

    // Public getters for checking occupancy
    public bool HasPlayer() => hasPlayer;
    public bool HasWolf() => wolfOnCard != null;
    public Wolf GetWolfOnCard() => wolfOnCard;
}
