using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tile highlighting system with support for multi-tile mobs.
/// Matches SDK's rendering approach where outlines scale with unit size.
/// SDK Reference: CanvasSpriteModel.ts, GLTFModel.ts, TileMarkerModel.ts
/// </summary>
public class TileHighlight : MonoBehaviour
{
    [Header("Highlight Materials")]
    [Tooltip("Pre-made material assets - these ensure shaders are included in builds")]
    public Material hoverMaterial;
    public Material playerTileMaterial;
    public Material destinationMaterial;
    public Material npcMaterial;

    [Header("Highlight Colors (Runtime Customization)")]
    [Tooltip("These colors override the material colors at runtime")]
    public Color hoverColor = Color.white; // White
    public Color playerTileColor = new Color(1f, 0.84f, 0f); // Gold
    public Color destinationColor = Color.white; // White
    public Color npcTileColor = Color.red; // Red for NPCs

    [Header("Line Settings")]
    public float lineWidth = 0.05f;
    public float heightOffset = 0.075f; // Slightly above ground

    private GameObject hoverHighlight;
    private GameObject playerTileHighlight;
    private GameObject destinationHighlight;

    // NPC highlight management
    private Dictionary<Unit, GameObject> npcHighlights = new Dictionary<Unit, GameObject>();
    private List<Unit> currentNPCs = new List<Unit>();

    void Start()
    {
        // Create hover highlight (always 1x1) with customizable color
        hoverHighlight = CreateTileBorder("HoverHighlight", hoverMaterial, hoverColor, 1);

        // Create player tile highlight (always 1x1) with customizable color
        playerTileHighlight = CreateTileBorder("PlayerTileHighlight", playerTileMaterial, playerTileColor, 1);

        // Create destination highlight (always 1x1) with customizable color
        destinationHighlight = CreateTileBorder("DestinationHighlight", destinationMaterial, destinationColor, 1);

        // Initially hide all
        hoverHighlight.SetActive(false);
        playerTileHighlight.SetActive(false);
        destinationHighlight.SetActive(false);
    }

    void Update()
    {
        // Update NPC highlights every frame
        UpdateNPCHighlights();
    }

