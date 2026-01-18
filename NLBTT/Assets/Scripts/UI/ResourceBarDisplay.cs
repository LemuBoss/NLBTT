using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages visual display of health and hunger using progress bars
/// Bars empty from RIGHT to LEFT with numerical value display
/// </summary>
public class ResourceBarDisplay : MonoBehaviour
{
    [Header("Health Display")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarBackground;
    [SerializeField] private TextMeshProUGUI healthValueText;
    [SerializeField] private Color healthColor = new Color(0.8f, 0.1f, 0.1f); // Red
    [SerializeField] private Color healthBackgroundColor = new Color(0.3f, 0.05f, 0.05f); // Dark red
    [SerializeField] private Image healthIcon;
    
    [Header("Hunger Display")]
    [SerializeField] private Image hungerBarFill;
    [SerializeField] private Image hungerBarBackground;
    [SerializeField] private TextMeshProUGUI hungerValueText;
    [SerializeField] private Color hungerColor = new Color(0.9f, 0.8f, 0.2f); // Yellow
    [SerializeField] private Color hungerBackgroundColor = new Color(0.3f, 0.25f, 0.05f); // Dark yellow
    [SerializeField] private Image hungerIcon;
    
    [Header("Bar Settings")]
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinScale = 0.95f;
    [SerializeField] private float pulseMaxScale = 1.05f;
    
    [Header("Critical Thresholds")]
    [SerializeField] private int criticalHealth = 1;
    [SerializeField] private int criticalHunger = 2;
    
    private int currentHealth = 0;
    private int maxHealth = 5;
    private int currentHunger = 0;
    private int currentHungerCap = 0;
    
    private bool isHealthCritical = false;
    private bool isHungerCritical = false;
    
    private bool canAnimate = true;
    
    private Coroutine healthAnimationCoroutine;
    private Coroutine hungerAnimationCoroutine;

    private void Start()
    {
        // Setup bar colors
        if (healthBarFill != null)
        {
            healthBarFill.color = healthColor;
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left; // Empty from right to left (fill grows from left)
            healthBarFill.fillAmount = 1f;
        }
        
        if (healthBarBackground != null)
            healthBarBackground.color = healthBackgroundColor;
        
        if (hungerBarFill != null)
        {
            hungerBarFill.color = hungerColor;
            hungerBarFill.type = Image.Type.Filled;
            hungerBarFill.fillMethod = Image.FillMethod.Horizontal;
            hungerBarFill.fillOrigin = (int)Image.OriginHorizontal.Left; // Empty from right to left (fill grows from left)
            hungerBarFill.fillAmount = 1f;
        }
        
        if (hungerBarBackground != null)
            hungerBarBackground.color = hungerBackgroundColor;
        
        // Validate references
        if (healthBarFill == null)
            Debug.LogError("ResourceBarDisplay: Health Bar Fill not assigned!");
        if (hungerBarFill == null)
            Debug.LogError("ResourceBarDisplay: Hunger Bar Fill not assigned!");
        if (healthValueText == null)
            Debug.LogError("ResourceBarDisplay: Health Value Text not assigned!");
        if (hungerValueText == null)
            Debug.LogError("ResourceBarDisplay: Hunger Value Text not assigned!");
    }

    private void Update()
    {
        // Check if we can animate (no UI showing)
        bool wasCanAnimate = canAnimate;
        canAnimate = !PauseMenuManager.IsGamePaused() && 
                     (UIQueueManager.Instance == null || !UIQueueManager.Instance.IsAnyUIShowing());
        
        // If we just became able to animate, snap to target
        if (!wasCanAnimate && canAnimate)
        {
            SnapBarsToTarget();
        }
        
        // Handle pulsing for critical states
        if (canAnimate)
        {
            if (isHealthCritical && healthBarFill != null)
            {
                PulseBar(healthBarFill.transform);
            }
            else if (healthBarFill != null)
            {
                healthBarFill.transform.localScale = Vector3.one;
            }
            
            if (isHungerCritical && hungerBarFill != null)
            {
                PulseBar(hungerBarFill.transform);
            }
            else if (hungerBarFill != null)
            {
                hungerBarFill.transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Updates health display
    /// </summary>
    public void UpdateHealth(int health)
    {
        if (health == currentHealth)
            return;
        
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        isHealthCritical = currentHealth <= criticalHealth && currentHealth > 0;
        
        // Update text
        if (healthValueText != null)
            healthValueText.text = $"{currentHealth}/{maxHealth}";
        
        // Update bar fill
        float targetFill = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        SetBarFill(healthBarFill, targetFill, ref healthAnimationCoroutine);
    }

    /// <summary>
    /// Updates hunger display
    /// </summary>
    public void UpdateHunger(int hunger, int hungerCap)
    {
        if (hunger == currentHunger && hungerCap == currentHungerCap)
            return;
        
        currentHunger = Mathf.Clamp(hunger, 0, hungerCap);
        currentHungerCap = hungerCap;
        isHungerCritical = currentHunger <= criticalHunger && currentHunger > 0;
        
        // Update text
        if (hungerValueText != null)
            hungerValueText.text = $"{currentHunger}/{currentHungerCap}";
        
        // Update bar fill
        float targetFill = currentHungerCap > 0 ? (float)currentHunger / currentHungerCap : 0f;
        SetBarFill(hungerBarFill, targetFill, ref hungerAnimationCoroutine);
    }

    /// <summary>
    /// Sets the fill amount for a bar with animation
    /// </summary>
    private void SetBarFill(Image barFill, float targetFill, ref Coroutine animationCoroutine)
    {
        if (barFill == null)
            return;
        
        targetFill = Mathf.Clamp01(targetFill);
        
        if (!canAnimate)
        {
            // Snap immediately if we can't animate
            barFill.fillAmount = targetFill;
            return;
        }
        
        // Stop any existing animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // Start new animation
        animationCoroutine = StartCoroutine(AnimateBarFill(barFill, targetFill));
    }

    /// <summary>
    /// Animates bar fill change
    /// </summary>
    private IEnumerator AnimateBarFill(Image barFill, float targetFill)
    {
        float startFill = barFill.fillAmount;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            barFill.fillAmount = Mathf.Lerp(startFill, targetFill, t);
            
            yield return null;
        }
        
        barFill.fillAmount = targetFill;
    }

    /// <summary>
    /// Snaps all bars to their target values instantly
    /// </summary>
    private void SnapBarsToTarget()
    {
        if (healthAnimationCoroutine != null)
        {
            StopCoroutine(healthAnimationCoroutine);
            healthAnimationCoroutine = null;
        }
        
        if (hungerAnimationCoroutine != null)
        {
            StopCoroutine(hungerAnimationCoroutine);
            hungerAnimationCoroutine = null;
        }
        
        if (healthBarFill != null)
        {
            float healthFill = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
            healthBarFill.fillAmount = healthFill;
        }
        
        if (hungerBarFill != null)
        {
            float hungerFill = currentHungerCap > 0 ? (float)currentHunger / currentHungerCap : 0f;
            hungerBarFill.fillAmount = hungerFill;
        }
    }

    /// <summary>
    /// Creates pulsing effect for critical states
    /// </summary>
    private void PulseBar(Transform barTransform)
    {
        float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        
        barTransform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// Resets display
    /// </summary>
    public void ResetDisplay()
    {
        if (healthAnimationCoroutine != null)
            StopCoroutine(healthAnimationCoroutine);
        if (hungerAnimationCoroutine != null)
            StopCoroutine(hungerAnimationCoroutine);
        
        currentHealth = 0;
        currentHunger = 0;
        currentHungerCap = 0;
        isHealthCritical = false;
        isHungerCritical = false;
        
        if (healthBarFill != null)
            healthBarFill.fillAmount = 0f;
        if (hungerBarFill != null)
            hungerBarFill.fillAmount = 0f;
        if (healthValueText != null)
            healthValueText.text = $"0/{maxHealth}";
        if (hungerValueText != null)
            hungerValueText.text = "0/0";
    }
}

