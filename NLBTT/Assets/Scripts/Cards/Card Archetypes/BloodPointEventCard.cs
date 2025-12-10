using UnityEngine;

/// <summary>
/// Base class for blood point event cards
/// </summary>
public abstract class BloodPointEventCard : Card
{
    protected bool hasTriggered = false;
    protected Player player;
    protected BloodpointUIManager uiManager;
    
    // Event information - set these in child class constructors
    protected string eventTitle = "Bloodpoint Event";
    protected string eventResultText = "";

    public override void OnPlayerEnter()
    {
        // Apply stamina restoration from base Card class
        base.OnPlayerEnter();
        
        // Get player reference if we don't have it yet
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
        }
        
        // Get UI manager reference if we don't have it yet
        if (uiManager == null)
        {
            uiManager = Object.FindFirstObjectByType<BloodpointUIManager>();
        }

        if (!hasTriggered)
        {
            if (player != null)
            {
                // Trigger the event and get the result text
                TriggerBloodPointEvent();
                player.modifyBloodpointCardVisited(1);
                
                // Store this card as the last visited bloodpoint card
                player.SetLastBloodPointCardVisited(this);
                
                hasTriggered = true;
                
                // Show UI popup with the result
                if (uiManager != null)
                {
                    uiManager.ShowBloodpointEvent(eventTitle, eventResultText);
                }
                else
                {
                    Debug.LogWarning("BloodpointUIManager not found in scene!");
                }
                
                Debug.Log($"[BloodPointEventCard] Triggered event on {this.GetType().Name}");
            }
            else
            {
                Debug.LogError("BloodPointEventCard: Cannot trigger event - Player reference is null!");
            }
        }
        else
        {
            Debug.Log($"[BloodPointEventCard] {this.GetType().Name} already triggered, ignoring");
        }
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