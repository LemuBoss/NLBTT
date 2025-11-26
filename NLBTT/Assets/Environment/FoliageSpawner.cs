using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns decorative foliage prefabs in empty spaces on the game table.
/// Uses physics overlap checks to avoid placing foliage where cards exist.
/// Completely independent from BoardManager.
/// </summary>
public class FoliageDecorator : MonoBehaviour
{
    [Header("Spawn Area")]
    [Tooltip("Optional: Reference to BoardManager to auto-sync spawn area with board offset")]
    [SerializeField] private BoardManager boardManager;
    
    [Tooltip("Center point of the spawn area (X, Y, Z). The green gizmo shows exactly where foliage will spawn.")]
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
    
    [Tooltip("Width (X-axis) of the spawn area")]
    [SerializeField] private float spawnAreaWidth = 10f;
    
    [Tooltip("Depth (Z-axis) of the spawn area")]
    [SerializeField] private float spawnAreaDepth = 10f;
    
    [Header("Foliage Settings")]
    [Tooltip("Array of foliage prefabs to randomly choose from")]
    [SerializeField] private GameObject[] foliagePrefabs;
    
    [Tooltip("How many foliage pieces to attempt to spawn")]
    [SerializeField] private int foliageCount = 20;
    
    [Tooltip("Maximum placement attempts before giving up")]
    [SerializeField] private int maxPlacementAttempts = 100;
    
    [Header("Spacing & Collision")]
    [Tooltip("Minimum distance between foliage pieces")]
    [SerializeField] private float minFoliageSpacing = 0.3f;
    
    [Tooltip("Size of the overlap check box (width, height, depth)")]
    [SerializeField] private Vector3 overlapCheckSize = new Vector3(0.2f, 0.5f, 0.2f);
    
    [Tooltip("Layers to check for collisions (should include cards and other foliage)")]
    [SerializeField] private LayerMask collisionLayers = -1; // Default: all layers
    
    [Header("Randomization")]
    [Tooltip("Random rotation range around Y-axis (in degrees)")]
    [SerializeField] private float randomRotationRange = 360f;
    
