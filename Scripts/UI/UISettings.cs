using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI-specific settings matching SDK's Settings class.
/// SDK Reference: Settings.ts (UI-related fields)
/// </summary>
public class UISettings : MonoBehaviour
{
    public static UISettings Instance { get; private set; }

    [Header("Scale Settings")]
    [Tooltip("Maximum UI scale (user preference, 0.5 - 3.0)")]
    [Range(0.5f, 3.0f)]
    public float maxUiScale = 1.5f;

    [Tooltip("Calculated control panel scale (set automatically)")]
    public float controlPanelScale = 1.0f;

    [Tooltip("Minimap scale")]
    public float minimapScale = 1.0f;

    [Header("Input Settings")]
    [Tooltip("Input delay in milliseconds (simulates network lag, 0-200ms)")]
    [Range(0, 200)]
    public int inputDelay = 20;

    [Header("Key Bindings")]
    public KeyCode inventoryKey = KeyCode.Tab;
    public KeyCode spellbookKey = KeyCode.F6;
    public KeyCode prayerKey = KeyCode.F5;
    public KeyCode equipmentKey = KeyCode.F4;
    public KeyCode combatKey = KeyCode.F1;

    [Header("Audio Settings")]
    [Tooltip("Master volume (0.0 - 1.0)")]
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Tooltip("Audio enabled (always true now)")]
    public bool playsAudio = true;

    [Tooltip("Area audio enabled (always true now)")]
    public bool playsAreaAudio = true;

    [Header("Gameplay Settings")]
    [Tooltip("Enable metronome tick sound")]
    public bool metronome = false;

    [Tooltip("Enable WASD camera controls")]
    public bool wasdCamera = false;

    [Header("UI State")]
    [Tooltip("Is a key currently being bound?")]
    public bool isKeybinding = false;

    [Header("Prayer Grid Positions")]
    [Tooltip("Custom grid positions for prayers (PrayerType -> Vector2Int)")]
    public Dictionary<PrayerType, Vector2Int> prayerGridPositions = new Dictionary<PrayerType, Vector2Int>();

    [Header("Spell Grid Positions")]
    [Tooltip("Custom grid positions for spells (SpellType -> Vector2Int)")]
    public Dictionary<SpellType, Vector2Int> spellGridPositions = new Dictionary<SpellType, Vector2Int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize with default positions
        InitializeDefaultPrayerPositions();
        InitializeDefaultSpellPositions();
    }

    /// <summary>
    /// Initialize default prayer grid positions.
    /// Grid is 5 columns (0-4) by 6 rows (0-5).
    /// </summary>
    private void InitializeDefaultPrayerPositions()
    {
        if (prayerGridPositions == null || prayerGridPositions.Count == 0)
        {
            prayerGridPositions = new Dictionary<PrayerType, Vector2Int>();
            ResetPrayerPositionsToDefault();
        }
    }

    /// <summary>
    /// Initialize default spell grid positions.
    /// Grid is 4 columns (0-3) by 7 rows (0-6).
    /// </summary>
    private void InitializeDefaultSpellPositions()
    {
        if (spellGridPositions == null || spellGridPositions.Count == 0)
        {
            spellGridPositions = new Dictionary<SpellType, Vector2Int>();
            ResetSpellPositionsToDefault();
        }
    }

    /// <summary>
    /// Reset all prayer positions to defaults.
    /// </summary>
    public void ResetPrayerPositionsToDefault()
    {
        prayerGridPositions.Clear();

        // Default positions as specified
        prayerGridPositions[PrayerType.PROTECT_FROM_MAGIC] = new Vector2Int(1, 3);
        prayerGridPositions[PrayerType.PROTECT_FROM_RANGE] = new Vector2Int(2, 3);
        prayerGridPositions[PrayerType.PROTECT_FROM_MELEE] = new Vector2Int(3, 3);
        prayerGridPositions[PrayerType.REDEMPTION] = new Vector2Int(2, 4);
        prayerGridPositions[PrayerType.PIETY] = new Vector2Int(1, 5);
        prayerGridPositions[PrayerType.RIGOUR] = new Vector2Int(2, 5);
        prayerGridPositions[PrayerType.AUGURY] = new Vector2Int(3, 5);
    }

    /// <summary>
    /// Reset all spell positions to defaults.
    /// </summary>
    public void ResetSpellPositionsToDefault()
    {
        spellGridPositions.Clear();

        // Default positions: Blood Barrage at (0, 5), Ice Barrage at (0, 6)
        spellGridPositions[SpellType.BLOOD_BARRAGE] = new Vector2Int(0, 5);
        spellGridPositions[SpellType.ICE_BARRAGE] = new Vector2Int(0, 6);
    }

    /// <summary>
    /// Get grid position for a prayer type.
    /// Returns default if not set.
    /// </summary>
    public Vector2Int GetPrayerGridPosition(PrayerType type)
    {
        if (prayerGridPositions.ContainsKey(type))
        {
            return prayerGridPositions[type];
        }

        // Return a default position if somehow missing
        Debug.LogWarning($"[UISettings] Prayer {type} has no grid position! Using (0,0)");
        return Vector2Int.zero;
    }

    /// <summary>
    /// Set grid position for a prayer type.
    /// </summary>
    public void SetPrayerGridPosition(PrayerType type, Vector2Int position)
    {
        prayerGridPositions[type] = position;
    }

    /// <summary>
    /// Swap positions of two prayers.
    /// </summary>
    public void SwapPrayerPositions(PrayerType prayer1, PrayerType prayer2)
    {
        if (!prayerGridPositions.ContainsKey(prayer1) || !prayerGridPositions.ContainsKey(prayer2))
            return;

        Vector2Int temp = prayerGridPositions[prayer1];
        prayerGridPositions[prayer1] = prayerGridPositions[prayer2];
        prayerGridPositions[prayer2] = temp;
    }

    /// <summary>
    /// Get grid position for a spell type.
    /// Returns default if not set.
    /// </summary>
    public Vector2Int GetSpellGridPosition(SpellType type)
    {
        if (spellGridPositions.ContainsKey(type))
        {
            return spellGridPositions[type];
        }

        // Return a default position if somehow missing
        Debug.LogWarning($"[UISettings] Spell {type} has no grid position! Using (0,0)");
        return Vector2Int.zero;
    }

    /// <summary>
    /// Set grid position for a spell type.
    /// </summary>
    public void SetSpellGridPosition(SpellType type, Vector2Int position)
    {
        spellGridPositions[type] = position;
    }

    /// <summary>
    /// Swap positions of two spells.
    /// </summary>
    public void SwapSpellPositions(SpellType spell1, SpellType spell2)
    {
        if (!spellGridPositions.ContainsKey(spell1) || !spellGridPositions.ContainsKey(spell2))
            return;

        Vector2Int temp = spellGridPositions[spell1];
        spellGridPositions[spell1] = spellGridPositions[spell2];
        spellGridPositions[spell2] = temp;
    }

    /// <summary>
    /// Check if running on mobile device.
    /// SDK Reference: Settings.mobileCheck()
    /// Currently returns false (desktop only).
    /// </summary>
    public static bool IsMobile()
    {
        // TODO: Add mobile detection when implementing mobile support
        return false;
    }

    /// <summary>
    /// Save settings to PlayerPrefs.
    /// SDK Reference: Settings.persistToStorage()
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("UI_MaxScale", maxUiScale);
        PlayerPrefs.SetFloat("UI_Volume", volume);
        PlayerPrefs.SetInt("UI_InputDelay", inputDelay);
        PlayerPrefs.SetInt("UI_InventoryKey", (int)inventoryKey);
        PlayerPrefs.SetInt("UI_SpellbookKey", (int)spellbookKey);
        PlayerPrefs.SetInt("UI_PrayerKey", (int)prayerKey);
        PlayerPrefs.SetInt("UI_EquipmentKey", (int)equipmentKey);
        PlayerPrefs.SetInt("UI_CombatKey", (int)combatKey);
        PlayerPrefs.SetInt("UI_Metronome", metronome ? 1 : 0);
        PlayerPrefs.SetInt("UI_WasdCamera", wasdCamera ? 1 : 0);

        // Save prayer and spell positions as serialized strings
        SavePrayerPositions();
        SaveSpellPositions();

        PlayerPrefs.Save();

        Debug.Log("[UI] Settings saved");
    }

    /// <summary>
    /// Save prayer positions to PlayerPrefs.
    /// Format: "TYPE:X,Y|TYPE:X,Y|..."
    /// </summary>
    private void SavePrayerPositions()
    {
        List<string> positionStrings = new List<string>();

        foreach (var kvp in prayerGridPositions)
        {
            string posStr = $"{(int)kvp.Key}:{kvp.Value.x},{kvp.Value.y}";
            positionStrings.Add(posStr);
        }

        string serialized = string.Join("|", positionStrings);
        PlayerPrefs.SetString("UI_PrayerPositions", serialized);
    }

    /// <summary>
    /// Save spell positions to PlayerPrefs.
    /// Format: "TYPE:X,Y|TYPE:X,Y|..."
    /// </summary>
    private void SaveSpellPositions()
    {
        List<string> positionStrings = new List<string>();

        foreach (var kvp in spellGridPositions)
        {
            string posStr = $"{(int)kvp.Key}:{kvp.Value.x},{kvp.Value.y}";
            positionStrings.Add(posStr);
        }

        string serialized = string.Join("|", positionStrings);
        PlayerPrefs.SetString("UI_SpellPositions", serialized);
    }

    /// <summary>
    /// Load settings from PlayerPrefs.
    /// </summary>
    public void LoadSettings()
    {
        maxUiScale = PlayerPrefs.GetFloat("UI_MaxScale", 1.0f);
        volume = PlayerPrefs.GetFloat("UI_Volume", 1.0f);
        inputDelay = PlayerPrefs.GetInt("UI_InputDelay", 20);
        inventoryKey = (KeyCode)PlayerPrefs.GetInt("UI_InventoryKey", (int)KeyCode.Tab);
        spellbookKey = (KeyCode)PlayerPrefs.GetInt("UI_SpellbookKey", (int)KeyCode.F6);
        prayerKey = (KeyCode)PlayerPrefs.GetInt("UI_PrayerKey", (int)KeyCode.F5);
        equipmentKey = (KeyCode)PlayerPrefs.GetInt("UI_EquipmentKey", (int)KeyCode.F4);
        combatKey = (KeyCode)PlayerPrefs.GetInt("UI_CombatKey", (int)KeyCode.F1);
        metronome = PlayerPrefs.GetInt("UI_Metronome", 0) == 1;
        wasdCamera = PlayerPrefs.GetInt("UI_WasdCamera", 0) == 1;

        // Always enable audio (no mute functionality)
        playsAudio = true;
        playsAreaAudio = true;

        // Load prayer and spell positions
        LoadPrayerPositions();
        LoadSpellPositions();

        Debug.Log("[UI] Settings loaded");
    }

    /// <summary>
    /// Load prayer positions from PlayerPrefs.
    /// </summary>
    private void LoadPrayerPositions()
    {
        string serialized = PlayerPrefs.GetString("UI_PrayerPositions", "");

        if (string.IsNullOrEmpty(serialized))
        {
            // No saved positions, use defaults
            ResetPrayerPositionsToDefault();
            return;
        }

        try
        {
            prayerGridPositions.Clear();
            string[] positionStrings = serialized.Split('|');

            foreach (string posStr in positionStrings)
            {
                string[] parts = posStr.Split(':');
                if (parts.Length != 2) continue;

                int typeInt = int.Parse(parts[0]);
                PrayerType type = (PrayerType)typeInt;

                string[] coords = parts[1].Split(',');
                if (coords.Length != 2) continue;

                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);

                prayerGridPositions[type] = new Vector2Int(x, y);
            }

            Debug.Log($"[UISettings] Loaded {prayerGridPositions.Count} prayer positions");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UISettings] Failed to load prayer positions: {e.Message}");
            ResetPrayerPositionsToDefault();
        }
    }

    /// <summary>
    /// Load spell positions from PlayerPrefs.
    /// </summary>
    private void LoadSpellPositions()
    {
        string serialized = PlayerPrefs.GetString("UI_SpellPositions", "");

        if (string.IsNullOrEmpty(serialized))
        {
            // No saved positions, use defaults
            ResetSpellPositionsToDefault();
            return;
        }

        try
        {
            spellGridPositions.Clear();
            string[] positionStrings = serialized.Split('|');

            foreach (string posStr in positionStrings)
            {
                string[] parts = posStr.Split(':');
                if (parts.Length != 2) continue;

                int typeInt = int.Parse(parts[0]);
                SpellType type = (SpellType)typeInt;

                string[] coords = parts[1].Split(',');
                if (coords.Length != 2) continue;

                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);

                spellGridPositions[type] = new Vector2Int(x, y);
            }

            Debug.Log($"[UISettings] Loaded {spellGridPositions.Count} spell positions");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UISettings] Failed to load spell positions: {e.Message}");
            ResetSpellPositionsToDefault();
        }
    }

    void Start()
    {
        LoadSettings();
    }
}