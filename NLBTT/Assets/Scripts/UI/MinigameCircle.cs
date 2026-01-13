using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Individual circle behavior - handles timer, interactions, and visual feedback
/// </summary>
public class MinigameCircle : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private Image timerFillImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Colors")]
    [SerializeField] private Color realCircleColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color fakeCircleColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color penaltyFlashColor = Color.red;
    [SerializeField] private Color bonusFlashColor = Color.cyan;
    
    [Header("Animation Settings")]
    [SerializeField] private float swellScale = 1.2f;
    [SerializeField] private float swellDuration = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    // State
    private CircleType circleType;
    private float currentTime;
    private float maxTime;
    private float radius;
    private MinigameController controller;
    private RectTransform rectTransform;
    private bool isExpired = false;
    
    // Drift
    private bool driftEnabled = false;
    private float driftSpeed;
    private Vector2 driftDirection;
    private float driftChangeTimer;
    private const float DRIFT_DIRECTION_CHANGE_INTERVAL = 1.5f;
    
    // Fake circle hover tracking
    private bool hasTriggeredPenalty = false;
    private bool wasMouseInBounds = false;
    
    public CircleType Type => circleType;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (timerFillImage == null)
            Debug.LogError("MinigameCircle: Timer fill image not assigned!");
        
        if (outlineImage == null)
            Debug.LogError("MinigameCircle: Outline image not assigned!");
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }
    
    /// <summary>
    /// Initializes the circle with its configuration
    /// </summary>
    public void Initialize(CircleType type, float maxTime, float radius, MinigameController controller, 
        bool enableDrift, float driftSpeed)
    {
        this.circleType = type;
        this.maxTime = maxTime;
        this.currentTime = maxTime;
        this.radius = radius;
        this.controller = controller;
        this.driftEnabled = enableDrift;
        this.driftSpeed = driftSpeed;
        
        // Set visual appearance based on type
        if (circleType == CircleType.Real)
        {
            if (timerFillImage != null)
            {
                timerFillImage.color = realCircleColor;
                timerFillImage.fillAmount = 1f;
                timerFillImage.gameObject.SetActive(true);
            }
            
            if (outlineImage != null)
                outlineImage.color = realCircleColor;
        }
        else // Fake
        {
            if (timerFillImage != null)
                timerFillImage.gameObject.SetActive(false);
            
            if (outlineImage != null)
                outlineImage.color = fakeCircleColor;
        }
        
        // Initialize drift
        if (driftEnabled)
        {
            driftDirection = Random.insideUnitCircle.normalized;
            driftChangeTimer = DRIFT_DIRECTION_CHANGE_INTERVAL;
        }
    }
    
    private void Update()
    {
        if (isExpired)
            return;
        
        // Update timer for real circles
        if (circleType == CircleType.Real)
        {
            UpdateTimer();
        }
        
        // Handle drift movement
        if (driftEnabled)
        {
            UpdateDrift();
        }
        
        // Handle mouse interaction
        HandleMouseInteraction();
    }
    
    /// <summary>
    /// Updates the circle's timer
    /// </summary>
    private void UpdateTimer()
    {
        if (controller == null)
            return;
        
        float drainSpeed = controller.GetDrainSpeedMultiplier();
        
        // Apply drift penalty if drifting
        if (driftEnabled)
            drainSpeed *= controller.GetDriftTimerPenalty();
        
        currentTime -= Time.deltaTime * drainSpeed;
        
        // Update fill amount
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = Mathf.Clamp01(currentTime / maxTime);
            
            // Change color as time runs low
            float timePercent = currentTime / maxTime;
            if (timePercent < 0.25f)
            {
                timerFillImage.color = Color.Lerp(Color.red, realCircleColor, timePercent * 4f);
            }
        }
        
        // Check if timer expired
        if (currentTime <= 0f && !isExpired)
        {
            OnTimerExpired();
        }
    }
    
    /// <summary>
    /// Updates drift movement
    /// </summary>
    private void UpdateDrift()
    {
        // Change direction periodically
        driftChangeTimer -= Time.deltaTime;
        if (driftChangeTimer <= 0f)
        {
            driftDirection = Random.insideUnitCircle.normalized;
            driftChangeTimer = DRIFT_DIRECTION_CHANGE_INTERVAL;
        }
        
        // Move in drift direction
        Vector2 movement = driftDirection * driftSpeed * Time.deltaTime;
        rectTransform.anchoredPosition += movement;
        
        // Keep within parent bounds
        ClampPositionToBounds();
    }
    
    /// <summary>
    /// Clamps circle position to stay within parent container bounds
    /// </summary>
    private void ClampPositionToBounds()
    {
        if (rectTransform.parent is RectTransform parentRect)
        {
            Rect parentBounds = parentRect.rect;
            Vector2 pos = rectTransform.anchoredPosition;
            
            float halfSize = radius;
            pos.x = Mathf.Clamp(pos.x, parentBounds.xMin + halfSize, parentBounds.xMax - halfSize);
            pos.y = Mathf.Clamp(pos.y, parentBounds.yMin + halfSize, parentBounds.yMax - halfSize);
            
            rectTransform.anchoredPosition = pos;
        }
    }
    
    /// <summary>
    /// Handles mouse clicks and hover detection
    /// </summary>
    private void HandleMouseInteraction()
    {
        Vector2 mousePos = Input.mousePosition;
        bool isMouseInBounds = IsPositionInBounds(mousePos);
        
        // Handle click on real circles
        if (circleType == CircleType.Real && Input.GetMouseButtonDown(0) && isMouseInBounds)
        {
            OnCircleClicked();
        }
        
        // Handle hover on fake circles
        if (circleType == CircleType.Fake)
        {
            // Mouse just entered bounds
            if (isMouseInBounds && !wasMouseInBounds)
            {
                if (!hasTriggeredPenalty)
                {
                    hasTriggeredPenalty = true;
                    OnFakeCircleTouched();
                }
            }
            
            // Mouse left bounds - reset penalty flag
            if (!isMouseInBounds && wasMouseInBounds)
            {
                hasTriggeredPenalty = false;
            }
            
            wasMouseInBounds = isMouseInBounds;
        }
    }
    
    /// <summary>
    /// Checks if a screen position is within this circle's bounds
    /// </summary>
    public bool IsPositionInBounds(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenPosition,
            null,
            out Vector2 localPoint
        );
        
        return localPoint.magnitude <= radius;
    }
    
    /// <summary>
    /// Called when this circle is clicked (real circles only)
    /// </summary>
    private void OnCircleClicked()
    {
        if (isExpired)
            return;
            
        isExpired = true; // Prevent double-triggering
        
        if (controller != null)
        {
            controller.OnCircleClicked(this);
        }
        
        // Fade out and destroy
        StartCoroutine(FadeOutAndDestroy());
    }
    
    /// <summary>
    /// Called when this circle's timer expires
    /// </summary>
    private void OnTimerExpired()
    {
        if (isExpired)
            return;
            
        isExpired = true; // Prevent double-triggering
        
        if (controller != null)
        {
            controller.OnCircleTimerExpired(this);
        }
        
        // Visual feedback then destroy
        StartCoroutine(ExpireAnimation());
    }
    
    /// <summary>
    /// Called when a fake circle is touched by the mouse
    /// </summary>
    private void OnFakeCircleTouched()
    {
        if (controller != null)
        {
            controller.OnFakeCircleTouched(this);
        }
        
        // Visual feedback
        StartCoroutine(PulseEffect(penaltyFlashColor));
    }
    
    /// <summary>
    /// Applies a time penalty (percentage of remaining time)
    /// </summary>
    public void ApplyTimePenalty(float penaltyPercent)
    {
        if (circleType != CircleType.Real)
            return;
        
        float penaltyAmount = currentTime * penaltyPercent;
        currentTime = Mathf.Max(0f, currentTime - penaltyAmount);
        
        // Visual feedback
        StartCoroutine(SwellPulseEffect(penaltyFlashColor));
    }
    
    /// <summary>
    /// Adds bonus time to this circle
    /// </summary>
    public void AddBonusTime(float bonusTime)
    {
        if (circleType != CircleType.Real)
            return;
        
        currentTime = Mathf.Min(maxTime, currentTime + bonusTime);
        
        // Visual feedback
        StartCoroutine(SwellPulseEffect(bonusFlashColor));
    }
    
    /// <summary>
    /// Swell and flash effect for penalties/bonuses
    /// </summary>
    private IEnumerator SwellPulseEffect(Color flashColor)
    {
        Vector3 originalScale = transform.localScale;
        Color originalColor = timerFillImage != null ? timerFillImage.color : Color.white;
        
        float elapsed = 0f;
        
        // Swell up
        while (elapsed < swellDuration / 2f)
        {
            float t = elapsed / (swellDuration / 2f);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * swellScale, t);
            
            if (timerFillImage != null)
                timerFillImage.color = Color.Lerp(originalColor, flashColor, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Shrink back
        elapsed = 0f;
        while (elapsed < swellDuration / 2f)
        {
            float t = elapsed / (swellDuration / 2f);
            transform.localScale = Vector3.Lerp(originalScale * swellScale, originalScale, t);
            
            if (timerFillImage != null)
                timerFillImage.color = Color.Lerp(flashColor, originalColor, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
        if (timerFillImage != null)
            timerFillImage.color = originalColor;
    }
    
    /// <summary>
    /// Simple pulse effect for fake circles
    /// </summary>
    private IEnumerator PulseEffect(Color flashColor)
    {
        Color originalColor = outlineImage != null ? outlineImage.color : Color.white;
        
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            if (outlineImage != null)
                outlineImage.color = Color.Lerp(flashColor, originalColor, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (outlineImage != null)
            outlineImage.color = originalColor;
    }
    
    /// <summary>
    /// Fade out animation when circle is successfully clicked
    /// </summary>
    private IEnumerator FadeOutAndDestroy()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration && this != null)
        {
            float t = elapsed / fadeOutDuration;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }
    
    /// <summary>
    /// Expire animation when timer runs out
    /// </summary>
    private IEnumerator ExpireAnimation()
    {
        // Flash red and fade out
        float elapsed = 0f;
        float duration = 0.4f;
        
        while (elapsed < duration && this != null)
        {
            float t = elapsed / duration;
            
            if (timerFillImage != null)
                timerFillImage.color = Color.Lerp(Color.red, Color.clear, t);
            
            if (outlineImage != null)
                outlineImage.color = Color.Lerp(Color.red, Color.clear, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        // Safety cleanup - stop all coroutines when destroyed
        StopAllCoroutines();
    }
}

