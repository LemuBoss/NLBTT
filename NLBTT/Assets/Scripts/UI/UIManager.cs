using UnityEngine;
using TMPro;

/// <summary>
/// Manages all UI display for the player's resources and Altar interactions
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ResourceBarDisplay resourceDisplay; // Changed from ResourceCircleDisplay
    
    [Header("HUD Root")]
    [SerializeField] private GameObject resourceHUDRoot;
    
    [Header("Bloodpoints Display")]
    [SerializeField] private TextMeshProUGUI bloodpointsText;
    
    [Header("Altar Interaction")]
    [SerializeField] private TextMeshProUGUI altarPromptText;
    [SerializeField] private TextMeshProUGUI bloodpointsInAltarText;
    
    private void Start()
    {
        // Auto-find components
        if (player == null)
            player = Object.FindFirstObjectByType<Player>();
        if (boardManager == null)
            boardManager = Object.FindFirstObjectByType<BoardManager>();
        if (resourceDisplay == null)
            resourceDisplay = GetComponent<ResourceBarDisplay>(); // Changed from ResourceCircleDisplay
        
        // Validate references
        if (player == null)
            Debug.LogError("UIManager: Player not found!");
        if (boardManager == null)
            Debug.LogError("UIManager: BoardManager not found!");
        if (resourceDisplay == null)
            Debug.LogError("UIManager: ResourceBarDisplay not found! Add component to UIManager."); // Changed error message
        
        // Validate HUD Root
        if (resourceHUDRoot == null)
            Debug.LogError("UIManager: ResourceHUDRoot not assigned! Please assign the HUD root GameObject in the Inspector.");
        
        // Hide altar prompt initially
        if (altarPromptText != null)
            altarPromptText.gameObject.SetActive(false);
        
        // Show HUD at start
        ShowHUD();
        
        // Initialize displays
        if (player != null && resourceDisplay != null)
        {
            resourceDisplay.UpdateHealth(player.GetHealth());
            resourceDisplay.UpdateHunger(player.GetHunger(), player.GetHungerCap());
        }
    }
    
    private void Update()
    {
        bool isGamePaused = PauseMenuManager.IsGamePaused();
        
        // Handle HUD visibility based on pause state
        if (isGamePaused)
        {
            HideHUD();
        }
        else
        {
            // Only show HUD when unpaused
            ShowHUD();
            UpdateResourceDisplay();
            UpdateAltarDisplay();
        }
    }
    
    /// <summary>
    /// Updates the resource display
    /// </summary>
    private void UpdateResourceDisplay()
    {
        if (player == null || resourceDisplay == null)
            return;
        
        // Update visual bar displays
        resourceDisplay.UpdateHealth(player.GetHealth());
        resourceDisplay.UpdateHunger(player.GetHunger(), player.GetHungerCap());
        
        // Update bloodpoints text
        if (bloodpointsText != null)
            bloodpointsText.text = $"{player.GetBloodpoints()}";
    }
    
    /// <summary>
    /// Updates Altar display
    /// </summary>
    private void UpdateAltarDisplay()
    {
        if (player == null || boardManager == null)
            return;
        
        // Check if player is on Altar
        Card currentCard = boardManager.GetCardAt(player.GetPosition().x, player.GetPosition().y);
        bool isOnAltar = currentCard != null && currentCard is AltarCard;
        
        // Show/hide altar prompt
        if (altarPromptText != null)
        {
            if (isOnAltar)
            {
                altarPromptText.gameObject.SetActive(true);
                altarPromptText.text = "ENTER UM BLUTPUNKTE ZU TRANSFERIEREN";
            }
            else
            {
                altarPromptText.gameObject.SetActive(false);
            }
        }
        
        // Update bloodpoints in altar
        if (bloodpointsInAltarText != null)
            bloodpointsInAltarText.text = $"BLUTPUNKTE IM ALTAR: {player.GetBloodpointsInAltar()}/{player.GetAltarRequirement()}";
    }
    
    /// <summary>
    /// Hides the entire resource HUD
    /// </summary>
    public void HideHUD()
    {
        if (resourceHUDRoot != null)
        {
            if (resourceHUDRoot.activeSelf)
            {
                resourceHUDRoot.SetActive(false);
                Debug.Log("[UIManager] HUD hidden");
            }
        }
        else
        {
            Debug.LogWarning("[UIManager] Cannot hide HUD - resourceHUDRoot is null!");
        }
    }
    
    /// <summary>
    /// Shows the resource HUD
    /// </summary>
    public void ShowHUD()
    {
        if (resourceHUDRoot != null)
        {
            if (!resourceHUDRoot.activeSelf)
            {
                resourceHUDRoot.SetActive(true);
                Debug.Log("[UIManager] HUD shown");
            }
        }
        else
        {
            Debug.LogWarning("[UIManager] Cannot show HUD - resourceHUDRoot is null!");
        }
    }
    
    /// <summary>
    /// Resets the UI display
    /// </summary>
    public void ResetDisplay()
    {
        if (resourceDisplay != null)
        {
            resourceDisplay.ResetDisplay();
        }
        
        if (player != null && resourceDisplay != null)
        {
            resourceDisplay.UpdateHealth(player.GetHealth());
            resourceDisplay.UpdateHunger(player.GetHunger(), player.GetHungerCap());
        }
    }
}

