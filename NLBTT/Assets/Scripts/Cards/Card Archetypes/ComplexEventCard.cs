using UnityEngine;

/// <summary>
/// Base class for complex event cards with choices and multiple outcomes
/// Now integrates with EventUIManager to show UI when manually triggered via spacebar
/// Respects the isEventClosed flag - won't trigger if event is closed
/// </summary>
public abstract class ComplexEventCard : Card
{
    // Event text
    protected string eventTitle;
    protected string eventDescription;
    
    // Choice texts
    protected string choiceAText;
    protected string choiceBText;
    
    // Outcome texts
    protected string outcomeASuccessText;
    protected string outcomeAFailureText;
    protected string outcomeBSuccessText;
    protected string outcomeBFailureText;
    
    // Probabilities (0.0 to 1.0)
    protected float choiceASuccessProbability = 0.5f;
    protected float choiceBSuccessProbability = 0.5f;
    
    // Reference to player for applying effects
    protected Player player;
    
    // Track the last outcome for UI display
    private string lastOutcomeText = "";

    public ComplexEventCard()
    {
        // Set hasEvent to true for all ComplexEventCards
        hasEvent = true;
    }

    /// <summary>
    /// MODIFIED: No longer automatically triggers the event
    /// Just calls base for stamina and wolf encounter checks
    /// </summary>
    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        
        // Find player reference if not already set
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
        }
        
        // Log that player is on an event card (for debugging)
        if (hasEvent && !isEventClosed)
        {
            Debug.Log($"[{this.GetType().Name}] Player entered event card. Press SPACEBAR to trigger event.");
        }
        else if (isEventClosed)
        {
            Debug.Log($"[{this.GetType().Name}] Event is closed, no action available");
        }
    }

    /// <summary>
    /// NEW: Manual event trigger via spacebar
    /// Shows the event UI when player presses spacebar while standing on this card
    /// </summary>
    public override void TriggerEvent()
    {
        // Don't trigger if event is already closed
        if (isEventClosed)
        {
            Debug.Log($"[{this.GetType().Name}] Event is closed, cannot trigger again");
            return;
        }
        
        Debug.Log($"[{this.GetType().Name}] Event manually triggered via spacebar");
        
        // Find the EventUIManager in the scene
        EventUIManager uiManager = Object.FindFirstObjectByType<EventUIManager>();
        
        if (uiManager != null)
        {
            // Show the event choice UI
            uiManager.ShowEventChoice(this);
        }
        else
        {
            Debug.LogError("ComplexEventCard: EventUIManager not found in scene!");
        }
    }

    // Called by UI when player selects Choice A
    public void SelectChoiceA()
    {
        float roll = Random.value;
        bool isSuccess = roll <= choiceASuccessProbability;
        
        // Store the outcome text for UI
        lastOutcomeText = isSuccess ? outcomeASuccessText : outcomeAFailureText;
        
        // Execute the appropriate outcome
        if (isSuccess)
        {
            OnChoiceASuccess();
        }
        else
        {
            OnChoiceAFailure();
        }

        // Let subclasses decide if this choice closes the event
        HandleChoiceAEventClosure(isSuccess);
    }

    // Called by UI when player selects Choice B
    public void SelectChoiceB()
    {
        float roll = Random.value;
        bool isSuccess = roll <= choiceBSuccessProbability;
        
        // Store the outcome text for UI
        lastOutcomeText = isSuccess ? outcomeBSuccessText : outcomeBFailureText;
        
        // Execute the appropriate outcome
        if (isSuccess)
        {
            OnChoiceBSuccess();
        }
        else
        {
            OnChoiceBFailure();
        }

        // Let subclasses decide if this choice closes the event
        HandleChoiceBEventClosure(isSuccess);
    }

    // Getters for UI
    public string GetEventTitle() => eventTitle;
    public string GetEventDescription() => eventDescription;
    public string GetChoiceAText() => choiceAText;
    public string GetChoiceBText() => choiceBText;
    public string GetLastOutcomeText() => lastOutcomeText;

    // Override these in specific card implementations
    protected abstract void OnChoiceASuccess();
    protected abstract void OnChoiceAFailure();
    protected abstract void OnChoiceBSuccess();
    protected abstract void OnChoiceBFailure();

    // Override these to control when events close (default: never close automatically)
    protected virtual void HandleChoiceAEventClosure(bool wasSuccess)
    {
        // By default, don't close the event
        // Subclasses can override to close based on success/failure
    }

    protected virtual void HandleChoiceBEventClosure(bool wasSuccess)
    {
        // By default, don't close the event
        // Subclasses can override to close based on success/failure
    }

    // Helper method to get outcome text for UI display
    public string GetOutcomeText(bool isChoiceA, bool isSuccess)
    {
        if (isChoiceA)
            return isSuccess ? outcomeASuccessText : outcomeAFailureText;
        else
            return isSuccess ? outcomeBSuccessText : outcomeBFailureText;
    }
}

