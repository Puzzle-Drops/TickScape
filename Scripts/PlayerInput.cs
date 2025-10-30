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
        // Get the clickable layer mask
        int clickableLayer = LayerMask.NameToLayer("Clickable");
        if (clickableLayer == -1)
        {
            Debug.LogError("PlayerInput: 'Clickable' layer not found! Please create it in Tags and Layers.");
            return;
        }
        LayerMask clickableMask = 1 << clickableLayer;

        // Handle mouse hover
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

        // Update player's true tile (NOT lerped)
        tileHighlight.SetPlayerTile(player.gridPosition);

        // Update destination tile if exists
        tileHighlight.SetDestinationTile(player.pathTargetLocation);

        // Handle clicks
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100f, clickableMask))
            {
                Vector2Int gridPos = GridManager.Instance.WorldToGrid(hit.point);

                // ONLY allow movement to valid tiles that exist
                // Note: We validate tile existence here, but let Player.MoveTo handle walkability
                // This matches SDK pattern where click layer validates existence, movement layer handles pathability
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

        // Toggle running with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            player.running = !player.running;
            Debug.Log($"Running: {player.running}");
        }
    }


}
