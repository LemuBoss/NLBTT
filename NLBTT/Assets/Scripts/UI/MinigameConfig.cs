using UnityEngine;

/// <summary>
/// Configuration ScriptableObject for minigame difficulty and parameters
/// Create different configs for different event types (berry picking, wolf encounter, etc.)
/// </summary>
[CreateAssetMenu(fileName = "MinigameConfig", menuName = "Minigame/Config", order = 1)]
public class MinigameConfig : ScriptableObject
{
    [Header("Circle Spawning")]
    [Tooltip("Number of real circles the player must click")]
    public int targetCircleCount = 5;
    
    [Tooltip("Number of fake circles that penalize the player")]
    public int fakeCircleCount = 2;
    
    [Tooltip("Minimum size of circles (diameter in pixels)")]
    public float minCircleSize = 60f;
    
    [Tooltip("Maximum size of circles (diameter in pixels)")]
    public float maxCircleSize = 100f;
    
    [Header("Timing")]
    [Tooltip("Minimum time a circle starts with (seconds)")]
    public float minCircleTime = 2f;
    
    [Tooltip("Maximum time a circle starts with (seconds)")]
    public float maxCircleTime = 5f;
    
    [Tooltip("Base speed at which timers drain (1.0 = normal speed)")]
    public float baseTimerDrainSpeed = 1f;
    
    [Header("Difficulty Scaling")]
    [Tooltip("How much faster remaining timers drain per failed circle (0.3 = 30% faster per failure)")]
    [Range(0f, 1f)]
    public float cascadeMultiplier = 0.3f;
    
    [Tooltip("Percentage of remaining time lost on misclick (0.15 = 15%)")]
    [Range(0f, 0.5f)]
    public float misclickPenaltyPercent = 0.15f;
    
    [Tooltip("Percentage of remaining time lost when touching fake circle (0.2 = 20%)")]
    [Range(0f, 0.5f)]
    public float fakeCirclePenaltyPercent = 0.2f;
    
    [Header("Drift Mechanics")]
    [Tooltip("Enable circles to drift around randomly")]
    public bool enableDrift = false;
    
    [Tooltip("Speed at which circles drift (pixels per second)")]
    public float driftSpeed = 15f;
    
    [Tooltip("Timer drain multiplier when drift is active (1.2 = 20% faster)")]
    public float driftTimerPenalty = 1.2f;
    
    [Header("Special Modifiers")]
    [Tooltip("Invert fake circles to give bonus time instead of penalty")]
    public bool invertFakeCircles = false;
    
    [Tooltip("Amount of time added when touching fake circle (if inverted)")]
    public float fakeCircleBonus = 1f;
    
    [Header("Difficulty Presets")]
    [Tooltip("Quick reference for what this config represents")]
    public DifficultyPreset difficultyPreset = DifficultyPreset.Medium;
}

/// <summary>
/// Quick reference enum for difficulty levels
/// </summary>
public enum DifficultyPreset
{
    VeryEasy,
    Easy,
    Medium,
    Hard,
    VeryHard,
    Custom
}
