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
    
    [Header("Resource Display")]
    [SerializeField] private GameObject resourceHUDRoot; // Parent object containing all HUD elements
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI bloodpointsText;
    
    [Header("Altar Interaction")]
    [SerializeField] private TextMeshProUGUI altarPromptText;
    [SerializeField] private TextMeshProUGUI bloodpointsInAltarText;
    
    private bool wasGamePausedLastFrame = false;
    
    private void Start()
    {
        // Auto-find Player and BoardManager if not assigned
        if (player == null)
            player = Object.FindFirstObjectByType<Player>();
        if (boardManager == null)
            boardManager = Object.FindFirstObjectByType<BoardManager>();
        
        if (player == null)
            Debug.LogError("UIManager: Player not found!");
        if (boardManager == null)
            Debug.LogError("UIManager: BoardManager not found!");
        
        // Hide altar prompt initially
        if (altarPromptText != null)
            altarPromptText.gameObject.SetActive(false);
        
        // Ensure HUD is visible at start
        ShowHUD();
    }
    
    private void Update()
    {
        bool isGamePaused = PauseMenuManager.IsGamePaused();
        
        // Detect when game transitions from paused to unpaused
        if (wasGamePausedLastFrame && !isGamePaused)
        {
            // Just unpaused - force show HUD
            ShowHUD();
            Debug.Log("[UIManager] Game unpaused - showing HUD");
        }
        
        // Hide HUD when game is paused
        if (isGamePaused)
        {
            HideHUD();
        }
        else
        {
            // Show HUD and update when not paused
            ShowHUD();
            UpdateResourceDisplay();
            UpdateAltarDisplay();
        }
        
        wasGamePausedLastFrame = isGamePaused;
    }
    
    /// <summary>
    /// Updates the resource counter display
    /// </summary>
    private void UpdateResourceDisplay()
    {
        if (player == null)
            return;
        
        if (hungerText != null)
            hungerText.text = $"NAHRUNG: {player.GetHunger()}/{player.GetHungerCap()} [2]";
        
        if (staminaText != null)
            staminaText.text = $"AUSDAUER: {player.GetStamina()}/{player.GetStaminaCap()}";
        
        if (healthText != null)
            healthText.text = $"GESUNDHEIT: {player.GetHealth()} [1]";
        
        if (bloodpointsText != null)
            bloodpointsText.text = $"BLUTPUNKTE: {player.GetBloodpoints()}";
    }
    
    /// <summary>
    /// Updates Altar display and shows prompt only when player is on Altar
    /// </summary>
    private void UpdateAltarDisplay()
    {
        if (player == null || boardManager == null)
            return;
        
        // Check if player is standing on an Altar
        Card currentCard = boardManager.GetCardAt(player.GetPosition().x, player.GetPosition().y);
        bool isOnAltar = currentCard != null && currentCard is AltarCard;
        
        // Show/hide altar prompt based on position
        if (altarPromptText != null)
        {
            if (isOnAltar)
            {
                altarPromptText.gameObject.SetActive(true);
                altarPromptText.text = $"ENTER UM BLUTPUNKE ZU TRANSFERIEREN";
            }
            else
            {
                altarPromptText.gameObject.SetActive(false);
            }
        }
        
        // Update bloodpoints stored in altar
        if (bloodpointsInAltarText != null)
            bloodpointsInAltarText.text = $"BLUTPUNKTE IM ALTAR: {player.GetBloodpointsInAltar()}/{player.GetAltarRequirement()}";
    }
    
    /// <summary>
    /// Hides the entire resource HUD
    /// </summary>
    public void HideHUD()
    {
        if (resourceHUDRoot != null && resourceHUDRoot.activeSelf)
        {
            resourceHUDRoot.SetActive(false);
        }
    }
    
    /// <summary>
    /// Shows the resource HUD
    /// </summary>
    public void ShowHUD()
    {
        if (resourceHUDRoot != null && !resourceHUDRoot.activeSelf)
        {
            resourceHUDRoot.SetActive(true);
        }
    }
}
