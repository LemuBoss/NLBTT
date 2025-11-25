using UnityEngine;

/// <summary>
/// Base class for complex event cards with choices and multiple outcomes
/// Now integrates with EventUIManager to show UI when triggered
/// </summary>
public abstract class ComplexEventCard : Card
{
    protected bool hasTriggered = false;

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

    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        
        if (!hasTriggered)
        {
            hasTriggered = true;
            
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
            
            // Find player reference if not already set
            if (player == null)
            {
                player = Object.FindFirstObjectByType<Player>();
            }
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

    // Helper method to get outcome text for UI display
    public string GetOutcomeText(bool isChoiceA, bool isSuccess)
    {
        if (isChoiceA)
            return isSuccess ? outcomeASuccessText : outcomeAFailureText;
        else
            return isSuccess ? outcomeBSuccessText : outcomeBFailureText;
    }
}