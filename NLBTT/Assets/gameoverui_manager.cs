using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Game Over and Victory UI windows
/// Displays appropriate messages and restart options
/// </summary>
public class GameOverUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    
    [Header("Game Over Panel Elements")]
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI gameOverMessageText;
    [SerializeField] private Button gameOverRestartButton;
    [SerializeField] private TextMeshProUGUI gameOverRestartButtonText;
    
    [Header("Victory Panel Elements")]
    [SerializeField] private TextMeshProUGUI victoryTitleText;
    [SerializeField] private TextMeshProUGUI victoryMessageText;
    [SerializeField] private Button victoryRestartButton;
    [SerializeField] private TextMeshProUGUI victoryRestartButtonText;
    
    private Player player;
    private bool isGameEnded = false;
    
    private void Awake()
    {
        // Hide both panels initially
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
        
        // Set up button listeners
        if (gameOverRestartButton != null)
            gameOverRestartButton.onClick.AddListener(OnRestartClicked);
        
        if (victoryRestartButton != null)
            victoryRestartButton.onClick.AddListener(OnRestartClicked);
        
        // Set default button text
        if (gameOverRestartButtonText != null)
            gameOverRestartButtonText.text = "Neustart";
        
        if (victoryRestartButtonText != null)
            victoryRestartButtonText.text = "Neustart";
        
        // Set default title texts
        if (gameOverTitleText != null)
            gameOverTitleText.text = "GAME OVER";
        
        if (victoryTitleText != null)
            victoryTitleText.text = "VICTORY!";
        
        // Find player reference
        player = Object.FindFirstObjectByType<Player>();
        if (player == null)
        {
            Debug.LogWarning("GameOverUIManager: Player not found in scene!");
        }
    }
    
    private void Update()
    {
        // Check for game end conditions if game hasn't ended yet
        if (!isGameEnded && player != null)
        {
            CheckGameEndConditions();
        }
    }
    
    /// <summary>
    /// Checks if the player has met win or loss conditions
    /// </summary>
    private void CheckGameEndConditions()
    {
        // Check for death (loss condition)
        if (player.isDead())
        {
            ShowGameOver("Du bist gestorben! Deine Gesundheit hat 0 erreicht.");
            return;
        }
        
        // Check for victory (win condition)
        // Note: You'll need to expose AltarRequirements and bloodpointsStoredInAltar
        // as public properties in the Player class for this to work
        if (player.GetBloodpointsInAltar() >= GetAltarRequirement())
        {
            ShowVictory($"Du hast genug Blutpunkte gesammelt! {player.GetBloodpointsInAltar()}/{GetAltarRequirement()} Blutpunkte im Altar.");
        }
    }
    
    /// <summary>
    /// Gets the altar requirement from the player
    /// You'll need to add a public getter in Player.cs: public int GetAltarRequirement() => AltarRequirements;
    /// </summary>
    private int GetAltarRequirement()
    {
        // For now, return a default value
        // You should add a public getter in Player.cs to expose AltarRequirements
        return 10; // Default value - replace with player.GetAltarRequirement() once implemented
    }
    
    /// <summary>
    /// Shows the Game Over window with a custom message
    /// </summary>
    public void ShowGameOver(string message)
    {
        if (isGameEnded)
            return; // Already showing an end screen
        
        isGameEnded = true;
        
        if (gameOverMessageText != null)
            gameOverMessageText.text = message;
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        
        Debug.Log($"[GameOverUIManager] Game Over: {message}");
    }
    
    /// <summary>
    /// Shows the Victory window with a custom message
    /// </summary>
    public void ShowVictory(string message)
    {
        if (isGameEnded)
            return; // Already showing an end screen
        
        isGameEnded = true;
        
        if (victoryMessageText != null)
            victoryMessageText.text = message;
        
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        
        Debug.Log($"[GameOverUIManager] Victory: {message}");
    }
    
    /// <summary>
    /// Called when the player clicks the Restart button
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("[GameOverUIManager] Restart button clicked");
        
        // Reset player resources
        ResetPlayerResources();
        
        // Hide both panels
        HideAllPanels();
        
        // Reset game ended flag
        isGameEnded = false;
        
        // TODO: Add additional restart logic here:
        // - Regenerate the board
        // - Reset player position
        // - Reset any other game state
        
        Debug.Log("[GameOverUIManager] Game restarted");
    }
    
    /// <summary>
    /// Resets the player's resources to their starting values
    /// </summary>
    private void ResetPlayerResources()
    {
        if (player == null)
        {
            Debug.LogError("GameOverUIManager: Cannot reset resources - Player is null!");
            return;
        }
        
        // Reset to starting values
        // You may want to make these configurable in the Inspector
        player.modifyHealth(5 - player.GetHealth()); // Set to 5
        player.modifyHunger(20 - player.GetHunger()); // Set to 20
        player.modifyStamina(5 - player.GetStamina()); // Set to 5
        player.modifyBloodpoints(-player.GetBloodpoints()); // Set to 0
        player.modifyBloodpointCardVisited(-player.GetBloodpointCardsVisited()); // Set to 0
        
        Debug.Log("[GameOverUIManager] Player resources reset to starting values");
    }
    
    /// <summary>
    /// Hides all game end UI panels
    /// </summary>
    public void HideAllPanels()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
        
        isGameEnded = false;
    }
    
    /// <summary>
    /// Returns whether any game end screen is currently showing
    /// Used by CardVisual and other systems to block interactions
    /// </summary>
    public bool IsShowingGameEnd()
    {
        return (gameOverPanel != null && gameOverPanel.activeSelf) ||
               (victoryPanel != null && victoryPanel.activeSelf);
    }
    
    /// <summary>
    /// Returns whether the game has ended (win or loss)
    /// </summary>
    public bool IsGameEnded()
    {
        return isGameEnded;
    }
    
    /// <summary>
    /// Static helper to check if game is paused/ended from anywhere
    /// </summary>
    public static bool IsGameEndedGlobal()
    {
        GameOverUIManager manager = Object.FindFirstObjectByType<GameOverUIManager>();
        return manager != null && manager.IsGameEnded();
    }
}