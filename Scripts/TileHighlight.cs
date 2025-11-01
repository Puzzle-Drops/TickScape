using UnityEngine;
using System.Collections.Generic;

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
        // Create hover highlight with customizable color
        hoverHighlight = CreateTileBorder("HoverHighlight", hoverMaterial, hoverColor);

        // Create player tile highlight with customizable color
        playerTileHighlight = CreateTileBorder("PlayerTileHighlight", playerTileMaterial, playerTileColor);

        // Create destination highlight with customizable color
        destinationHighlight = CreateTileBorder("DestinationHighlight", destinationMaterial, destinationColor);

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
    /// Create a tile border with a material template and runtime color override.
    /// </summary>
    /// <param name="name">Name for the GameObject</param>
    /// <param name="materialTemplate">Material asset to use as template (provides shader)</param>
    /// <param name="color">Color to apply at runtime (overrides material color)</param>
    /// <returns>GameObject with configured LineRenderer</returns>
    GameObject CreateTileBorder(string name, Material materialTemplate, Color color)
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

            Debug.Log($"[TileHighlight] Created '{name}' using material template '{materialTemplate.name}' " +
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

        // Set corner positions (square border)
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-0.5f, heightOffset, -0.5f),
            new Vector3(0.5f, heightOffset, -0.5f),
            new Vector3(0.5f, heightOffset, 0.5f),
            new Vector3(-0.5f, heightOffset, 0.5f),
            new Vector3(-0.5f, heightOffset, -0.5f) // Close the loop
        };

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        return border;
    }

    /// <summary>
    /// Update highlights for all NPCs (Units that aren't Players)
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

            // Create new highlight if needed
            if (!npcHighlights.ContainsKey(npc))
            {
                highlight = CreateTileBorder($"NPC_{npc.UnitName()}_Highlight", npcMaterial, npcTileColor);
                npcHighlights[npc] = highlight;
            }
            else
            {
                highlight = npcHighlights[npc];
            }

            // Update position based on NPC's true grid position
            Vector3 worldPos = GridManager.Instance.GridToWorld(npc.gridPosition);
            highlight.transform.position = worldPos;

            // Show/hide based on NPC state
            highlight.SetActive(!npc.IsDying());
        }
    }

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

    public void SetPlayerTile(Vector2Int gridPos)
    {
        if (GridManager.Instance != null)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
            playerTileHighlight.transform.position = worldPos;
            playerTileHighlight.SetActive(true);
        }
    }

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
