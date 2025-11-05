using UnityEngine;
using UnityEditor;

public class TileColorVariance : EditorWindow
{
    public Transform gridParent;
    public float varianceAmount = 0.1f;

    [MenuItem("Tools/Tile Color Variance")]
    public static void ShowWindow()
    {
        GetWindow<TileColorVariance>("Tile Color Variance");
    }

    void OnGUI()
    {
        GUILayout.Label("Randomize Tile Colors", EditorStyles.boldLabel);

        gridParent = (Transform)EditorGUILayout.ObjectField("Grid Parent", gridParent, typeof(Transform), true);
        varianceAmount = EditorGUILayout.Slider("Variance Amount", varianceAmount, 0f, 0.5f);

        if (GUILayout.Button("Apply Color Variance"))
        {
            ApplyVariance();
        }

        if (GUILayout.Button("Reset to Original Colors"))
        {
            ResetColors();
        }
    }

    void ApplyVariance()
    {
        if (gridParent == null)
        {
            Debug.LogError("Please assign a Grid Parent!");
            return;
        }

        Tile[] tiles = gridParent.GetComponentsInChildren<Tile>();

        if (tiles.Length == 0)
        {
            Debug.LogError("No tiles found in children!");
            return;
        }

        Random.InitState(System.DateTime.Now.Millisecond);

        foreach (Tile tile in tiles)
        {
            Color originalColor = tile.tileColor;

            float r = originalColor.r + Random.Range(-varianceAmount, varianceAmount);
            float g = originalColor.g + Random.Range(-varianceAmount, varianceAmount);
            float b = originalColor.b + Random.Range(-varianceAmount, varianceAmount);

            tile.tileColor = new Color(
                Mathf.Clamp01(r),
                Mathf.Clamp01(g),
                Mathf.Clamp01(b),
                originalColor.a
            );

            EditorUtility.SetDirty(tile);
        }

        Debug.Log($"Applied ±{varianceAmount} color variance to {tiles.Length} tiles!");
    }

    void ResetColors()
    {
        if (gridParent == null)
        {
            Debug.LogError("Please assign a Grid Parent!");
            return;
        }

        Tile[] tiles = gridParent.GetComponentsInChildren<Tile>();

        foreach (Tile tile in tiles)
        {
            tile.tileColor = Color.white;
            EditorUtility.SetDirty(tile);
        }

        Debug.Log($"Reset {tiles.Length} tiles to white!");
    }
}