    /// <summary>
    /// Create a tile border with size-aware dimensions.
    /// Matches SDK pattern where outline geometry is created once based on unit size.
    /// SDK Reference: CanvasSpriteModel.ts lines 60-68, GLTFModel.ts lines 82-93
    /// </summary>
    /// <param name="name">Name for the GameObject</param>
    /// <param name="materialTemplate">Material asset to use as template (provides shader)</param>
    /// <param name="color">Color to apply at runtime (overrides material color)</param>
    /// <param name="size">Size in tiles (1 for 1x1, 2 for 2x2, etc.)</param>
    /// <returns>GameObject with configured LineRenderer</returns>
    GameObject CreateTileBorder(string name, Material materialTemplate, Color color, int size)
    {
        GameObject border = new GameObject(name);
        border.transform.parent = transform;

        LineRenderer lineRenderer = border.AddComponent<LineRenderer>();

        // Configure line renderer
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Use the pre-made material asset as template, apply custom color
        if (materialTemplate != null)
        {
            // Clone the material so each line can have independent properties
            Material instanceMaterial = new Material(materialTemplate);

            // Apply the custom color from inspector
            instanceMaterial.color = color;

            lineRenderer.material = instanceMaterial;

            Debug.Log($"[TileHighlight] Created '{name}' (size {size}x{size}) using material template '{materialTemplate.name}' " +
                     $"with shader '{materialTemplate.shader.name}' and custom color {color}");
        }
        else
        {
            Debug.LogError($"[TileHighlight] No material template assigned for '{name}'! " +
                          $"Tile highlight will be invisible. Please assign materials in Inspector.");

            // Emergency fallback: try to create material with built-in shader
            Shader fallbackShader = Shader.Find("Particles/Standard Unlit");
            if (fallbackShader != null)
            {
                Material fallbackMaterial = new Material(fallbackShader);
                fallbackMaterial.color = color;
                lineRenderer.material = fallbackMaterial;
                Debug.LogWarning($"[TileHighlight] Using fallback shader for '{name}'");
            }
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;

        // Set high sorting order to ensure it renders on top
        lineRenderer.sortingOrder = 100;

        // ===== CRITICAL: Calculate corners based on size =====
        // SDK creates outlines from (0, 0) to (size, -size) positioned at unit's SW corner
        // We maintain the current "centered with border" style for visual consistency:
        // - 1x1: (-0.5, -0.5) to (0.5, 0.5) - same as before ✅
        // - 2x2: (-0.5, -0.5) to (1.5, 1.5) - covers 2x2 area with border ✅
        // - 3x3: (-0.5, -0.5) to (2.5, 2.5) - covers 3x3 area with border ✅
        // Formula: from (-0.5, -0.5) to (size - 0.5, size - 0.5)
        
        float halfTile = 0.5f;
        float extent = size - halfTile;  // For size=2: extent = 1.5

        Vector3[] positions = new Vector3[]
        {
            new Vector3(-halfTile, heightOffset, -halfTile),  // SW corner
            new Vector3(extent, heightOffset, -halfTile),     // SE corner
            new Vector3(extent, heightOffset, extent),        // NE corner
            new Vector3(-halfTile, heightOffset, extent),     // NW corner
            new Vector3(-halfTile, heightOffset, -halfTile)   // Close the loop
        };

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        return border;
    }

    /// <summary>
    /// Update highlights for all NPCs (Units that aren't Players).
    /// Creates size-appropriate highlights for each NPC once, then reuses them.
    /// SDK Reference: SDK creates outline once in constructor, updates position in draw()
    /// </summary>
    void UpdateNPCHighlights()
    {
        if (GridManager.Instance == null)
            return;

        // Get all current NPCs (Units that aren't Players)
        currentNPCs.Clear();
        foreach (Entity entity in GridManager.Instance.GetAllEntities())
        {
            if (entity is Unit unit && !(entity is Player))
            {
                currentNPCs.Add(unit);
            }
        }

        // Remove highlights for NPCs that no longer exist
        List<Unit> toRemove = new List<Unit>();
        foreach (var kvp in npcHighlights)
        {
            if (!currentNPCs.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (Unit unit in toRemove)
        {
            npcHighlights.Remove(unit);
        }

        // Add or update highlights for current NPCs
        foreach (Unit npc in currentNPCs)
        {
            GameObject highlight;

            // Create new highlight if needed (ONCE, with correct size)
            // SDK Reference: CanvasSpriteModel constructor creates outline once with size
            if (!npcHighlights.ContainsKey(npc))
            {
                // CRITICAL: Pass npc.size to create size-appropriate highlight
                highlight = CreateTileBorder($"NPC_{npc.UnitName()}_Highlight", 
                                            npcMaterial, npcTileColor, npc.size);
                npcHighlights[npc] = highlight;
            }
            else
            {
                // Reuse existing highlight (SDK pattern: geometry created once, just update position)
                highlight = npcHighlights[npc];
            }

            // Update position based on NPC's SW corner (gridPosition)
            // SDK Reference: outline.position.x = x; outline.position.z = y;
            // The LineRenderer corners are relative to this position and extend based on size
            Vector3 worldPos = GridManager.Instance.GridToWorld(npc.gridPosition);
            highlight.transform.position = worldPos;

            // Show/hide based on NPC state
            highlight.SetActive(!npc.IsDying());
        }
    }

    /// <summary>
    /// Set hover tile highlight (always 1x1).
    /// </summary>
    public void SetHoverTile(Vector2Int? gridPos)
    {
        if (gridPos.HasValue && GridManager.Instance != null)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos.Value);
            hoverHighlight.transform.position = worldPos;
            hoverHighlight.SetActive(true);
        }
        else
        {
            hoverHighlight.SetActive(false);
        }
    }

    /// <summary>
    /// Set player tile highlight (always 1x1).
    /// </summary>
    public void SetPlayerTile(Vector2Int gridPos)
    {
        if (GridManager.Instance != null)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
            playerTileHighlight.transform.position = worldPos;
            playerTileHighlight.SetActive(true);
        }
    }

    /// <summary>
    /// Set destination tile highlight (always 1x1).
    /// </summary>
    public void SetDestinationTile(Vector2Int? gridPos)
    {
        if (gridPos.HasValue && GridManager.Instance != null)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos.Value);
            destinationHighlight.transform.position = worldPos;
            destinationHighlight.SetActive(true);
        }
        else
        {
            destinationHighlight.SetActive(false);
        }
    }
}
