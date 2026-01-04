using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI indicator that shows when wolves are actively tracking the player's scent
/// Displays an icon with a pulsing glow animation
/// </summary>
public class WolfTrackingIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image trackingIcon;
    [SerializeField] private WolfAI wolfAI;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float glowPeakDuration = 0.3f;
    [SerializeField] private float fadeToIdleDuration = 0.4f;
    [SerializeField] private float peakAlpha = 1.0f;
    [SerializeField] private float idleAlpha = 0.7f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool isVisible = false;
    private bool isAnimating = false;
    private Coroutine currentAnimation = null;
    private int trackingWolvesCount = 0;

    private void Awake()
    {
        // Find WolfAI if not assigned
        if (wolfAI == null)
        {
            wolfAI = Object.FindFirstObjectByType<WolfAI>();
            if (wolfAI == null)
            {
                Debug.LogError("[WolfTrackingIndicator] WolfAI not found in scene!");
            }
        }
        
        // Validate icon reference
        if (trackingIcon == null)
        {
            Debug.LogError("[WolfTrackingIndicator] Tracking icon Image not assigned!");
            return;
        }
        
        // Start invisible
        SetIconAlpha(0f);
        trackingIcon.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Don't update visibility when game is paused
        if (PauseMenuManager.IsGamePaused())
        {
            return;
        }
        
        UpdateTrackingStatus();
    }

    /// <summary>
    /// Checks if any wolves are tracking and updates the indicator accordingly
    /// </summary>
    private void UpdateTrackingStatus()
    {
        if (wolfAI == null) return;
        
        int currentTrackingCount = CountTrackingWolves();
        
        // Check if tracking status changed
        if (currentTrackingCount > 0 && !isVisible)
        {
            // Wolves started tracking - show indicator with animation
            LogDebug($"{currentTrackingCount} wolf(ves) started tracking player");
            ShowIndicator();
        }
        else if (currentTrackingCount == 0 && isVisible)
        {
            // All wolves stopped tracking - hide indicator
            LogDebug("All wolves stopped tracking player");
            HideIndicator();
        }
        else if (currentTrackingCount != trackingWolvesCount && isVisible)
        {
            // Number of tracking wolves changed but still tracking
            LogDebug($"Tracking wolves count changed: {trackingWolvesCount} -> {currentTrackingCount}");
        }
        
        trackingWolvesCount = currentTrackingCount;
    }

    /// <summary>
    /// Counts how many wolves are currently tracking the player
    /// </summary>
    private int CountTrackingWolves()
    {
        if (wolfAI == null) return 0;
        
        int count = 0;
        var wolves = wolfAI.GetWolves();
        
        foreach (Wolf wolf in wolves)
        {
            if (wolf != null && wolf.IsTrackingScent())
            {
                count++;
            }
        }
        
        return count;
    }

    /// <summary>
    /// Shows the tracking indicator with glow animation
    /// </summary>
    private void ShowIndicator()
    {
        if (trackingIcon == null) return;
        
        // Stop any current animation
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        trackingIcon.gameObject.SetActive(true);
        isVisible = true;
        
        // Start glow animation (only if not already at idle state)
        currentAnimation = StartCoroutine(GlowAnimation());
    }

    /// <summary>
    /// Hides the tracking indicator with fade out
    /// </summary>
    private void HideIndicator()
    {
        if (trackingIcon == null) return;
        
        // Stop any current animation
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        isVisible = false;
        
        // Start fade out animation
        currentAnimation = StartCoroutine(FadeOutAnimation());
    }

    /// <summary>
    /// Glow animation: fade in -> peak -> settle to idle brightness
    /// </summary>
    private IEnumerator GlowAnimation()
    {
        isAnimating = true;
        
        // Phase 1: Fade in
        float elapsed = 0f;
        float startAlpha = trackingIcon.color.a;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time so animation works during pause
            float t = elapsed / fadeInDuration;
            float alpha = Mathf.Lerp(startAlpha, peakAlpha, t);
            SetIconAlpha(alpha);
            yield return null;
        }
        
        SetIconAlpha(peakAlpha);
        
        // Phase 2: Hold at peak
        yield return new WaitForSecondsRealtime(glowPeakDuration);
        
        // Phase 3: Fade to idle brightness
        elapsed = 0f;
        while (elapsed < fadeToIdleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeToIdleDuration;
            float alpha = Mathf.Lerp(peakAlpha, idleAlpha, t);
            SetIconAlpha(alpha);
            yield return null;
        }
        
        SetIconAlpha(idleAlpha);
        
        isAnimating = false;
        currentAnimation = null;
    }

    /// <summary>
    /// Fade out animation when tracking stops
    /// </summary>
    private IEnumerator FadeOutAnimation()
    {
        isAnimating = true;
        
        float elapsed = 0f;
        float startAlpha = trackingIcon.color.a;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeOutDuration;
            float alpha = Mathf.Lerp(startAlpha, 0f, t);
            SetIconAlpha(alpha);
            yield return null;
        }
        
        SetIconAlpha(0f);
        trackingIcon.gameObject.SetActive(false);
        
        isAnimating = false;
        currentAnimation = null;
    }

    /// <summary>
    /// Sets the alpha value of the tracking icon
    /// </summary>
    private void SetIconAlpha(float alpha)
    {
        if (trackingIcon == null) return;
        
        Color color = trackingIcon.color;
        color.a = alpha;
        trackingIcon.color = color;
    }

    /// <summary>
    /// Force update the tracking status (useful after board regeneration)
    /// </summary>
    public void ForceUpdateStatus()
    {
        trackingWolvesCount = 0; // Reset count to force update
        UpdateTrackingStatus();
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[WolfTrackingIndicator] {message}");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Clamp alpha values
        peakAlpha = Mathf.Clamp01(peakAlpha);
        idleAlpha = Mathf.Clamp01(idleAlpha);
        
        // Ensure durations are positive
        fadeInDuration = Mathf.Max(0.1f, fadeInDuration);
        glowPeakDuration = Mathf.Max(0f, glowPeakDuration);
        fadeToIdleDuration = Mathf.Max(0.1f, fadeToIdleDuration);
        fadeOutDuration = Mathf.Max(0.1f, fadeOutDuration);
    }
#endif
}