using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages visual display of health and hunger using circle sprites
/// Circles empty from RIGHT to LEFT, filling VERTICALLY from bottom to top
/// </summary>
public class ResourceCircleDisplay : MonoBehaviour
{
    [Header("Circle Prefabs")]
    [SerializeField] private GameObject circlePrefab; // Unity's default circle sprite
    
    [Header("Health Display")]
    [SerializeField] private Transform healthContainer;
    [SerializeField] private Color healthColor = new Color(0.8f, 0.1f, 0.1f); // Red
    [SerializeField] private Image healthIcon; // Icon next to health circles
    
    [Header("Hunger Display")]
    [SerializeField] private Transform hungerContainer;
    [SerializeField] private Color hungerColor = new Color(0.9f, 0.8f, 0.2f); // Yellow
    [SerializeField] private Image hungerIcon; // Icon next to hunger circles
    
    [Header("Circle Settings")]
    [SerializeField] private float circleSize = 30f;
    [SerializeField] private float circleSpacing = 5f;
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinScale = 0.9f;
    [SerializeField] private float pulseMaxScale = 1.1f;
    
    [Header("Value Per Circle")]
    [SerializeField] private int healthPerCircle = 1; // 1 HP per circle
    [SerializeField] private int hungerPerCircle = 4; // 4 Hunger per circle (changed from 2)
    
    [Header("Critical Thresholds")]
    [SerializeField] private int criticalHealth = 1;
    [SerializeField] private int criticalHunger = 2;
    
    private List<CircleData> healthCircles = new List<CircleData>();
    private List<CircleData> hungerCircles = new List<CircleData>();
    
    private int currentHealth = 0;
    private int currentHunger = 0;
    private int currentHungerCap = 0;
    
    private bool isHealthCritical = false;
    private bool isHungerCritical = false;
    
    private bool canAnimate = true; // Only animate when no UI is showing
    
    private class CircleData
    {
        public GameObject gameObject;
        public Image fillImage;
        public float targetFill; // 0.0 to 1.0
        public float currentFill;
        public Coroutine animationCoroutine;
    }

    private void Start()
    {
        if (circlePrefab == null)
        {
            Debug.LogError("ResourceCircleDisplay: Circle prefab not assigned!");
        }
    }

