using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates a boolean layout for card placement using a path-based algorithm.
/// Creates a "spine" path through random waypoints, then buffs it out to create
/// natural-looking island formations with guaranteed connectivity.
/// </summary>
public static class BoardLayoutGenerator
{
    /// <summary>
    /// Generates a boolean grid where true = place card, false = empty space
    /// </summary>
    /// <param name="width">Grid width</param>
    /// <param name="height">Grid height</param>
    /// <param name="startPosition">Player starting position (center of path)</param>
    /// <param name="numberOfWaypoints">How many random points to visit</param>
    /// <param name="minBuffRadius">Minimum buffing radius around path</param>
    /// <param name="maxBuffRadius">Maximum buffing radius around path</param>
    /// <param name="orthogonalBuffProbability">Probability to fill orthogonal neighbors (0-1)</param>
    /// <param name="diagonalBuffProbability">Probability to fill diagonal neighbors (0-1)</param>
    /// <param name="seed">Random seed for reproducibility (optional)</param>
    /// <returns>2D boolean array for card placement</returns>
    public static bool[,] Generate(
        int width,
        int height,
        Vector2Int startPosition,
        int numberOfWaypoints,
        int minBuffRadius,
        int maxBuffRadius,
        float orthogonalBuffProbability,
        float diagonalBuffProbability,
        int? seed = null)
    {
        // Initialize empty grid
        bool[,] layout = new bool[width, height];
        
        // Use provided seed or random one
        System.Random rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        
        // Track all cells that are part of the spine path
        HashSet<Vector2Int> spineCells = new HashSet<Vector2Int>();
        
        // Step 1: Generate random waypoints
        List<Vector2Int> waypoints = GenerateWaypoints(width, height, startPosition, numberOfWaypoints, rng);
        
        // Step 2: Create spine path through all waypoints
        CreateSpinePath(spineCells, startPosition, waypoints, width, height);
        
        // Step 3: Mark all spine cells in layout
        foreach (Vector2Int cell in spineCells)
        {
            if (IsValidPosition(cell.x, cell.y, width, height))
            {
                layout[cell.x, cell.y] = true;
            }
        }
        
        // Step 4: Buff out the spine path
        BuffSpinePath(layout, spineCells, width, height, minBuffRadius, maxBuffRadius, 
                      orthogonalBuffProbability, diagonalBuffProbability, rng);
        
        return layout;
    }
    
    /// <summary>
    /// Generates random waypoints across the grid, avoiding edges
    /// </summary>
    private static List<Vector2Int> GenerateWaypoints(int width, int height, Vector2Int startPosition, 
                                                      int count, System.Random rng)
    {
        List<Vector2Int> waypoints = new List<Vector2Int>();
        
        // Add some padding to avoid waypoints at the very edge
        int padding = Mathf.Max(2, Mathf.Min(width, height) / 10);
        
        for (int i = 0; i < count; i++)
        {
            Vector2Int waypoint = new Vector2Int(
                rng.Next(padding, width - padding),
                rng.Next(padding, height - padding)
            );
            
            // Avoid placing waypoint on start position
            if (waypoint != startPosition)
            {
                waypoints.Add(waypoint);
            }
        }
        
        return waypoints;
    }
    
    /// <summary>
    /// Creates the spine path: Start → Waypoint1 → Waypoint2 → ... → Start
    /// </summary>
    private static void CreateSpinePath(HashSet<Vector2Int> spineCells, Vector2Int start, 
                                       List<Vector2Int> waypoints, int width, int height)
    {
        Vector2Int currentPos = start;
        
        // Visit each waypoint in sequence
        foreach (Vector2Int waypoint in waypoints)
        {
            DrawManhattanPath(spineCells, currentPos, waypoint, width, height);
            currentPos = waypoint;
        }
        
        // Return to start to close the loop
        DrawManhattanPath(spineCells, currentPos, start, width, height);
    }
    
