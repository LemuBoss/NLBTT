using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the UI popup for BloodPointEventCard interactions
/// Displays bloodpoint event results to the player
/// </summary>
public class BloodpointUIManager : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject bloodpointPanel;
    
    [Header("Panel Elements")]
    [SerializeField] private TextMeshProUGUI eventTitleText;
    [SerializeField] private TextMeshProUGUI eventResultText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;
    
    private void Awake()
    {
        // Hide panel initially
        if (bloodpointPanel != null)
            bloodpointPanel.SetActive(false);
        
        // Set up button listener
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        // Set default button text
        if (continueButtonText != null)
            continueButtonText.text = "Weiter";
    }
    
    /// <summary>
    /// Shows the bloodpoint event result popup
    /// </summary>
    /// <param name="title">Title of the bloodpoint event</param>
    /// <param name="resultText">Description of what happened</param>
    public void ShowBloodpointEvent(string title, string resultText)
    {
        if (bloodpointPanel == null)
        {
            Debug.LogError("BloodpointUIManager: bloodpointPanel is not assigned!");
            return;
        }
        
        // Populate the panel with event information
        if (eventTitleText != null)
            eventTitleText.text = title;
        
        if (eventResultText != null)
            eventResultText.text = resultText;
        
        // Show the panel
        bloodpointPanel.SetActive(true);
        
        Debug.Log($"[BloodpointUIManager] Showing bloodpoint event: {title}");
    }
    
    /// <summary>
    /// Called when the player clicks the Continue button
    /// </summary>
    private void OnContinueClicked()
    {
        Debug.Log("[BloodpointUIManager] Continue button clicked");
        
        // Hide panel
        if (bloodpointPanel != null)
            bloodpointPanel.SetActive(false);
    }
    
    /// <summary>
    /// Hides the bloodpoint panel
    /// </summary>
    public void HidePanel()
    {
        if (bloodpointPanel != null)
            bloodpointPanel.SetActive(false);
    }
    
    /// <summary>
    /// Returns whether the UI is currently showing a bloodpoint event
    /// </summary>
    public bool IsShowingEvent()
    {
        return bloodpointPanel != null && bloodpointPanel.activeSelf;
    }
}