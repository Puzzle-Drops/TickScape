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
    [Tooltip("Maximum UI scale (user preference, 0.5 - 2.0)")]
    [Range(0.5f, 2.0f)]
    public float maxUiScale = 1.0f;

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
    public bool playsAudio = true;
    public bool playsAreaAudio = true;
    public bool metronome = false;

    [Header("UI State")]
    [Tooltip("Is a key currently being bound?")]
    public bool isKeybinding = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        PlayerPrefs.SetInt("UI_InputDelay", inputDelay);
        PlayerPrefs.SetInt("UI_InventoryKey", (int)inventoryKey);
        PlayerPrefs.SetInt("UI_SpellbookKey", (int)spellbookKey);
        PlayerPrefs.SetInt("UI_PrayerKey", (int)prayerKey);
        PlayerPrefs.SetInt("UI_EquipmentKey", (int)equipmentKey);
        PlayerPrefs.SetInt("UI_CombatKey", (int)combatKey);
        PlayerPrefs.SetInt("UI_PlaysAudio", playsAudio ? 1 : 0);
        PlayerPrefs.SetInt("UI_PlaysAreaAudio", playsAreaAudio ? 1 : 0);
        PlayerPrefs.SetInt("UI_Metronome", metronome ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("[UI] Settings saved");
    }

    /// <summary>
    /// Load settings from PlayerPrefs.
    /// </summary>
    public void LoadSettings()
    {
        maxUiScale = PlayerPrefs.GetFloat("UI_MaxScale", 1.0f);
        inputDelay = PlayerPrefs.GetInt("UI_InputDelay", 20);
        inventoryKey = (KeyCode)PlayerPrefs.GetInt("UI_InventoryKey", (int)KeyCode.Tab);
        spellbookKey = (KeyCode)PlayerPrefs.GetInt("UI_SpellbookKey", (int)KeyCode.F6);
        prayerKey = (KeyCode)PlayerPrefs.GetInt("UI_PrayerKey", (int)KeyCode.F5);
        equipmentKey = (KeyCode)PlayerPrefs.GetInt("UI_EquipmentKey", (int)KeyCode.F4);
        combatKey = (KeyCode)PlayerPrefs.GetInt("UI_CombatKey", (int)KeyCode.F1);
        playsAudio = PlayerPrefs.GetInt("UI_PlaysAudio", 1) == 1;
        playsAreaAudio = PlayerPrefs.GetInt("UI_PlaysAreaAudio", 1) == 1;
        metronome = PlayerPrefs.GetInt("UI_Metronome", 0) == 1;

        Debug.Log("[UI] Settings loaded");
    }

    void Start()
    {
        LoadSettings();
    }
}