    /// <summary>
    /// Draws a Manhattan distance path between two points (L-shaped: horizontal then vertical)
    /// </summary>
    private static void DrawManhattanPath(HashSet<Vector2Int> cells, Vector2Int from, Vector2Int to,
                                         int width, int height)
    {
        Vector2Int current = from;
        
        // Move horizontally first
        int xDirection = to.x > from.x ? 1 : -1;
        while (current.x != to.x)
        {
            cells.Add(current);
            current.x += xDirection;
        }
        
        // Then move vertically
        int yDirection = to.y > from.y ? 1 : -1;
        while (current.y != to.y)
        {
            cells.Add(current);
            current.y += yDirection;
        }
        
        // Add final cell
        cells.Add(current);
    }
    
    /// <summary>
    /// Buffs out the spine path by adding neighboring cells with variable radius and probability
    /// </summary>
    private static void BuffSpinePath(bool[,] layout, HashSet<Vector2Int> spineCells,
                                     int width, int height, int minRadius, int maxRadius,
                                     float orthogonalProb, float diagonalProb, System.Random rng)
    {
        // Create a copy of spine cells to iterate over (we'll be modifying layout)
        List<Vector2Int> spineList = new List<Vector2Int>(spineCells);
        
        foreach (Vector2Int spineCell in spineList)
        {
            // Random radius for this cell (creates organic variation)
            int radius = rng.Next(minRadius, maxRadius + 1);
            
            // Check all cells within radius
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // Skip the center cell (already marked)
                    if (dx == 0 && dy == 0)
                        continue;
                    
                    Vector2Int neighborPos = new Vector2Int(spineCell.x + dx, spineCell.y + dy);
                    
                    // Check if position is valid and not already filled
                    if (!IsValidPosition(neighborPos.x, neighborPos.y, width, height))
                        continue;
                    
                    if (layout[neighborPos.x, neighborPos.y])
                        continue;
                    
                    // Calculate Manhattan distance for falloff
                    int manhattanDist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (manhattanDist > radius)
                        continue;
                    
                    // Determine if this is orthogonal or diagonal neighbor
                    bool isOrthogonal = (dx == 0) != (dy == 0); // XOR: exactly one is zero
                    
                    // Roll probability based on neighbor type
                    float probability = isOrthogonal ? orthogonalProb : diagonalProb;
                    
                    // Add distance-based falloff (cells further from spine are less likely)
                    float distanceFalloff = 1.0f - ((float)manhattanDist / (radius + 1));
                    probability *= distanceFalloff;
                    
                    // Random roll
                    if (rng.NextDouble() < probability)
                    {
                        // Only place if at least one orthogonal neighbor is already true
                        if (HasOrthogonalNeighbor(layout, neighborPos.x, neighborPos.y, width, height))
                        {
                            layout[neighborPos.x, neighborPos.y] = true;
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Checks if a position is within grid bounds
    /// </summary>
    private static bool IsValidPosition(int x, int y, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    
    /// <summary>
    /// Checks if a position has at least one orthogonal neighbor that is already marked as true
    /// </summary>
    private static bool HasOrthogonalNeighbor(bool[,] layout, int x, int y, int width, int height)
    {
        // Check up
        if (IsValidPosition(x, y + 1, width, height) && layout[x, y + 1])
            return true;
        
        // Check down
        if (IsValidPosition(x, y - 1, width, height) && layout[x, y - 1])
            return true;
        
        // Check right
        if (IsValidPosition(x + 1, y, width, height) && layout[x + 1, y])
            return true;
        
        // Check left
        if (IsValidPosition(x - 1, y, width, height) && layout[x - 1, y])
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Debug utility: Visualizes the layout in console
    /// </summary>
    public static void DebugPrintLayout(bool[,] layout)
    {
        int width = layout.GetLength(0);
        int height = layout.GetLength(1);
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Layout ({width}x{height}):");
        
        // Print from top to bottom (reverse Y for readability)
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                sb.Append(layout[x, y] ? "█" : "·");
            }
            sb.AppendLine();
        }
        
        Debug.Log(sb.ToString());
    }
}