    [Tooltip("Random scale variation (multiplier)")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
    [Tooltip("Use random seed for reproducible results")]
    [SerializeField] private bool useRandomSeed = true;
    
    [Tooltip("Manual seed (only used if useRandomSeed is false)")]
    [SerializeField] private int manualSeed = 12345;
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
    
    private List<Vector3> placedFoliagePositions = new List<Vector3>();
    private System.Random rng;
    
    void Start()
    {
        // Initialize random number generator
        rng = useRandomSeed ? new System.Random() : new System.Random(manualSeed);
        
        // Sync spawn area with BoardManager if assigned
        if (boardManager != null)
        {
            // Access the boardOffset from BoardManager's serialized field
            // We'll need to make this public or add a getter
            Debug.Log("FoliageDecorator: Syncing with BoardManager offset");
        }
        
        Debug.Log($"FoliageDecorator: Spawn area center set to ({spawnAreaCenter.x:F2}, {spawnAreaCenter.y:F2}, {spawnAreaCenter.z:F2})");
        
        // Spawn foliage after a short delay to ensure cards are placed first
        Invoke(nameof(SpawnFoliage), 0.1f);
    }
    
    /// <summary>
    /// Main method to spawn all foliage pieces
    /// </summary>
    public void SpawnFoliage()
    {
        Debug.Log("FoliageDecorator: Starting foliage spawn process...");
        
        if (foliagePrefabs == null || foliagePrefabs.Length == 0)
        {
            Debug.LogWarning("FoliageDecorator: No foliage prefabs assigned!");
            return;
        }
        
        Debug.Log($"FoliageDecorator: {foliagePrefabs.Length} prefab(s) available");
        
        // Clear any existing foliage
        ClearFoliage();
        
        int successfulPlacements = 0;
        int totalAttempts = 0;
        int collisionFailures = 0;
        int spacingFailures = 0;
        
        while (successfulPlacements < foliageCount && totalAttempts < maxPlacementAttempts)
        {
            totalAttempts++;
            
            // Generate random position within spawn area
            Vector3 candidatePosition = GetRandomPositionInArea();
            
            // Check if position is valid (no collisions)
            if (IsPositionValid(candidatePosition, out string failReason))
            {
                // Spawn foliage at this position
                PlaceFoliage(candidatePosition);
                successfulPlacements++;
            }
            else
            {
                if (failReason == "collision")
                    collisionFailures++;
                else if (failReason == "spacing")
                    spacingFailures++;
            }
        }
        
        Debug.Log($"FoliageDecorator: Placed {successfulPlacements} foliage pieces in {totalAttempts} attempts");
        Debug.Log($"FoliageDecorator: Failures - Collisions: {collisionFailures}, Spacing: {spacingFailures}");
    }
    
    /// <summary>
    /// Generates a random position within the spawn area
    /// </summary>
    private Vector3 GetRandomPositionInArea()
    {
        float randomX = spawnAreaCenter.x + ((float)rng.NextDouble() - 0.5f) * spawnAreaWidth;
        float randomZ = spawnAreaCenter.z + ((float)rng.NextDouble() - 0.5f) * spawnAreaDepth;
        
        // Use the spawn area center's Y position instead of separate foliageHeight
        return new Vector3(randomX, spawnAreaCenter.y, randomZ);
    }
    
    /// <summary>
    /// Checks if a position is valid for placing foliage (no collisions, proper spacing)
    /// </summary>
    private bool IsPositionValid(Vector3 position, out string failReason)
    {
        failReason = "";
        
        // Check for physics collisions (cards, other objects)
        Collider[] colliders = Physics.OverlapBox(
            position,
            overlapCheckSize / 2f,
            Quaternion.identity,
            collisionLayers
        );
        
        if (colliders.Length > 0)
        {
            failReason = "collision";
            return false; // Something is blocking this position
        }
        
        // Check minimum spacing from other foliage
        foreach (Vector3 existingPos in placedFoliagePositions)
        {
            float distance = Vector3.Distance(position, existingPos);
            if (distance < minFoliageSpacing)
            {
                failReason = "spacing";
                return false; // Too close to another foliage piece
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Places a foliage prefab at the given position
    /// </summary>
    private void PlaceFoliage(Vector3 position)
    {
        // Randomly select a prefab
        GameObject prefab = foliagePrefabs[rng.Next(foliagePrefabs.Length)];
        
        // Random rotation around Y-axis
        float randomYRotation = ((float)rng.NextDouble() - 0.5f) * randomRotationRange;
        Quaternion rotation = Quaternion.Euler(0f, randomYRotation, 0f);
        
        // Instantiate the foliage
        GameObject foliageObj = Instantiate(prefab, position, rotation, transform);
        
        // Apply random scale
        float randomScale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)rng.NextDouble());
        foliageObj.transform.localScale = Vector3.one * randomScale;
        
        // Track this position
        placedFoliagePositions.Add(position);
        
        // Debug log for each placement
        Debug.Log($"FoliageDecorator: Placed '{prefab.name}' at position ({position.x:F2}, {position.y:F2}, {position.z:F2}), " +
                  $"rotation Y: {randomYRotation:F1}°, scale: {randomScale:F2}x");
    }
    
    /// <summary>
    /// Clears all spawned foliage
    /// </summary>
    public void ClearFoliage()
    {
        // Destroy all children (foliage objects)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
        placedFoliagePositions.Clear();
        
        Debug.Log("FoliageDecorator: Cleared all foliage");
    }
    
    /// <summary>
    /// For testing: Press F to regenerate foliage
    /// </summary>
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("Regenerating foliage (F key pressed)");
            SpawnFoliage();
        }
    }
    
    /// <summary>
    /// Visualizes the spawn area in the Scene view
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;
        
        // Draw spawn area bounds
        Gizmos.color = gizmoColor;
        Vector3 size = new Vector3(spawnAreaWidth, 0.1f, spawnAreaDepth);
        Gizmos.DrawCube(spawnAreaCenter, size);
        
        // Draw wireframe
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCenter, size);
        
        // Draw placed foliage positions (when playing)
        if (Application.isPlaying && placedFoliagePositions.Count > 0)
        {
            Gizmos.color = Color.cyan;
            foreach (Vector3 pos in placedFoliagePositions)
            {
                Gizmos.DrawWireSphere(pos, minFoliageSpacing / 2f);
            }
        }
    }
    
    /// <summary>
    /// Visualizes overlap check boxes in Scene view (when selected)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showGizmos || !Application.isPlaying)
            return;
        
        // Draw overlap check boxes at placed foliage positions
        Gizmos.color = Color.yellow;
        foreach (Vector3 pos in placedFoliagePositions)
        {
            Gizmos.DrawWireCube(pos, overlapCheckSize);
        }
    }
}