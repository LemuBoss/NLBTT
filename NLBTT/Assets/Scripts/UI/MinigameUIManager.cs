using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages the visual presentation and effects for the minigame UI
/// </summary>
public class MinigameUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Image backgroundClickDetector;
    
    [Header("Effect Elements")]
    [SerializeField] private Image penaltyBorderFlash;
    [SerializeField] private Image bonusBorderFlash;
    
    [Header("Effect Settings")]
    [SerializeField] private float borderFlashDuration = 0.3f;
    [SerializeField] private float borderFlashIntensity = 0.4f;
    [SerializeField] private Color penaltyColor = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField] private Color bonusColor = new Color(0f, 1f, 1f, 0.3f);
    
    [Header("References")]
    [SerializeField] private MinigameController controller;
    
    private Coroutine currentFlashCoroutine;
    
    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<MinigameController>();
        
        // Set up click detector
        if (backgroundClickDetector != null)
        {
            backgroundClickDetector.raycastTarget = true;
            
            // Add click event listener
            Button clickButton = backgroundClickDetector.gameObject.GetComponent<Button>();
            if (clickButton == null)
                clickButton = backgroundClickDetector.gameObject.AddComponent<Button>();
            
            clickButton.onClick.AddListener(OnBackgroundClicked);
        }
        
        // Initialize border flashes
        if (penaltyBorderFlash != null)
        {
            penaltyBorderFlash.color = new Color(penaltyColor.r, penaltyColor.g, penaltyColor.b, 0f);
        }
        
        if (bonusBorderFlash != null)
        {
            bonusBorderFlash.color = new Color(bonusColor.r, bonusColor.g, bonusColor.b, 0f);
        }
        
        // Hide panel initially
        if (minigamePanel != null)
            minigamePanel.SetActive(false);
    }
    
    /// <summary>
    /// Shows the minigame UI panel
    /// </summary>
    public void ShowMinigame()
    {
        if (minigamePanel != null)
        {
            minigamePanel.SetActive(true);
        }
        
        // Reset all effects
        if (penaltyBorderFlash != null)
            penaltyBorderFlash.color = new Color(penaltyColor.r, penaltyColor.g, penaltyColor.b, 0f);
        
        if (bonusBorderFlash != null)
            bonusBorderFlash.color = new Color(bonusColor.r, bonusColor.g, bonusColor.b, 0f);
    }
    
    /// <summary>
    /// Hides the minigame UI panel
    /// </summary>
    public void HideMinigame()
    {
        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Shows penalty visual effect (red border flash)
    /// </summary>
    public void ShowPenaltyEffect()
    {
        if (penaltyBorderFlash == null)
            return;
        
        // Stop any existing flash
        if (currentFlashCoroutine != null)
            StopCoroutine(currentFlashCoroutine);
        
        currentFlashCoroutine = StartCoroutine(BorderFlashEffect(penaltyBorderFlash, penaltyColor));
    }
    
    /// <summary>
    /// Shows bonus visual effect (cyan border flash)
    /// </summary>
    public void ShowBonusEffect()
    {
        if (bonusBorderFlash == null)
            return;
        
        // Stop any existing flash
        if (currentFlashCoroutine != null)
            StopCoroutine(currentFlashCoroutine);
        
        currentFlashCoroutine = StartCoroutine(BorderFlashEffect(bonusBorderFlash, bonusColor));
    }
    
    /// <summary>
    /// Border flash animation
    /// </summary>
    private IEnumerator BorderFlashEffect(Image borderImage, Color flashColor)
    {
        float elapsed = 0f;
        
        // Fade in
        while (elapsed < borderFlashDuration / 2f)
        {
            float t = elapsed / (borderFlashDuration / 2f);
            float alpha = Mathf.Lerp(0f, borderFlashIntensity, t);
            borderImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Fade out
        elapsed = 0f;
        while (elapsed < borderFlashDuration / 2f)
        {
            float t = elapsed / (borderFlashDuration / 2f);
            float alpha = Mathf.Lerp(borderFlashIntensity, 0f, t);
            borderImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        borderImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        currentFlashCoroutine = null;
    }
    
    /// <summary>
    /// Called when the background (not a circle) is clicked
    /// </summary>
    private void OnBackgroundClicked()
    {
        if (controller == null || !controller.IsActive())
            return;
        
        Vector2 clickPos = Input.mousePosition;
        
        // Check if click was actually on a circle
        if (!controller.IsClickOnAnyCircle(clickPos))
        {
            controller.OnMisclick(clickPos);
        }
    }
    
    /// <summary>
    /// Returns whether the minigame UI is currently visible
    /// </summary>
    public bool IsMinigameVisible()
    {
        return minigamePanel != null && minigamePanel.activeSelf;
    }
}