    private void Update()
    {
        // Check if we can animate (no UI showing)
        bool wasCanAnimate = canAnimate;
        canAnimate = !PauseMenuManager.IsGamePaused() && 
                     (UIQueueManager.Instance == null || !UIQueueManager.Instance.IsAnyUIShowing());
        
        // If we just became able to animate, catch up any pending changes
        if (!wasCanAnimate && canAnimate)
        {
            SnapAllCirclesToTarget();
        }
        
        // Handle pulsing for critical states
        if (canAnimate)
        {
            if (isHealthCritical)
            {
                PulseCircles(healthCircles);
            }
            
            if (isHungerCritical)
            {
                PulseCircles(hungerCircles);
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
        
        currentHealth = health;
        isHealthCritical = health <= criticalHealth && health > 0;
        
        UpdateCircleDisplay(healthCircles, health, healthPerCircle, healthColor, healthContainer);
    }

    /// <summary>
    /// Updates hunger display (uses quarter circles - 4 hunger per circle)
    /// </summary>
    public void UpdateHunger(int hunger, int hungerCap)
    {
        if (hunger == currentHunger && hungerCap == currentHungerCap)
            return;
        
        currentHunger = hunger;
        currentHungerCap = hungerCap;
        isHungerCritical = hunger <= criticalHunger && hunger > 0;
        
        UpdateCircleDisplay(hungerCircles, hunger, hungerPerCircle, hungerColor, hungerContainer);
    }

    /// <summary>
    /// Core method to update circle displays
    /// </summary>
    private void UpdateCircleDisplay(List<CircleData> circles, int value, int valuePerCircle, 
                                    Color color, Transform container)
    {
        // Calculate required circles
        int requiredCircles;
        if (circles == healthCircles)
        {
            // Health: always show max 5 circles
            requiredCircles = 5;
        }
        else
        {
            // Hunger: calculate based on cap
            requiredCircles = Mathf.CeilToInt(currentHungerCap / (float)valuePerCircle);
        }
        
        // Adjust circle count
        while (circles.Count < requiredCircles)
        {
            CreateCircle(circles, color, container);
        }
        
        while (circles.Count > requiredCircles)
        {
            RemoveCircle(circles, circles.Count - 1);
        }
        
        // Update fill amounts
        // IMPORTANT: Circles empty from RIGHT to LEFT
        // Index 0 = leftmost, Index N = rightmost
        // Rightmost circles should empty first!
        
        for (int i = 0; i < circles.Count; i++)
        {
            // How many units of value remain after filling all circles to the right?
            // Circles to the right of this one
            int circlesRightOfThis = circles.Count - 1 - i;
            
            // Minimum value needed to START filling this circle
            int minValueForThisCircle = circlesRightOfThis * valuePerCircle;
            
            // How much value is allocated to this circle?
            int valueInThisCircle = Mathf.Max(0, value - minValueForThisCircle);
            
            float targetFill;
            if (valueInThisCircle <= 0)
            {
                targetFill = 0f;
            }
            else if (valueInThisCircle >= valuePerCircle)
            {
                targetFill = 1f;
            }
            else
            {
                targetFill = valueInThisCircle / (float)valuePerCircle;
            }
            
            SetCircleFill(circles[i], targetFill);
        }
    }

    /// <summary>
    /// Creates a new circle
    /// </summary>
    private void CreateCircle(List<CircleData> circles, Color color, Transform container)
    {
        if (circlePrefab == null)
            return;
        
        GameObject circle = Instantiate(circlePrefab, container);
        
        // Setup circle
        RectTransform rectTransform = circle.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(circleSize, circleSize);
        
        Image fillImage = circle.GetComponent<Image>();
        if (fillImage == null)
        {
            fillImage = circle.AddComponent<Image>();
        }
        
        // VERTICAL FILL - Bottom to Top
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.OriginVertical.Top; // Fill from bottom to top
        fillImage.fillClockwise = true;
        fillImage.fillAmount = 1f;
        fillImage.color = color;
        
        CircleData data = new CircleData
        {
            gameObject = circle,
            fillImage = fillImage,
            targetFill = 1f,
            currentFill = 1f
        };
        
        circles.Add(data);
        
        // Position all circles
        RepositionCircles(circles);
    }

    /// <summary>
    /// Removes a circle
    /// </summary>
    private void RemoveCircle(List<CircleData> circles, int index)
    {
        if (index < 0 || index >= circles.Count)
            return;
        
        CircleData data = circles[index];
        
        if (data.animationCoroutine != null)
        {
            StopCoroutine(data.animationCoroutine);
        }
        
        Destroy(data.gameObject);
        circles.RemoveAt(index);
        
        RepositionCircles(circles);
    }

    /// <summary>
    /// Sets the fill amount for a circle with animation
    /// </summary>
    private void SetCircleFill(CircleData data, float targetFill)
    {
        data.targetFill = Mathf.Clamp01(targetFill);
        
        if (!canAnimate)
        {
            // Snap immediately if we can't animate
            data.currentFill = data.targetFill;
            data.fillImage.fillAmount = data.currentFill;
            return;
        }
        
        // Stop any existing animation
        if (data.animationCoroutine != null)
        {
            StopCoroutine(data.animationCoroutine);
        }
        
        // Start new animation
        data.animationCoroutine = StartCoroutine(AnimateFill(data));
    }

    /// <summary>
    /// Animates fill change
    /// </summary>
    private IEnumerator AnimateFill(CircleData data)
    {
        float startFill = data.currentFill;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            data.currentFill = Mathf.Lerp(startFill, data.targetFill, t);
            data.fillImage.fillAmount = data.currentFill;
            
            yield return null;
        }
        
        data.currentFill = data.targetFill;
        data.fillImage.fillAmount = data.currentFill;
        data.animationCoroutine = null;
    }

    /// <summary>
    /// Snaps all circles to their target values instantly
    /// </summary>
    private void SnapAllCirclesToTarget()
    {
        foreach (var data in healthCircles)
        {
            if (data.animationCoroutine != null)
            {
                StopCoroutine(data.animationCoroutine);
                data.animationCoroutine = null;
            }
            data.currentFill = data.targetFill;
            data.fillImage.fillAmount = data.currentFill;
        }
        
        foreach (var data in hungerCircles)
        {
            if (data.animationCoroutine != null)
            {
                StopCoroutine(data.animationCoroutine);
                data.animationCoroutine = null;
            }
            data.currentFill = data.targetFill;
            data.fillImage.fillAmount = data.currentFill;
        }
    }

    /// <summary>
    /// Repositions all circles in a container
    /// </summary>
    private void RepositionCircles(List<CircleData> circles)
    {
        for (int i = 0; i < circles.Count; i++)
        {
            RectTransform rectTransform = circles[i].gameObject.GetComponent<RectTransform>();
            float xPos = i * (circleSize + circleSpacing);
            rectTransform.anchoredPosition = new Vector2(xPos, 0);
        }
    }

    /// <summary>
    /// Creates pulsing effect for critical states
    /// </summary>
    private void PulseCircles(List<CircleData> circles)
    {
        float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        
        foreach (var data in circles)
        {
            data.gameObject.transform.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// Resets display
    /// </summary>
    public void ResetDisplay()
    {
        // Clear all circles
        foreach (var data in healthCircles)
        {
            if (data.animationCoroutine != null)
                StopCoroutine(data.animationCoroutine);
            Destroy(data.gameObject);
        }
        healthCircles.Clear();
        
        foreach (var data in hungerCircles)
        {
            if (data.animationCoroutine != null)
                StopCoroutine(data.animationCoroutine);
            Destroy(data.gameObject);
        }
        hungerCircles.Clear();
        
        currentHealth = 0;
        currentHunger = 0;
        currentHungerCap = 0;
        isHealthCritical = false;
        isHungerCritical = false;
    }
}

