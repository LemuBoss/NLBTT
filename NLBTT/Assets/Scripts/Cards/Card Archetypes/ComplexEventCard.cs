using UnityEngine;

/// <summary>
/// Base class for complex event cards with choices and multiple outcomes
/// Now supports optional minigame integration
/// Integrates with EventUIManager to show UI when manually triggered via spacebar
/// Respects the isEventClosed flag - won't trigger if event is closed
/// FIX: Sets lastOutcomeText AFTER callbacks to allow dynamic text updates
/// </summary>
public abstract class ComplexEventCard : Card
{
    // Event text
    protected string eventTitle;
    protected string eventDescription;
    
    // Choice texts
    protected string choiceAText;
    protected string choiceBText;
    
    // CHOICE C: Optional third choice (e.g. for items like Bunny Statue)
    protected string choiceCText = null; // null = no third choice available
    protected string outcomeCText = "";
    
    // Outcome texts
    protected string outcomeASuccessText;
    protected string outcomeAFailureText;
    protected string outcomeBSuccessText;
    protected string outcomeBFailureText;
    
    // Probabilities (0.0 to 1.0) - only used if no minigame is configured
    protected float choiceASuccessProbability = 0.5f;
    protected float choiceBSuccessProbability = 0.5f;
    
    // Minigame configuration (null = use random roll)
    protected MinigameConfig choiceAMinigameConfig = null;
    protected MinigameConfig choiceBMinigameConfig = null;
    
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

    /// <summary>
    /// Called by UI when player selects Choice A
    /// Determines if minigame or random roll should be used
    /// </summary>
    public void SelectChoiceA()
    {
        if (choiceAMinigameConfig != null)
        {
            // Use minigame to determine outcome
            StartMinigameForChoice(true);
        }
        else
        {
            // Use random roll to determine outcome
            float roll = Random.value;
            bool isSuccess = roll <= choiceASuccessProbability;
            
            ExecuteChoiceAOutcome(isSuccess);
        }
    }

    /// <summary>
    /// Called by UI when player selects Choice B
    /// Determines if minigame or random roll should be used
    /// </summary>
    public void SelectChoiceB()
    {
        if (choiceBMinigameConfig != null)
        {
            // Use minigame to determine outcome
            StartMinigameForChoice(false);
        }
        else
        {
            // Use random roll to determine outcome
            float roll = Random.value;
            bool isSuccess = roll <= choiceBSuccessProbability;
            
            ExecuteChoiceBOutcome(isSuccess);
        }
    }
    
    /// <summary>
    /// Returns the text for Choice C button (empty if not available)
    /// </summary>
    public virtual string GetChoiceCText()
    {
        return choiceCText ?? "";
    }

    /// <summary>
    /// Returns true if this event has a third choice available
    /// </summary>
    public virtual bool HasChoiceC()
    {
        return !string.IsNullOrEmpty(choiceCText);
    }

    /// <summary>
    /// Selects Choice C and triggers its outcome
    /// Choice C never uses minigames - instant effect only
    /// </summary>
    public virtual void SelectChoiceC()
    {
        Debug.Log($"{GetType().Name} - Choice C selected: {choiceCText}");
    
        // Execute the outcome first
        OnChoiceC();
        
        // THEN set the outcome text (allows dynamic updates in OnChoiceC)
        lastOutcomeText = outcomeCText;
    
        // Close the event
        CloseEvent();
    }

    /// <summary>
    /// Override this in subclasses to define what happens when Choice C is selected
    /// </summary>
    protected virtual void OnChoiceC()
    {
        // Default: do nothing
        Debug.Log($"{GetType().Name} - Choice C executed");
    }

    /// <summary>
    /// Starts the minigame for the selected choice
    /// </summary>
    private void StartMinigameForChoice(bool isChoiceA)
    {
        MinigameController minigameController = Object.FindFirstObjectByType<MinigameController>();
        
        if (minigameController == null)
        {
            Debug.LogError("ComplexEventCard: MinigameController not found! Falling back to random roll.");
            
            // Fallback to random roll
            if (isChoiceA)
            {
                float roll = Random.value;
                ExecuteChoiceAOutcome(roll <= choiceASuccessProbability);
            }
            else
            {
                float roll = Random.value;
                ExecuteChoiceBOutcome(roll <= choiceBSuccessProbability);
            }
            return;
        }
        
        // Get modifiers from player state
        MinigameModifiers modifiers = MinigameModifiers.FromPlayerState(player);
        
        // Get the appropriate config
        MinigameConfig config = isChoiceA ? choiceAMinigameConfig : choiceBMinigameConfig;
        
        // Start minigame with callbacks
        minigameController.StartMinigame(
            config,
            modifiers,
            () => {
                // Success callback
                if (isChoiceA)
                    ExecuteChoiceAOutcome(true);
                else
                    ExecuteChoiceBOutcome(true);
                
                // Show outcome after minigame completes
                ShowOutcomeAfterMinigame();
            },
            () => {
                // Failure callback
                if (isChoiceA)
                    ExecuteChoiceAOutcome(false);
                else
                    ExecuteChoiceBOutcome(false);
                
                // Show outcome after minigame completes
                ShowOutcomeAfterMinigame();
            }
        );
    }

    /// <summary>
    /// Shows the outcome panel after minigame completes
    /// </summary>
    private void ShowOutcomeAfterMinigame()
    {
        EventUIManager uiManager = Object.FindFirstObjectByType<EventUIManager>();
        if (uiManager != null)
        {
            uiManager.ShowOutcomeAfterMinigame(lastOutcomeText);
        }
    }

    /// <summary>
    /// Executes Choice A outcome and stores outcome text
    /// FIX: Now sets lastOutcomeText AFTER callback to allow dynamic updates
    /// </summary>
    private void ExecuteChoiceAOutcome(bool isSuccess)
    {
        // Execute the appropriate outcome FIRST
        if (isSuccess)
        {
            OnChoiceASuccess();
        }
        else
        {
            OnChoiceAFailure();
        }
        
        // THEN store the outcome text for UI (allows subclasses to modify it)
        lastOutcomeText = isSuccess ? outcomeASuccessText : outcomeAFailureText;

        // Let subclasses decide if this choice closes the event
        HandleChoiceAEventClosure(isSuccess);
    }

    /// <summary>
    /// Executes Choice B outcome and stores outcome text
    /// FIX: Now sets lastOutcomeText AFTER callback to allow dynamic updates
    /// </summary>
    private void ExecuteChoiceBOutcome(bool isSuccess)
    {
        // Execute the appropriate outcome FIRST
        if (isSuccess)
        {
            OnChoiceBSuccess();
        }
        else
        {
            OnChoiceBFailure();
        }
        
        // THEN store the outcome text for UI (allows subclasses to modify it)
        lastOutcomeText = isSuccess ? outcomeBSuccessText : outcomeBFailureText;

        // Let subclasses decide if this choice closes the event
        HandleChoiceBEventClosure(isSuccess);
    }

    // Getters for UI
    public string GetEventTitle() => eventTitle;
    public string GetEventDescription() => eventDescription;
    public string GetChoiceAText() => choiceAText;
    public string GetChoiceBText() => choiceBText;
    public string GetLastOutcomeText() => lastOutcomeText;
    
    
    
    // Check if choices use minigames
    public bool ChoiceAUsesMinigame() => choiceAMinigameConfig != null;
    public bool ChoiceBUsesMinigame() => choiceBMinigameConfig != null;

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

