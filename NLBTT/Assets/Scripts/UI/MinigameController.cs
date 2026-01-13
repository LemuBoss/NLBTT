using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Main orchestrator for the circle-clicking minigame
/// Manages spawning, state, and win/loss conditions
/// </summary>
public class MinigameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MinigameUIManager uiManager;
    [SerializeField] private GameObject circlePreab;
    [SerializeField] private RectTransform circleContainer;
    
    [Header("Test Configuration")]
    [SerializeField] private MinigameConfig testConfig;
    [SerializeField] private KeyCode testKey = KeyCode.M;
    [SerializeField] private bool enableTestMode = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // State tracking
    private List<MinigameCircle> activeCircles = new List<MinigameCircle>();
    private MinigameConfig currentConfig;
    private MinigameModifiers currentModifiers;
    private bool isActive = false;
    private int completedCircles = 0;
    private int failedCircles = 0;
    private int totalRealCircles = 0; // Track how many real circles we actually spawned
    private float globalTimerMultiplier = 1f;
    
    // Spawning
    private List<Vector2> occupiedPositions = new List<Vector2>();
    private List<float> occupiedRadii = new List<float>();
    private const float MIN_SPAWN_GAP = 10f; // Reduced from 20f for tighter packing
    private const int MAX_SPAWN_ATTEMPTS = 200; // Increased attempts
    
    // Events - these are cleared after each use
    private System.Action onMinigameSuccessCallback;
    private System.Action onMinigameFailureCallback;
    
    public System.Action OnMinigameSuccess 
    { 
        get => onMinigameSuccessCallback; 
        set => onMinigameSuccessCallback = value; 
    }
    
    public System.Action OnMinigameFailure 
    { 
        get => onMinigameFailureCallback; 
        set => onMinigameFailureCallback = value; 
    }
    
    private void Awake()
    {
        if (uiManager == null)
            uiManager = GetComponent<MinigameUIManager>();
        
        if (uiManager == null)
            Debug.LogError("MinigameController: MinigameUIManager not found!");
        
        if (circlePreab == null)
            Debug.LogError("MinigameController: Circle prefab not assigned!");
        
        if (circleContainer == null)
            Debug.LogError("MinigameController: Circle container not assigned!");
    }
    
    private void Update()
    {
        // Test mode trigger
        if (enableTestMode && !isActive && Input.GetKeyDown(testKey))
        {
            if (testConfig != null)
            {
                LogDebug("Test mode: Starting minigame");
                StartMinigame(testConfig, new MinigameModifiers());
            }
            else
            {
                Debug.LogError("MinigameController: Test config not assigned!");
            }
        }
        
        // Update global multiplier based on failures
        if (isActive)
        {
            UpdateGlobalMultiplier();
            
            // Safety check: If all real circles are gone but we haven't ended, end now
            int remainingRealCircles = 0;
            foreach (var circle in activeCircles)
            {
                if (circle != null && circle.Type == CircleType.Real)
                    remainingRealCircles++;
            }
            
            if (remainingRealCircles == 0 && completedCircles < totalRealCircles)
            {
                Debug.LogWarning("MinigameController: All real circles gone but minigame still active - forcing failure");
                EndMinigame(false);
            }
        }
    }
    
    /// <summary>
    /// Starts the minigame with the given configuration and modifiers
    /// </summary>
    public void StartMinigame(MinigameConfig config, MinigameModifiers modifiers, System.Action onSuccess, System.Action onFailure)
    {
        if (isActive)
        {
            Debug.LogWarning("MinigameController: Minigame already active!");
            return;
        }
        
        currentConfig = config;
        currentModifiers = modifiers;
        onMinigameSuccessCallback = onSuccess;
        onMinigameFailureCallback = onFailure;
        isActive = true;
        completedCircles = 0;
        failedCircles = 0;
        totalRealCircles = 0;
        globalTimerMultiplier = 1f;
        
        LogDebug($"Starting minigame: {config.targetCircleCount} targets, {config.fakeCircleCount} fakes");
        
        // Show UI
        if (uiManager != null)
            uiManager.ShowMinigame();
        
        // Spawn circles
        SpawnAllCircles();
    }
    
    /// <summary>
    /// Overload for testing without callbacks
    /// </summary>
    public void StartMinigame(MinigameConfig config, MinigameModifiers modifiers)
    {
        StartMinigame(config, modifiers, 
            () => Debug.Log("[MinigameController] TEST MODE - SUCCESS!"), 
            () => Debug.Log("[MinigameController] TEST MODE - FAILURE!"));
    }
    
    /// <summary>
    /// Spawns all real and fake circles
    /// </summary>
    private void SpawnAllCircles()
    {
        // Clear any existing circles first (safety check)
        if (circleContainer != null)
        {
            foreach (Transform child in circleContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        occupiedPositions.Clear();
        occupiedRadii.Clear();
        activeCircles.Clear();
        
        // Apply modifier to fake circle count
        int totalFakes = Mathf.Max(0, currentConfig.fakeCircleCount + currentModifiers.fakeCircleCountModifier);
        int totalReal = currentConfig.targetCircleCount;
        
        LogDebug($"Attempting to spawn {totalReal} real circles and {totalFakes} fake circles");
        
        // Spawn real circles first (higher priority)
        int realSpawned = 0;
        for (int i = 0; i < totalReal; i++)
        {
            if (SpawnCircle(CircleType.Real))
                realSpawned++;
        }
        
        // Track how many real circles we actually spawned
        totalRealCircles = realSpawned;
        
        // Spawn fake circles
        int fakeSpawned = 0;
        for (int i = 0; i < totalFakes; i++)
        {
            if (SpawnCircle(CircleType.Fake))
                fakeSpawned++;
        }
        
        LogDebug($"Successfully spawned {realSpawned}/{totalReal} real circles and {fakeSpawned}/{totalFakes} fake circles");
        
        // Check if we have enough circles to play
        if (totalRealCircles == 0)
        {
            Debug.LogError("MinigameController: No real circles spawned! Cannot start minigame.");
            EndMinigame(false);
            return;
        }
        
        // Warning if we couldn't spawn all circles
        if (realSpawned < totalReal)
        {
            Debug.LogWarning($"MinigameController: Could only spawn {realSpawned}/{totalReal} real circles! Container may be too small or circles too large.");
        }
        
        if (fakeSpawned < totalFakes)
        {
            Debug.LogWarning($"MinigameController: Could only spawn {fakeSpawned}/{totalFakes} fake circles! This is OK, continuing with fewer fakes.");
        }
    }
    
    /// <summary>
    /// Spawns a single circle of the given type
    /// Returns true if successful, false if couldn't find valid position
    /// </summary>
    private bool SpawnCircle(CircleType type)
    {
        // Try to find valid position
        Vector2 position;
        float radius;
        int attempts = 0;
        
        do
        {
            position = GetRandomPosition();
            radius = Random.Range(currentConfig.minCircleSize, currentConfig.maxCircleSize) / 2f;
            attempts++;
            
            if (attempts >= MAX_SPAWN_ATTEMPTS)
            {
                Debug.LogWarning($"MinigameController: Could not find valid position for {type} circle after {MAX_SPAWN_ATTEMPTS} attempts");
                return false; // Return false instead of return (void)
            }
        }
        while (!IsPositionValid(position, radius));
        
        // Spawn the circle
        GameObject circleObj = Instantiate(circlePreab, circleContainer);
        RectTransform rectTransform = circleObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        
        // Configure the circle component
        MinigameCircle circle = circleObj.GetComponent<MinigameCircle>();
        if (circle != null)
        {
            float maxTime = type == CircleType.Real 
                ? Random.Range(currentConfig.minCircleTime, currentConfig.maxCircleTime)
                : 0f;
            
            bool enableDrift = currentConfig.enableDrift || currentModifiers.enableDrift;
            
            circle.Initialize(
                type, 
                maxTime, 
                radius,
                this,
                enableDrift,
                currentConfig.driftSpeed
            );
            
            activeCircles.Add(circle);
            occupiedPositions.Add(position);
            occupiedRadii.Add(radius);
            
            LogDebug($"Spawned {type} circle at {position} with radius {radius}");
            return true;
        }
        else
        {
            Debug.LogError("MinigameController: Circle prefab missing MinigameCircle component!");
            Destroy(circleObj);
            return false;
        }
    }
    
    /// <summary>
    /// Gets a random position within the circle container bounds
    /// </summary>
    private Vector2 GetRandomPosition()
    {
        Rect rect = circleContainer.rect;
        float padding = currentConfig.maxCircleSize / 2f + 10f;
        
        float x = Random.Range(rect.xMin + padding, rect.xMax - padding);
        float y = Random.Range(rect.yMin + padding, rect.yMax - padding);
        
        return new Vector2(x, y);
    }
    
    /// <summary>
    /// Checks if a position is valid (doesn't overlap with existing circles)
    /// </summary>
    private bool IsPositionValid(Vector2 position, float radius)
    {
        for (int i = 0; i < occupiedPositions.Count; i++)
        {
            float distance = Vector2.Distance(position, occupiedPositions[i]);
            float requiredDistance = radius + occupiedRadii[i] + MIN_SPAWN_GAP;
            
            if (distance < requiredDistance)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Updates the global timer multiplier based on failed circles
    /// </summary>
    private void UpdateGlobalMultiplier()
    {
        globalTimerMultiplier = 1f + (failedCircles * currentConfig.cascadeMultiplier);
    }
    
    /// <summary>
    /// Returns the current drain speed multiplier for circles
    /// </summary>
    public float GetDrainSpeedMultiplier()
    {
        return currentConfig.baseTimerDrainSpeed 
            * globalTimerMultiplier 
            * currentModifiers.timerDrainMultiplier;
    }
    
    /// <summary>
    /// Returns the drift timer penalty multiplier
    /// </summary>
    public float GetDriftTimerPenalty()
    {
        return currentConfig.driftTimerPenalty;
    }
    
    /// <summary>
    /// Called when a real circle is successfully clicked
    /// </summary>
    public void OnCircleClicked(MinigameCircle circle)
    {
        if (!isActive || circle.Type != CircleType.Real)
            return;
        
        completedCircles++;
        activeCircles.Remove(circle);
        
        LogDebug($"Circle clicked! {completedCircles}/{totalRealCircles} completed");
        
        // Check win condition - compare against actual spawned count
        if (completedCircles >= totalRealCircles)
        {
            LogDebug("All real circles clicked - SUCCESS!");
            EndMinigame(true);
        }
    }
    
    /// <summary>
    /// Called when a real circle's timer expires
    /// </summary>
    public void OnCircleTimerExpired(MinigameCircle circle)
    {
        if (!isActive || circle.Type != CircleType.Real)
            return;
        
        failedCircles++;
        activeCircles.Remove(circle);
        
        LogDebug($"Circle timer expired! {failedCircles} failed");
        
        // Immediate failure
        EndMinigame(false);
    }
    
    /// <summary>
    /// Called when player misclicks (clicks outside any circle)
    /// </summary>
    public void OnMisclick(Vector2 clickPosition)
    {
        if (!isActive)
            return;
        
        LogDebug("Misclick detected!");
        
        // Apply penalty to all real circles
        float penaltyPercent = currentConfig.misclickPenaltyPercent * currentModifiers.misclickPenaltyMultiplier;
        
        foreach (var circle in activeCircles)
        {
            if (circle.Type == CircleType.Real)
            {
                circle.ApplyTimePenalty(penaltyPercent);
            }
        }
        
        // Visual feedback
        if (uiManager != null)
            uiManager.ShowPenaltyEffect();
    }
    
    /// <summary>
    /// Called when player touches a fake circle
    /// </summary>
    public void OnFakeCircleTouched(MinigameCircle circle)
    {
        if (!isActive)
            return;
        
        LogDebug("Fake circle touched!");
        
        // Check if fake circles are inverted (give bonus instead)
        bool inverted = currentConfig.invertFakeCircles || currentModifiers.invertFakeCircles;
        
        if (inverted)
        {
            // Add time to all real circles
            foreach (var c in activeCircles)
            {
                if (c.Type == CircleType.Real)
                {
                    c.AddBonusTime(currentConfig.fakeCircleBonus);
                }
            }
            
            if (uiManager != null)
                uiManager.ShowBonusEffect();
        }
        else
        {
            // Apply penalty to all real circles
            float penaltyPercent = currentConfig.fakeCirclePenaltyPercent * currentModifiers.misclickPenaltyMultiplier;
            
            foreach (var c in activeCircles)
            {
                if (c.Type == CircleType.Real)
                {
                    c.ApplyTimePenalty(penaltyPercent);
                }
            }
            
            if (uiManager != null)
                uiManager.ShowPenaltyEffect();
        }
    }
    
    /// <summary>
    /// Checks if a click position is within any circle's bounds
    /// </summary>
    public bool IsClickOnAnyCircle(Vector2 clickPosition)
    {
        foreach (var circle in activeCircles)
        {
            if (circle.IsPositionInBounds(clickPosition))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Ends the minigame with success or failure
    /// </summary>
    private void EndMinigame(bool success)
    {
        if (!isActive)
            return;
        
        isActive = false;
        
        LogDebug($"Minigame ended: {(success ? "SUCCESS" : "FAILURE")}");
        
        // Destroy all remaining circles immediately
        foreach (var circle in activeCircles)
        {
            if (circle != null)
                Destroy(circle.gameObject);
        }
        activeCircles.Clear();
        
        // Also destroy any orphaned circles in the container (safety cleanup)
        if (circleContainer != null)
        {
            foreach (Transform child in circleContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Clear tracking lists
        occupiedPositions.Clear();
        occupiedRadii.Clear();

        
        // Trigger events
        if (success)
            onMinigameSuccessCallback?.Invoke();
        else
            onMinigameFailureCallback?.Invoke();
        
        // Clear callbacks
        onMinigameSuccessCallback = null;
        onMinigameFailureCallback = null;
    }
    
    
    /// <summary>
    /// Returns whether the minigame is currently active
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
    
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[MinigameController] {message}");
        }
    }
}


