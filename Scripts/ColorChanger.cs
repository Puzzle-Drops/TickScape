using UnityEngine;

[ExecuteInEditMode]
public class ColorChanger : MonoBehaviour
{
    [SerializeField]
    private Color objectColor = Color.white;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        ApplyColor();
    }

    void OnValidate()
    {
        // Apply color whenever the inspector values change
        ApplyColor();
    }

    void ApplyColor()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            // Use a property block to avoid creating tons of material instances
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", objectColor); // URP uses _BaseColor
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    // Public method to change color from other scripts if needed
    public void SetColor(Color newColor)
    {
        objectColor = newColor;
        ApplyColor();
    }

    public Color GetColor()
    {
        return objectColor;
    }
}
