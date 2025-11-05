using UnityEngine;

[ExecuteInEditMode]
public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public Color tileColor = Color.white;
    public bool isWalkable = true;
    public bool blocksLineOfSight = false;

    private MeshRenderer meshRenderer;
    private Material materialInstance;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        ApplyColor();
    }

    void Start()
    {
        // Only register in play mode
        if (Application.isPlaying)
        {
            // CRITICAL FIX: Register this tile with GridManager at runtime
            if (GridManager.Instance != null)
            {
                GridManager.Instance.RegisterTile(this);
            }
            else
            {
                Debug.LogError($"Tile at {gridPosition}: GridManager.Instance is null!");
            }
        }
    }

    void OnValidate()
    {
        // Apply color whenever the inspector values change
        if (!Application.isPlaying)
        {
            ApplyColor();
        }
    }

    void ApplyColor()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            // In edit mode, use sharedMaterial for persistent changes
            if (!Application.isPlaying)
            {
                // Use a property block to avoid creating tons of material instances
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", tileColor); // URP uses _BaseColor, not _Color!
                meshRenderer.SetPropertyBlock(propBlock);
            }
            // In play mode, create material instances
            else
            {
                if (meshRenderer.sharedMaterial != null)
                {
                    materialInstance = meshRenderer.material; // Auto-creates instance
                    materialInstance.SetColor("_BaseColor", tileColor); // URP uses _BaseColor!
                }
            }
        }
    }
}
