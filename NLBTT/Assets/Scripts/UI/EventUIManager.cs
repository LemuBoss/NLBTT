using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the UI windows for ComplexEventCard interactions
/// Displays event choices and outcomes to the player
/// Supports minigame integration and optional third choice
/// </summary>
public class EventUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject eventChoicePanel;
    [SerializeField] private GameObject eventOutcomePanel;
    
    [Header("Choice Panel Elements")]
    [SerializeField] private TextMeshProUGUI eventTitleText;
    [SerializeField] private TextMeshProUGUI eventDescriptionText;
    [SerializeField] private Button choiceAButton;
    [SerializeField] private Button choiceBButton;
    [SerializeField] private Button choiceCButton; // NEW: Optional third choice
    [SerializeField] private TextMeshProUGUI choiceAButtonText;
    [SerializeField] private TextMeshProUGUI choiceBButtonText;
    [SerializeField] private TextMeshProUGUI choiceCButtonText; // NEW
    
    [Header("Outcome Panel Elements")]
    [SerializeField] private TextMeshProUGUI outcomeText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;
    
    private ComplexEventCard currentEventCard;
    private bool waitingForChoice = false;
    private bool waitingForMinigame = false;
    
    private void Awake()
    {
        // Hide both panels initially
        if (eventChoicePanel != null)
            eventChoicePanel.SetActive(false);
        
        if (eventOutcomePanel != null)
            eventOutcomePanel.SetActive(false);
        
        // Set up button listeners
        if (choiceAButton != null)
            choiceAButton.onClick.AddListener(OnChoiceAClicked);
        
        if (choiceBButton != null)
            choiceBButton.onClick.AddListener(OnChoiceBClicked);
        
        if (choiceCButton != null)
        {
            choiceCButton.onClick.AddListener(OnChoiceCClicked);
            choiceCButton.gameObject.SetActive(false); // Hidden by default
        }
        
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        // Set default button text
        if (continueButtonText != null)
            continueButtonText.text = "Weiter";
    }
    
    /// <summary>
    /// Shows the event choice window with the given ComplexEventCard's information
    /// Automatically shows/hides third choice button based on availability
    /// </summary>
    public void ShowEventChoice(ComplexEventCard eventCard)
    {
        if (eventCard == null)
        {
            Debug.LogError("EventUIManager: Cannot show event - eventCard is null!");
            return;
        }
        
        currentEventCard = eventCard;
        waitingForChoice = true;
        waitingForMinigame = false;
        
        // Populate the choice panel with event information
        if (eventTitleText != null)
            eventTitleText.text = eventCard.GetEventTitle();
        
        if (eventDescriptionText != null)
            eventDescriptionText.text = eventCard.GetEventDescription();
        
        if (choiceAButtonText != null)
            choiceAButtonText.text = eventCard.GetChoiceAText();
        
        if (choiceBButtonText != null)
            choiceBButtonText.text = eventCard.GetChoiceBText();
        
        // NEW: Handle optional third choice
        if (choiceCButton != null)
        {
            string choiceCText = eventCard.GetChoiceCText();
            bool hasChoiceC = !string.IsNullOrEmpty(choiceCText);
            
            choiceCButton.gameObject.SetActive(hasChoiceC);
            
            if (hasChoiceC && choiceCButtonText != null)
            {
                choiceCButtonText.text = choiceCText;
            }
            
            Debug.Log($"[EventUIManager] Choice C available: {hasChoiceC}");
        }
        
        // Show the choice panel
        if (eventChoicePanel != null)
            eventChoicePanel.SetActive(true);
        
        // Make sure outcome panel is hidden
        if (eventOutcomePanel != null)
            eventOutcomePanel.SetActive(false);
        
        Debug.Log($"[EventUIManager] Showing event choice: {eventCard.GetEventTitle()}");
    }
    
    /// <summary>
    /// Called when the player clicks Choice A button
    /// </summary>
    private void OnChoiceAClicked()
    {
        if (!waitingForChoice || currentEventCard == null)
            return;
        
        Debug.Log("[EventUIManager] Player selected Choice A");
        
        // Hide choice panel
        if (eventChoicePanel != null)
            eventChoicePanel.SetActive(false);
        
        waitingForChoice = false;
        
        // Check if this choice uses a minigame
        if (currentEventCard.ChoiceAUsesMinigame())
        {
            // Minigame will handle showing outcome
            waitingForMinigame = true;
            currentEventCard.SelectChoiceA();
        }
        else
        {
            // No minigame - immediate outcome
            currentEventCard.SelectChoiceA();
            string outcomeMessage = currentEventCard.GetLastOutcomeText();
            ShowOutcome(outcomeMessage);
        }
    }
    
    /// <summary>
    /// Called when the player clicks Choice B button
    /// </summary>
    private void OnChoiceBClicked()
    {
        if (!waitingForChoice || currentEventCard == null)
            return;
        
        Debug.Log("[EventUIManager] Player selected Choice B");
        
        // Hide choice panel
        if (eventChoicePanel != null)
            eventChoicePanel.SetActive(false);
        
        waitingForChoice = false;
        
        // Check if this choice uses a minigame
        if (currentEventCard.ChoiceBUsesMinigame())
        {
            // Minigame will handle showing outcome
            waitingForMinigame = true;
            currentEventCard.SelectChoiceB();
        }
        else
        {
            // No minigame - immediate outcome
            currentEventCard.SelectChoiceB();
            string outcomeMessage = currentEventCard.GetLastOutcomeText();
            ShowOutcome(outcomeMessage);
        }
    }
    
    /// <summary>
    /// NEW: Called when the player clicks Choice C button (optional third choice)
    /// </summary>
    private void OnChoiceCClicked()
    {
        if (!waitingForChoice || currentEventCard == null)
            return;
        
        Debug.Log("[EventUIManager] Player selected Choice C");
        
        // Hide choice panel
        if (eventChoicePanel != null)
            eventChoicePanel.SetActive(false);
        
        waitingForChoice = false;
        
        // Choice C never uses minigames (instant effect)
        currentEventCard.SelectChoiceC();
        string outcomeMessage = currentEventCard.GetLastOutcomeText();
        ShowOutcome(outcomeMessage);
    }
    
    /// <summary>
    /// Shows the outcome window with the result text
    /// Called directly for non-minigame choices
    /// </summary>
    private void ShowOutcome(string outcomeMessage)
    {
        if (outcomeText != null)
            outcomeText.text = outcomeMessage;
        
        if (eventOutcomePanel != null)
            eventOutcomePanel.SetActive(true);
        
        Debug.Log($"[EventUIManager] Showing outcome: {outcomeMessage}");
    }
    
    /// <summary>
    /// Shows the outcome window after a minigame completes
    /// Called by ComplexEventCard after minigame success/failure
    /// </summary>
    public void ShowOutcomeAfterMinigame(string outcomeMessage)
    {
        if (!waitingForMinigame)
        {
            Debug.LogWarning("EventUIManager: ShowOutcomeAfterMinigame called but not waiting for minigame!");
            return;
        }
        
        waitingForMinigame = false;
        ShowOutcome(outcomeMessage);
    }
    
    /// <summary>
    /// Called when the player clicks the Continue button
    /// </summary>
    private void OnContinueClicked()
    {
        Debug.Log("[EventUIManager] Continue button clicked");
        
        // Hide outcome panel
        if (eventOutcomePanel != null)
            eventOutcomePanel.SetActive(false);
        
        // Clear current event
        currentEventCard = null;
        
        // Resume game
        ResumeGame();
    }
    
    /// <summary>
    /// Hides all event UI panels
    /// </summary>
    public void HideAllPanels()
    {
        if (eventChoicePanel != null)
            eventChoicePanel.SetActive(false);
        
        if (eventOutcomePanel != null)
            eventOutcomePanel.SetActive(false);
        
        if (choiceCButton != null)
            choiceCButton.gameObject.SetActive(false);
        
        currentEventCard = null;
        waitingForChoice = false;
        waitingForMinigame = false;
    }
    
    /// <summary>
    /// Returns whether the UI is currently showing an event
    /// </summary>
    public bool IsShowingEvent()
    {
        return (eventChoicePanel != null && eventChoicePanel.activeSelf) ||
               (eventOutcomePanel != null && eventOutcomePanel.activeSelf) ||
               waitingForMinigame;
    }
    
    /// <summary>
    /// Called when continuing from an outcome - resume game logic
    /// </summary>
    private void ResumeGame()
    {
        Debug.Log("[EventUIManager] Game resumed after event");
    }
}

