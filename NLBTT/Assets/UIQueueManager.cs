using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Master UI Manager that handles the display priority and queuing of different UI panels
/// Ensures only one UI panel is shown at a time and manages the queue
/// </summary>
public class UIQueueManager : MonoBehaviour
{
    public static UIQueueManager Instance { get; private set; }
    
    [Header("UI Manager References")]
    [SerializeField] private EventUIManager eventUIManager;
    [SerializeField] private GameOverUIManager gameOverUIManager;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private Queue<System.Action> uiQueue = new Queue<System.Action>();
    private bool isShowingUI = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple UIQueueManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Auto-find UI managers if not assigned
        if (eventUIManager == null)
            eventUIManager = Object.FindFirstObjectByType<EventUIManager>();
        
        if (gameOverUIManager == null)
            gameOverUIManager = Object.FindFirstObjectByType<GameOverUIManager>();
        
        // Validate references
        if (eventUIManager == null)
            Debug.LogError("UIQueueManager: EventUIManager not found!");
        
        if (gameOverUIManager == null)
            Debug.LogError("UIQueueManager: GameOverUIManager not found!");
    }
    
    private void Update()
    {
        // Check if we can process the next UI in queue
        if (!isShowingUI && uiQueue.Count > 0)
        {
            ProcessNextUI();
        }
        
        // Update current UI showing state
        UpdateUIShowingState();
    }
    
    /// <summary>
    /// Checks if any UI is currently being displayed
    /// </summary>
    private void UpdateUIShowingState()
    {
        bool wasShowingUI = isShowingUI;
        
        isShowingUI = false;
        
        if (eventUIManager != null && eventUIManager.IsShowingEvent())
            isShowingUI = true;
        
        if (gameOverUIManager != null && gameOverUIManager.IsShowingGameEnd())
            isShowingUI = true;
        
        // Log state change
        if (wasShowingUI && !isShowingUI)
        {
            LogDebug("UI closed - checking queue for next UI");
        }
    }
    
    /// <summary>
    /// Processes the next UI action in the queue
    /// </summary>
    private void ProcessNextUI()
    {
        if (uiQueue.Count == 0)
            return;
        
        System.Action nextUIAction = uiQueue.Dequeue();
        LogDebug($"Processing next UI from queue. Remaining in queue: {uiQueue.Count}");
        
        // Execute the UI action (this will show the UI)
        nextUIAction?.Invoke();
        
        // Set flag immediately so we don't process another one
        isShowingUI = true;
    }
    
    // ===== PUBLIC QUEUEING METHODS =====
    
    /// <summary>
    /// Queues an event UI to be shown
    /// </summary>
    public void QueueEventUI(ComplexEventCard eventCard)
    {
        if (eventCard == null)
        {
            Debug.LogError("UIQueueManager: Cannot queue null event card!");
            return;
        }
        
        LogDebug($"Queueing event UI: {eventCard.GetEventTitle()}");
        
        uiQueue.Enqueue(() =>
        {
            if (eventUIManager != null)
                eventUIManager.ShowEventChoice(eventCard);
        });
    }
    
    /// <summary>
    /// Queues a game over screen to be shown
    /// </summary>
    public void QueueGameOver(string message)
    {
        LogDebug($"Queueing Game Over UI: {message}");
        
        uiQueue.Enqueue(() =>
        {
            if (gameOverUIManager != null)
                gameOverUIManager.ShowGameOver(message);
        });
    }
    
    /// <summary>
    /// Queues a victory screen to be shown
    /// </summary>
    public void QueueVictory(string message)
    {
        LogDebug($"Queueing Victory UI: {message}");
        
        uiQueue.Enqueue(() =>
        {
            if (gameOverUIManager != null)
                gameOverUIManager.ShowVictory(message);
        });
    }
    
    /// <summary>
    /// Shows an event UI immediately (use sparingly, prefer queueing)
    /// </summary>
    public void ShowEventUIImmediate(ComplexEventCard eventCard)
    {
        if (eventUIManager != null && eventCard != null)
        {
            LogDebug($"Showing event UI immediately: {eventCard.GetEventTitle()}");
            eventUIManager.ShowEventChoice(eventCard);
            isShowingUI = true;
        }
    }
    
    /// <summary>
    /// Shows game over immediately (use sparingly, prefer queueing)
    /// </summary>
    public void ShowGameOverImmediate(string message)
    {
        if (gameOverUIManager != null)
        {
            LogDebug($"Showing Game Over UI immediately: {message}");
            gameOverUIManager.ShowGameOver(message);
            isShowingUI = true;
        }
    }
    
    /// <summary>
    /// Shows victory immediately (use sparingly, prefer queueing)
    /// </summary>
    public void ShowVictoryImmediate(string message)
    {
        if (gameOverUIManager != null)
        {
            LogDebug($"Showing Victory UI immediately: {message}");
            gameOverUIManager.ShowVictory(message);
            isShowingUI = true;
        }
    }
    
    /// <summary>
    /// Clears all queued UI actions (useful for game restart)
    /// </summary>
    public void ClearQueue()
    {
        int count = uiQueue.Count;
        uiQueue.Clear();
        LogDebug($"Cleared {count} UI actions from queue");
    }
    
    /// <summary>
    /// Returns whether any UI is currently being shown
    /// </summary>
    public bool IsAnyUIShowing()
    {
        return isShowingUI;
    }
    
    /// <summary>
    /// Returns the number of UI actions waiting in queue
    /// </summary>
    public int GetQueueCount()
    {
        return uiQueue.Count;
    }
    
    /// <summary>
    /// Helper method for debug logging
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[UIQueueManager] {message}");
        }
    }
}