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

    public string Title => title;
    public bool CanMoveOnto => canMoveOnto;
    public bool BlocksLineOfSight => blocksLineOfSight;
    public bool TurnedAround => turnedAround;

    // Track which entities are on this card
    private bool hasPlayer = false;
    private Wolf wolfOnCard = null;

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
    }

    // Called when player leaves this card
    public virtual void OnPlayerExit()
    {
        hasPlayer = false;
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

    // Public getters for checking occupancy
    public bool HasPlayer() => hasPlayer;
    public bool HasWolf() => wolfOnCard != null;
    public Wolf GetWolfOnCard() => wolfOnCard;
}