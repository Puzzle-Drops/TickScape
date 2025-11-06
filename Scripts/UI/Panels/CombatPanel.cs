using UnityEngine;

/// <summary>
/// Combat panel with attack styles, auto-retaliate, and special attack.
/// SDK Reference: CombatControls.ts
/// </summary>
public class CombatPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/combat_panel";
    protected override string TabTexturePath => "UI/Tabs/combat_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? UISettings.Instance.combatKey : KeyCode.F1;

    public override bool IsAvailable => true;

    // Button textures
    private Texture2D styleButtonTexture;
    private Texture2D styleButtonSelectedTexture;
    private Texture2D autoRetalButtonTexture;
    private Texture2D autoRetalButtonSelectedTexture;
    private Texture2D specialAttackBarTexture;

    // Text styles
    private GUIStyle weaponNameStyle;
    private GUIStyle weaponNameStyleShadow;
    private GUIStyle buttonTextStyle;
    private GUIStyle buttonTextStyleShadow;
    private GUIStyle specTextStyle;

    public override void Initialize()
    {
        base.Initialize();

        // Load button textures
        styleButtonTexture = TextureLoader.LoadTexture(
            "UI/Elements/attack_style_button",
            new Color(0.5f, 0.4f, 0.3f),
            70, 48
        );

        styleButtonSelectedTexture = TextureLoader.LoadTexture(
            "UI/Elements/attack_style_button_highlighted",
            new Color(0.7f, 0.6f, 0.4f),
            70, 48
        );

        autoRetalButtonTexture = TextureLoader.LoadTexture(
            "UI/Elements/auto_retal_button",
            new Color(0.5f, 0.4f, 0.3f),
            147, 38
        );

        autoRetalButtonSelectedTexture = TextureLoader.LoadTexture(
            "UI/Elements/auto_retal_button_highlighted",
            new Color(0.7f, 0.6f, 0.4f),
            147, 38
        );

        specialAttackBarTexture = TextureLoader.LoadTexture(
            "UI/Elements/special_attack_bar",
            new Color(0.3f, 0.2f, 0.2f),
            150, 24
        );

        // Text styles
        weaponNameStyle = UIFonts.CreateTextStyle(21, UIFonts.YellowText, TextAnchor.MiddleCenter);
        weaponNameStyleShadow = UIFonts.CreateShadowStyle(21, TextAnchor.MiddleCenter);

        buttonTextStyle = UIFonts.CreateTextStyle(14, UIFonts.YellowText, TextAnchor.MiddleCenter);
        buttonTextStyleShadow = UIFonts.CreateShadowStyle(14, TextAnchor.MiddleCenter);

        specTextStyle = UIFonts.CreateTextStyle(16, Color.black, TextAnchor.MiddleCenter);
    }

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null || player.equipment.weapon == null)
        {
            // Draw "No Weapon" message
            GUIStyle noWeaponStyle = new GUIStyle();
            noWeaponStyle.font = UIFonts.VT323;
            noWeaponStyle.fontSize = Mathf.RoundToInt(18 * scale);
            noWeaponStyle.normal.textColor = Color.gray;
            noWeaponStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(x, y + 100 * scale, 204 * scale, 50 * scale), "No Weapon\nEquipped", noWeaponStyle);
            return;
        }

        Weapon weapon = player.equipment.weapon;

        // Update font sizes
        weaponNameStyle.fontSize = Mathf.RoundToInt(21 * scale);
        weaponNameStyleShadow.fontSize = weaponNameStyle.fontSize;
        buttonTextStyle.fontSize = Mathf.RoundToInt(16 * scale);
        buttonTextStyleShadow.fontSize = buttonTextStyle.fontSize;
        specTextStyle.fontSize = buttonTextStyle.fontSize;

        // Draw weapon name
        string weaponName = FormatWeaponName(weapon.itemName.ToString());
        UIFonts.DrawShadowedText(new Rect(x + 64 * scale, y + 20 * scale, 100, 20), weaponName, weaponNameStyle, scale);

        // Draw attack style buttons (up to 4 styles)
        Vector2[] stylePositions = new Vector2[]
        {
            new Vector2(25, 45),
            new Vector2(105, 45),
            new Vector2(25, 100),
            new Vector2(105, 100)
        };

        AttackStyle currentStyle = weapon.GetAttackStyle();
        AttackStyle[] availableStyles = GetAvailableStyles(weapon);

        for (int i = 0; i < availableStyles.Length && i < 4; i++)
        {
            DrawAttackStyleButton(
                x + stylePositions[i].x * scale,
                y + stylePositions[i].y * scale,
                scale,
                availableStyles[i],
                availableStyles[i] == currentStyle
            );
        }

        // Draw auto-retaliate button
        DrawAutoRetaliateButton(x + 25 * scale, y + 155 * scale, scale, player.autoRetaliate);

        // Draw special attack bar (if weapon has special attack)
        if (weapon.HasSpecialAttack())
        {
            DrawSpecialAttackBar(x + 25 * scale, y + 210 * scale, scale, player);
        }
    }

    /// <summary>
    /// Draw attack style button.
    /// SDK Reference: CombatControls.drawAttackStyleButton() in CombatControls.ts lines 77-118
    /// </summary>
    private void DrawAttackStyleButton(float x, float y, float scale, AttackStyle style, bool isSelected)
    {
        Texture2D buttonTex = isSelected ? styleButtonSelectedTexture : styleButtonTexture;
        Rect buttonRect = new Rect(x, y, 70 * scale, 48 * scale);

        GUI.DrawTexture(buttonRect, buttonTex);

        // Draw style name
        string styleName = style.ToString();
        UIFonts.DrawShadowedText(new Rect(x - 1 * scale, y + 14 * scale, 70 * scale, 20), styleName, buttonTextStyle, scale);
    }

    /// <summary>
    /// Draw auto-retaliate button.
    /// SDK Reference: CombatControls.draw() auto-retaliate section in CombatControls.ts lines 120-132
    /// </summary>
    private void DrawAutoRetaliateButton(float x, float y, float scale, bool isActive)
    {
        Texture2D buttonTex = isActive ? autoRetalButtonSelectedTexture : autoRetalButtonTexture;
        Rect buttonRect = new Rect(x, y, 147 * scale, 38 * scale);

        GUI.DrawTexture(buttonRect, buttonTex);

        // Draw text
        UIFonts.DrawShadowedText(new Rect(x + 15 * scale, y + 11 * scale, 147 * scale, 20), "Auto Retaliate", buttonTextStyle, scale);
    }

    /// <summary>
    /// Draw special attack bar.
    /// SDK Reference: CombatControls.draw() special attack section in CombatControls.ts lines 134-163
    /// </summary>
    private void DrawSpecialAttackBar(float x, float y, float scale, Player player)
    {
        Rect barRect = new Rect(x, y, 150 * scale, 24 * scale);
        GUI.DrawTexture(barRect, specialAttackBarTexture);

        // Get special attack percentage
        PlayerStats pStats = player.currentStats as PlayerStats;
        float specPercent = pStats != null ? pStats.specialAttack / 100f : 1f;

        // Draw energy bar (red background, green fill)
        Rect innerRect = new Rect(x + 3 * scale, y + 6 * scale, 144 * scale, 14 * scale);

        // Red background
        Color oldColor = GUI.color;
        GUI.color = new Color(0.45f, 0.02f, 0.02f);
        GUI.DrawTexture(innerRect, Texture2D.whiteTexture);

        // Green fill
        Rect fillRect = new Rect(innerRect.x, innerRect.y, innerRect.width * specPercent, innerRect.height);
        GUI.color = new Color(0.22f, 0.49f, 0.23f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

        GUI.color = oldColor;

        // Draw text
        bool isUsingSpec = player.useSpecialAttack;
        specTextStyle.normal.textColor = isUsingSpec ? Color.yellow : Color.black;

        int specAmount = pStats != null ? pStats.specialAttack : 100;
        string specText = $"Special Attack: {specAmount}%";
        GUI.Label(new Rect(x + 2 * scale, y + 6 * scale, 150 * scale, 20), specText, specTextStyle);
    }

    /// <summary>
    /// Handle panel clicks for attack styles, auto-retaliate, special attack.
    /// SDK Reference: CombatControls.panelClickDown() in CombatControls.ts lines 48-75
    /// </summary>
    public override void OnPanelClickDown(float relativeX, float relativeY)
    {
        Player player = FindPlayer();
        if (player == null || player.equipment.weapon == null) return;

        Weapon weapon = player.equipment.weapon;

        // Check attack style button clicks
        Vector2[] stylePositions = new Vector2[]
        {
            new Vector2(25, 45),
            new Vector2(105, 45),
            new Vector2(25, 100),
            new Vector2(105, 100)
        };

        AttackStyle[] availableStyles = GetAvailableStyles(weapon);

        for (int i = 0; i < availableStyles.Length && i < 4; i++)
        {
            if (relativeX > stylePositions[i].x && relativeX < stylePositions[i].x + 70 &&
                relativeY > stylePositions[i].y && relativeY < stylePositions[i].y + 48)
            {
                SetAttackStyle(weapon, availableStyles[i]);
                return;
            }
        }

        // Check auto-retaliate button
        if (relativeX > 28 && relativeX < 175 && relativeY > 160 && relativeY < 200)
        {
            player.autoRetaliate = !player.autoRetaliate;
            Debug.Log($"[CombatPanel] Auto-retaliate: {player.autoRetaliate}");
            return;
        }

        // Check special attack bar
        if (weapon.HasSpecialAttack())
        {
            if (relativeX > 25 && relativeX < 175 && relativeY > 210 && relativeY < 234)
            {
                player.ToggleSpecialAttack();
                Debug.Log($"[CombatPanel] Special attack: {player.useSpecialAttack}");
                return;
            }
        }
    }

    /// <summary>
    /// Get available attack styles for weapon.
    /// TODO: This should come from weapon data, using simplified version for now.
    /// </summary>
    private AttackStyle[] GetAvailableStyles(Weapon weapon)
    {
        if (weapon.weaponType == AttackStyleType.RANGED)
        {
            return new AttackStyle[] { AttackStyle.ACCURATE, AttackStyle.RAPID, AttackStyle.LONGRANGE };
        }
        else if (weapon.weaponType == AttackStyleType.MAGIC)
        {
            return new AttackStyle[] { AttackStyle.ACCURATE };
        }
        else // Melee
        {
            return new AttackStyle[] { AttackStyle.ACCURATE, AttackStyle.AGGRESSIVE, AttackStyle.DEFENSIVE };
        }
    }

    /// <summary>
    /// Set attack style for weapon.
    /// </summary>
    private void SetAttackStyle(Weapon weapon, AttackStyle style)
    {
        AttackStylesController.Instance.SetAttackStyle(weapon.weaponType, style);
        Debug.Log($"[CombatPanel] Attack style set to: {style}");
    }

    /// <summary>
    /// Format weapon name for display (convert DRAGON_SCIMITAR to Dragon Scimitar).
    /// </summary>
    private string FormatWeaponName(string name)
    {
        name = name.Replace("_", " ");
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
    }

    private Player FindPlayer()
    {
        return Object.FindAnyObjectByType<Player>();
    }

}