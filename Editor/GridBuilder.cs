using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GridBuilder : EditorWindow
{
    public GameObject tilePrefab;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float tileSpacing = 1f;
    public Transform parentObject;

    [MenuItem("Tools/Grid Builder")]
    public static void ShowWindow()
    {
        GetWindow<GridBuilder>("Grid Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);

        tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", tilePrefab, typeof(GameObject), false);
        gridWidth = EditorGUILayout.IntField("Grid Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Grid Height", gridHeight);
        tileSpacing = EditorGUILayout.FloatField("Tile Spacing", tileSpacing);
        parentObject = (Transform)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(Transform), true);

        if (GUILayout.Button("Generate Grid"))
        {
            GenerateGrid();
        }
    }

    void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Please assign a Tile Prefab!");
            return;
        }

        GameObject gridParent = parentObject != null ? parentObject.gameObject : new GameObject("Grid");

        // Add or get GridManager component on the parent
        GridManager gridManager = gridParent.GetComponent<GridManager>();
        if (gridManager == null)
        {
            gridManager = gridParent.AddComponent<GridManager>();
        }

        // Set grid dimensions in GridManager
        gridManager.gridWidth = gridWidth;
        gridManager.gridHeight = gridHeight;
        gridManager.tileSize = tileSpacing;

        // Dictionary to store tiles for registration
        Dictionary<Vector2Int, Tile> tileDict = new Dictionary<Vector2Int, Tile>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 position = new Vector3(x * tileSpacing, 0, z * tileSpacing);
                GameObject tileObj = PrefabUtility.InstantiatePrefab(tilePrefab, gridParent.transform) as GameObject;
                tileObj.transform.position = position;
                tileObj.name = $"Tile_{x}_{z}";
                tileObj.layer = 8;  // Set to layer 8 "Clickable"

                Tile tile = tileObj.GetComponent<Tile>();
                if (tile != null)
                {
                    // NOTE: gridPosition.x = world X, gridPosition.y = world Z
                    tile.gridPosition = new Vector2Int(x, z);

                    // Add to dictionary for GridManager
                    tileDict.Add(tile.gridPosition, tile);
                }
            }
        }

        // Register all tiles with GridManager
        gridManager.RegisterTiles(tileDict);

        Debug.Log($"Generated {gridWidth * gridHeight} tiles with GridManager!");
    }
}
