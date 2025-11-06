using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    Player player;
    Camera mainCamera;
    TileHighlight tileHighlight;

    Vector2Int? lastHoveredTile = null;

    void Start()
    {
        player = GetComponent<Player>();
        mainCamera = Camera.main;

        // Find the pre-existing tile highlight system (must exist in scene)
        GameObject highlightObj = GameObject.Find("TileHighlightSystem");
        if (highlightObj == null)
        {
            Debug.LogError("PlayerInput: Could not find TileHighlightSystem in scene! " +
                          "Please create a GameObject named 'TileHighlightSystem' with TileHighlight component.");
            return;
        }

        tileHighlight = highlightObj.GetComponent<TileHighlight>();
        if (tileHighlight == null)
        {
            Debug.LogError("PlayerInput: TileHighlightSystem GameObject exists but has no TileHighlight component!");
            return;
        }

        Debug.Log("PlayerInput: Found TileHighlightSystem with materials assigned");
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Show exact hit point
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hit.point, 0.1f);

            // Show which tile it converts to
            Vector2Int gridPos = GridManager.Instance.WorldToGrid(hit.point);
            Vector3 tileCenter = GridManager.Instance.GridToWorld(gridPos);
            tileCenter += new Vector3(0.5f, 0, 0.5f); // Center of tile

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tileCenter, 0.15f);

            // Draw line between them
            Gizmos.color = Color.white;
            Gizmos.DrawLine(hit.point, tileCenter);
        }
    }

    void Update()
    {
        // Check if mouse is over UI
        bool mouseOverUI = UIManager.Instance != null && UIManager.Instance.IsMouseOverUI(Input.mousePosition);

        // Get the clickable layer mask
        int clickableLayer = LayerMask.NameToLayer("Clickable");
        if (clickableLayer == -1)
        {
            Debug.LogError("PlayerInput: 'Clickable' layer not found! Please create it in Tags and Layers.");
            return;
        }
        LayerMask clickableMask = 1 << clickableLayer;

        // Handle mouse hover (only if not over UI)
        if (!mouseOverUI)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableMask))
            {
                Vector2Int gridPos = GridManager.Instance.WorldToGrid(hit.point);

                // VALIDATE that a tile actually exists at this position
                Tile tile = GridManager.Instance.GetTileAt(gridPos);
                if (tile != null)
                {
                    // Only show highlight if tile exists
                    if (!lastHoveredTile.HasValue || lastHoveredTile.Value != gridPos)
                    {
                        tileHighlight.SetHoverTile(gridPos);
                        lastHoveredTile = gridPos;
                    }
                }
                else
                {
                    // Hit something but no tile there - clear highlight
                    if (lastHoveredTile.HasValue)
                    {
                        tileHighlight.SetHoverTile(null);
                        lastHoveredTile = null;
                    }
                }
            }
            else
            {
                // No hit at all - clear highlight
                if (lastHoveredTile.HasValue)
                {
                    tileHighlight.SetHoverTile(null);
                    lastHoveredTile = null;
                }
            }
        }
        else
        {
            // Mouse is over UI - clear any highlight
            if (lastHoveredTile.HasValue)
            {
                tileHighlight.SetHoverTile(null);
                lastHoveredTile = null;
            }
        }

        // Update player's true tile (NOT lerped)
        tileHighlight.SetPlayerTile(player.gridPosition);

        // Update destination tile if exists
        tileHighlight.SetDestinationTile(player.pathTargetLocation);

        // Handle clicks - ONLY if not over UI
        if (!mouseOverUI && Input.GetMouseButtonDown(0)) // Left click
        {
            Debug.Log("[PlayerInput] Processing click - UI check passed");
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableMask))
            {
                Vector2Int gridPos = GridManager.Instance.WorldToGrid(hit.point);

                // ONLY allow movement to valid tiles that exist
                Tile tile = GridManager.Instance.GetTileAt(gridPos);
                if (tile != null)
                {
                    player.MoveTo(gridPos.x, gridPos.y);
                    Debug.Log($"Clicked tile {gridPos}");
                }
                else
                {
                    Debug.LogWarning($"Clicked outside valid grid bounds: {gridPos}");
                }
            }
        }
        else if (mouseOverUI && Input.GetMouseButtonDown(0))
        {
            //Debug.Log("[PlayerInput] BLOCKED click - mouse over UI");
        }

    }

}
