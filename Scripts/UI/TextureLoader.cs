using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Safely loads UI sprites from Resources with fallback to colored rectangles.
/// SDK Reference: ImageLoader.createImage() in ImageLoader.ts
/// </summary>
public static class TextureLoader
{
    // Cache loaded textures to avoid repeated loads
    private static Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

    /// <summary>
    /// Load texture from Resources/UI folder.
    /// Returns colored fallback if texture missing.
    /// 
    /// Path format: "UI/Panels/inventory_panel" (no .png extension)
    /// </summary>
    public static Texture2D LoadTexture(string resourcePath, Color fallbackColor, int width = 64, int height = 64)
    {
        // Check cache first
        if (textureCache.ContainsKey(resourcePath))
        {
            return textureCache[resourcePath];
        }

        // Try loading from Resources
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);

        if (texture != null)
        {
            // Success - cache and return
            textureCache[resourcePath] = texture;
            return texture;
        }

        // Missing texture - ALWAYS log warning
        Debug.LogWarning($"[TextureLoader] ⚠️ MISSING: Resources/{resourcePath}.png");

        // Create fallback colored rectangle
        Texture2D fallback = CreateColoredTexture(width, height, fallbackColor);
        textureCache[resourcePath] = fallback;
        return fallback;
    }

    /// <summary>
    /// Create a solid-colored texture as fallback.
    /// </summary>
    private static Texture2D CreateColoredTexture(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point; // Pixel-perfect

        return texture;
    }

    /// <summary>
    /// Clear texture cache (call when changing scenes).
    /// </summary>
    public static void ClearCache()
    {
        textureCache.Clear();
        Debug.Log("[TextureLoader] Cache cleared");
    }
}