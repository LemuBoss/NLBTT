using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the pause menu with cinematic bars and fade-in text
/// Press ESC to pause/unpause
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("Pause Menu Panels")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private Image overlayImage;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private CanvasGroup textCanvasGroup;
    [SerializeField] private TextMeshProUGUI pauseText;
    [SerializeField] private Button resumeButton;
    
    [Header("Animation Settings")]
    [SerializeField] private float barSlideDistance = 150f; // How far the bars slide in
    [SerializeField] private float barAnimationDuration = 0.5f;
    [SerializeField] private float textFadeDuration = 0.8f;
    [SerializeField] private float textFadeDelay = 0.3f; // Delay before text starts fading
    [SerializeField] private AnimationCurve barAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve textFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Pause Text")]
    [TextArea(5, 10)]
    [SerializeField] private string pauseTextContent = @"SPIEL PAUSIERT

<size=18>Steuerung:</size>
Klicke auf benachbarte Karten um dich zu bewegen
[1] - Hunger in Blutpunkte umwandeln
[2] - Gesundheit in Blutpunkte umwandeln
[ENTER] - Blutpunkte am Altar deponieren
[ESC] - Spiel pausieren/fortsetzen

<size=18>Ziel:</size>
Sammle Blutpunkte und überlebe die Reise durch den Wald.
Terrain zieht dir Ausdauer ab. Bewegungen kosten Nahrung. Speziellle Karten verleiehn dir Blutpunkte.
Verwalte deine Ressourcen weise!";
    
    private bool isPaused = false;
    private bool isAnimating = false;
    
    // Store initial positions for animation
    private Vector2 topBarStartPos;
    private Vector2 bottomBarStartPos;
    private Vector2 topBarEndPos;
    private Vector2 bottomBarEndPos;
    
    private void Awake()
    {
        // Set up button listener
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        
        // Set pause text
        if (pauseText != null)
            pauseText.text = pauseTextContent;
        
        // Initialize positions
        if (topBar != null)
        {
            topBarStartPos = topBar.anchoredPosition;
            topBarEndPos = topBarStartPos + Vector2.down * barSlideDistance;
        }
        
        if (bottomBar != null)
        {
            bottomBarStartPos = bottomBar.anchoredPosition;
            bottomBarEndPos = bottomBarStartPos + Vector2.up * barSlideDistance;
        }
        
        // Hide pause menu initially
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
    }
    
    private void Update()
    {
        // Toggle pause with ESC key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isAnimating)
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }
    }
    
    /// <summary>
    /// Pauses the game and shows the pause menu with animations
    /// </summary>
    public void PauseGame()
    {
        if (isPaused || isAnimating)
            return;
        
        isPaused = true;
        Time.timeScale = 0f; // Pause game
        
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);
        
        // Start animations
        StartCoroutine(AnimatePauseMenu(true));
        
        Debug.Log("[PauseMenu] Game paused");
    }
    
    /// <summary>
    /// Resumes the game and hides the pause menu with animations
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused || isAnimating)
            return;
        
        isPaused = false;
        
        // Start exit animations
        StartCoroutine(AnimatePauseMenu(false));
        
        Debug.Log("[PauseMenu] Game resumed");
    }
    
    /// <summary>
    /// Animates the pause menu opening or closing
    /// </summary>
    private IEnumerator AnimatePauseMenu(bool opening)
    {
        isAnimating = true;
        
        if (opening)
        {
            // Reset positions and alpha for opening animation
            if (topBar != null)
                topBar.anchoredPosition = topBarStartPos;
            
            if (bottomBar != null)
                bottomBar.anchoredPosition = bottomBarStartPos;
            
            if (textCanvasGroup != null)
                textCanvasGroup.alpha = 0f;
            
            if (overlayImage != null)
            {
                Color overlayColor = overlayImage.color;
                overlayColor.a = 0f;
                overlayImage.color = overlayColor;
            }
            
            // Fade in overlay
            yield return StartCoroutine(FadeOverlay(true));
            
            // Slide in bars
            yield return StartCoroutine(AnimateBars(true));
            
            // Wait a bit, then fade in text
            yield return new WaitForSecondsRealtime(textFadeDelay);
            yield return StartCoroutine(FadeText(true));
        }
        else
        {
            // Fade out text first
            yield return StartCoroutine(FadeText(false));
            
            // Slide out bars
            yield return StartCoroutine(AnimateBars(false));
            
            // Fade out overlay
            yield return StartCoroutine(FadeOverlay(false));
            
            // Hide the menu
            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(false);
            
            // Resume game time
            Time.timeScale = 1f;
        }
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Animates the cinematic bars sliding in or out
    /// </summary>
    private IEnumerator AnimateBars(bool slideIn)
    {
        float elapsed = 0f;
        
        while (elapsed < barAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / barAnimationDuration);
            float curveValue = barAnimationCurve.Evaluate(t);
            
            if (slideIn)
            {
                // Slide bars in
                if (topBar != null)
                    topBar.anchoredPosition = Vector2.Lerp(topBarStartPos, topBarEndPos, curveValue);
                
                if (bottomBar != null)
                    bottomBar.anchoredPosition = Vector2.Lerp(bottomBarStartPos, bottomBarEndPos, curveValue);
            }
            else
            {
                // Slide bars out
                if (topBar != null)
                    topBar.anchoredPosition = Vector2.Lerp(topBarEndPos, topBarStartPos, curveValue);
                
                if (bottomBar != null)
                    bottomBar.anchoredPosition = Vector2.Lerp(bottomBarEndPos, bottomBarStartPos, curveValue);
            }
            
            yield return null;
        }
        
        // Ensure final position is exact
        if (slideIn)
        {
            if (topBar != null)
                topBar.anchoredPosition = topBarEndPos;
            if (bottomBar != null)
                bottomBar.anchoredPosition = bottomBarEndPos;
        }
        else
        {
            if (topBar != null)
                topBar.anchoredPosition = topBarStartPos;
            if (bottomBar != null)
                bottomBar.anchoredPosition = bottomBarStartPos;
        }
    }
    
    /// <summary>
    /// Fades the text in or out
    /// </summary>
    private IEnumerator FadeText(bool fadeIn)
    {
        if (textCanvasGroup == null)
            yield break;
        
        float elapsed = 0f;
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        
        while (elapsed < textFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / textFadeDuration);
            float curveValue = textFadeCurve.Evaluate(t);
            
            textCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            
            yield return null;
        }
        
        textCanvasGroup.alpha = endAlpha;
    }
    
    /// <summary>
    /// Fades the overlay in or out
    /// </summary>
    private IEnumerator FadeOverlay(bool fadeIn)
    {
        if (overlayImage == null)
            yield break;
        
        float elapsed = 0f;
        float duration = 0.3f;
        float startAlpha = fadeIn ? 0f : 0.7f;
        float endAlpha = fadeIn ? 0.7f : 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            Color color = overlayImage.color;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            overlayImage.color = color;
            
            yield return null;
        }
        
        Color finalColor = overlayImage.color;
        finalColor.a = endAlpha;
        overlayImage.color = finalColor;
    }
    
    /// <summary>
    /// Returns whether the game is currently paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    /// <summary>
    /// Static accessor for easy checking from other scripts
    /// </summary>
    public static bool IsGamePaused()
    {
        PauseMenuManager instance = FindFirstObjectByType<PauseMenuManager>();
        return instance != null && instance.isPaused;
    }
}

