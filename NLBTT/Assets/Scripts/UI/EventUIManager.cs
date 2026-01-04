using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the UI windows for ComplexEventCard interactions
/// Displays event choices and outcomes to the player
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
    [SerializeField] private TextMeshProUGUI choiceAButtonText;
    [SerializeField] private TextMeshProUGUI choiceBButtonText;
    
    [Header("Outcome Panel Elements")]
    [SerializeField] private TextMeshProUGUI outcomeText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;
    
    private ComplexEventCard currentEventCard;
    private bool waitingForChoice = false;
    
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
        
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        // Set default button text
        if (continueButtonText != null)
            continueButtonText.text = "Weiter";
    }
    
    /// <summary>
    /// Shows the event choice window with the given ComplexEventCard's information
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
        
        // Populate the choice panel with event information
        if (eventTitleText != null)
            eventTitleText.text = eventCard.GetEventTitle();
        
        if (eventDescriptionText != null)
            eventDescriptionText.text = eventCard.GetEventDescription();
        
        if (choiceAButtonText != null)
            choiceAButtonText.text = eventCard.GetChoiceAText();
        
        if (choiceBButtonText != null)
            choiceBButtonText.text = eventCard.GetChoiceBText();
        
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
        
        // Call the card's SelectChoiceA - it handles the roll internally
        currentEventCard.SelectChoiceA();
        
        // Get the outcome text and show it
        // We need to determine what happened to show the right text
        string outcomeMessage = currentEventCard.GetLastOutcomeText();
        ShowOutcome(outcomeMessage);
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
        
        // Call the card's SelectChoiceB - it handles the roll internally
        currentEventCard.SelectChoiceB();
        
        // Get the outcome text and show it
        string outcomeMessage = currentEventCard.GetLastOutcomeText();
        ShowOutcome(outcomeMessage);
    }
    
    /// <summary>
    /// Shows the outcome window with the result text
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
        
        // Resume game (you may want to notify other systems here)
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
        
        currentEventCard = null;
        waitingForChoice = false;
    }
    
    /// <summary>
    /// Returns whether the UI is currently showing an event
    /// </summary>
    public bool IsShowingEvent()
    {
        return (eventChoicePanel != null && eventChoicePanel.activeSelf) ||
               (eventOutcomePanel != null && eventOutcomePanel.activeSelf);
    }
    
    /// <summary>
    /// Called when continuing from an outcome - resume game logic
    /// Override or extend this to add your own game resume logic
    /// </summary>
    private void ResumeGame()
    {
        // Add any game resume logic here
        // For example: re-enable player movement, unpause time, etc.
        Debug.Log("[EventUIManager] Game resumed after event");
    }
}