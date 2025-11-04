using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current game tick number on screen (top-middle).
/// For debugging movement, pathfinding, and tile walkability issues.
/// </summary>
[RequireComponent(typeof(Text))]
public class TickCounterDisplay : MonoBehaviour
{
    private Text textComponent;

    [Header("Display Settings")]
    [Tooltip("Update text every frame or only when tick changes?")]
    public bool updateEveryFrame = true;

    [Tooltip("Show tick percent (interpolation progress)?")]
    public bool showTickPercent = true;

    private int lastTickCounter = -1;

    void Start()
    {
        textComponent = GetComponent<Text>();

        if (textComponent == null)
        {
            Debug.LogError("TickCounterDisplay: No Text component found!");
            enabled = false;
            return;
        }

        // Set default style
        textComponent.fontSize = 24;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;

        // Add outline for readability
        var outline = gameObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);
        }
    }

    void Update()
    {
        if (WorldManager.Instance == null)
        {
            textComponent.text = "Waiting for WorldManager...";
            return;
        }

        int currentTick = WorldManager.Instance.GetTickCounter();

        // Only update if tick changed (or if updateEveryFrame is true)
        if (updateEveryFrame || currentTick != lastTickCounter)
        {
            string displayText = $"{currentTick}";

            if (showTickPercent)
            {
                float tickPercent = WorldManager.Instance.GetTickPercent();
                displayText += $" ({tickPercent:P0})";
            }

            textComponent.text = displayText;
            lastTickCounter = currentTick;
        }
    }
}
