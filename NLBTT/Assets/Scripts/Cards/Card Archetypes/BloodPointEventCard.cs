using UnityEngine;

/// <summary>
/// Base class for blood point event cards
/// Now uses manual triggering via spacebar and integrates with event icon system
/// </summary>
public abstract class BloodPointEventCard : Card
{
    protected Player player;
    protected BloodpointUIManager uiManager;
    
    // Event information - set these in child class constructors
    protected string eventTitle = "Bloodpoint Event";
    protected string eventResultText = "";

    public BloodPointEventCard()
    {
        // CRITICAL: Set hasEvent to true for all BloodPointEventCards
        hasEvent = true;
    }

    /// <summary>
    /// MODIFIED: No longer automatically triggers the event
    /// Just calls base for stamina and wolf encounter checks
    /// </summary>
    public override void OnPlayerEnter()
    {
        // Apply stamina restoration from base Card class
        base.OnPlayerEnter();
        
        // Get player reference if we don't have it yet
        if (player == null)
        {
            player = UnityEngine.Object.FindFirstObjectByType<Player>();
        }
        
        // Get UI manager reference if we don't have it yet
        if (uiManager == null)
        {
            uiManager = UnityEngine.Object.FindFirstObjectByType<BloodpointUIManager>();
        }

        // Log that player is on a bloodpoint event card (for debugging)
        if (hasEvent && !isEventClosed)
        {
            Debug.Log($"[{this.GetType().Name}] Player entered bloodpoint card. Press SPACEBAR to trigger event.");
        }
        else if (isEventClosed)
        {
            Debug.Log($"[{this.GetType().Name}] Event already triggered, no action available");
        }
    }

    /// <summary>
    /// NEW: Manual event trigger via spacebar
    /// Triggers the bloodpoint event and shows the UI
    /// </summary>
    public override void TriggerEvent()
    {
        // Don't trigger if event is already closed
        if (isEventClosed)
        {
            Debug.Log($"[{this.GetType().Name}] Event already triggered, cannot trigger again");
            return;
        }
        
        Debug.Log($"[{this.GetType().Name}] Bloodpoint event manually triggered via spacebar");
        
        if (player != null)
        {
            // Trigger the event and get the result text
            TriggerBloodPointEvent();
            player.modifyBloodpointCardVisited(1);
            
            // Store this card as the last visited bloodpoint card
            player.SetLastBloodPointCardVisited(this);
            
            // Close the event so it can't be triggered again
            CloseEvent();
            
            // Notify EventIconManager that this event is closed
            NotifyEventClosed();
            
            // Show UI popup with the result
            if (uiManager != null)
            {
                uiManager.ShowBloodpointEvent(eventTitle, eventResultText);
            }
            else
            {
                Debug.LogWarning("BloodpointUIManager not found in scene!");
            }
            
            Debug.Log($"[BloodPointEventCard] Event triggered and closed on {this.GetType().Name}");
        }
        else
        {
            Debug.LogError("BloodPointEventCard: Cannot trigger event - Player reference is null!");
        }
    }

    /// <summary>
    /// Notifies the EventIconManager that this event has been closed
    /// </summary>
    private void NotifyEventClosed()
    {
        EventIconManager iconManager = UnityEngine.Object.FindFirstObjectByType<EventIconManager>();
        if (iconManager != null)
        {
            // Find which position this card is at
            BoardManager boardManager = UnityEngine.Object.FindFirstObjectByType<BoardManager>();
            if (boardManager != null)
            {
                Vector2Int cardPosition = FindThisCardPosition(boardManager);
                if (cardPosition.x >= 0)
                {
                    iconManager.OnEventClosed(cardPosition);
                }
            }
        }
    }

    /// <summary>
    /// Finds the grid position of this card on the board
    /// </summary>
    private Vector2Int FindThisCardPosition(BoardManager boardManager)
    {
        int width = boardManager.GetGridWidth();
        int height = boardManager.GetGridHeight();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Card card = boardManager.GetCardAt(x, y);
                if (card == this)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    // Override this in specific card implementations
    // Use this method to set the eventResultText based on the outcome
    public abstract void TriggerBloodPointEvent();
    
    /// <summary>
    /// Helper method for child classes to set the result text
    /// </summary>
    protected void SetResultText(string text)
    {
        eventResultText = text;
    }
}
