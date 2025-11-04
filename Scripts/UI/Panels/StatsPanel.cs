using UnityEngine;

/// <summary>
/// Stats panel showing equipment bonuses and max hit.
/// SDK Reference: StatsControls.ts
/// </summary>
public class StatsPanel : BasePanel
{
    protected override string PanelTexturePath => "UI/Panels/stats_panel";
    protected override string TabTexturePath => "UI/Tabs/stats_tab";

    public override KeyCode KeyBinding =>
        UISettings.Instance != null ? KeyCode.F2 : KeyCode.None;

    public override bool IsAvailable => true;
    public override bool AppearsOnLeftInMobile => false;

    // Text styles
    private GUIStyle headerStyle;
    private GUIStyle columnHeaderStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;

    public override void Initialize()
    {
        base.Initialize();

        // Header style (EQUIPMENT BONUSES, OTHER BONUSES)
        headerStyle = UIFonts.CreateTextStyle(18, UIFonts.YellowText, TextAnchor.MiddleCenter);
        headerStyle.font = UIFonts.VT323;

        // Column headers (STYLE, ATTACK, DEFENCE, STRENGTH)
        columnHeaderStyle = UIFonts.CreateTextStyle(14, UIFonts.YellowText, TextAnchor.MiddleLeft);
        columnHeaderStyle.font = UIFonts.VT323;

        // Labels (Stab:, Slash:, Max Hit:)
        labelStyle = UIFonts.CreateTextStyle(16, UIFonts.YellowText, TextAnchor.MiddleLeft);
        labelStyle.font = UIFonts.VT323;

        // Values (+75, +50, 32)
        valueStyle = UIFonts.CreateTextStyle(16, UIFonts.WhiteText, TextAnchor.MiddleRight);
        valueStyle.font = UIFonts.VT323;
    }

    public override void DrawPanel(float x, float y, float scale)
    {
        base.DrawPanel(x, y, scale);

        Player player = FindPlayer();
        if (player == null) return;

        // Update font sizes and ensure VT323 is set
        headerStyle.fontSize = Mathf.RoundToInt(18 * scale);
        headerStyle.font = UIFonts.VT323;
        
        columnHeaderStyle.fontSize = Mathf.RoundToInt(14 * scale);
        columnHeaderStyle.font = UIFonts.VT323;
        
        labelStyle.fontSize = Mathf.RoundToInt(16 * scale);
        labelStyle.font = UIFonts.VT323;
        
        valueStyle.fontSize = Mathf.RoundToInt(16 * scale);
        valueStyle.font = UIFonts.VT323;

        float currentY = y + 10 * scale;

        // Draw "EQUIPMENT BONUSES" header
        UIFonts.DrawShadowedText(
            new Rect(x, currentY, 204 * scale, 20),
            "EQUIPMENT BONUSES",
            headerStyle,
            scale
        );
        currentY += 25 * scale;

        // Draw equipment bonuses table
        currentY = DrawEquipmentBonuses(player, x, currentY, scale);

        // Add spacing
        currentY += 10 * scale;

        // Draw "OTHER BONUSES" header
        UIFonts.DrawShadowedText(
            new Rect(x, currentY, 204 * scale, 20),
            "OTHER BONUSES",
            headerStyle,
            scale
        );
        currentY += 25 * scale;

        // Draw other bonuses section
        DrawOtherBonuses(player, x, currentY, scale);
    }

    /// <summary>
    /// Draw the 4-column equipment bonuses table.
    /// Returns the Y position after the table.
    /// </summary>
    private float DrawEquipmentBonuses(Player player, float x, float y, float scale)
    {
        // Column positions (adjusted for 204px panel width)
        float styleX = x + 10 * scale;
        float attackX = x + 60 * scale;
        float defenceX = x + 110 * scale;
        float strengthX = x + 165 * scale;

        float currentY = y;

        // Draw column headers
        DrawColumnHeader("STYLE", styleX, currentY, scale, TextAnchor.MiddleLeft);
        DrawColumnHeader("ATK", attackX, currentY, scale, TextAnchor.MiddleRight);
        DrawColumnHeader("DEF", defenceX, currentY, scale, TextAnchor.MiddleRight);
        DrawColumnHeader("STR", strengthX, currentY, scale, TextAnchor.MiddleRight);
        currentY += 18 * scale;

        // Draw each combat style row
        DrawBonusRow("Stab:", player.bonuses.attack.stab, player.bonuses.defence.stab,
            player.bonuses.other.meleeStrength, false, styleX, attackX, defenceX, strengthX, currentY, scale);
        currentY += 20 * scale;

        DrawBonusRow("Slash:", player.bonuses.attack.slash, player.bonuses.defence.slash,
            player.bonuses.other.meleeStrength, false, styleX, attackX, defenceX, strengthX, currentY, scale);
        currentY += 20 * scale;

        DrawBonusRow("Crush:", player.bonuses.attack.crush, player.bonuses.defence.crush,
            player.bonuses.other.meleeStrength, false, styleX, attackX, defenceX, strengthX, currentY, scale);
        currentY += 20 * scale;

        DrawBonusRow("Range:", player.bonuses.attack.range, player.bonuses.defence.range,
            player.bonuses.other.rangedStrength, false, styleX, attackX, defenceX, strengthX, currentY, scale);
        currentY += 20 * scale;

        DrawBonusRow("Magic:", player.bonuses.attack.magic, player.bonuses.defence.magic,
            0, true, styleX, attackX, defenceX, strengthX, currentY, scale);
        currentY += 20 * scale;

        return currentY;
    }

    /// <summary>
    /// Draw a single row of the equipment bonuses table.
    /// </summary>
    private void DrawBonusRow(string style, int attackBonus, int defenceBonus, int strengthBonus,
        bool isMagicRow, float styleX, float attackX, float defenceX, float strengthX, float y, float scale)
    {
        // Style label
        UIFonts.DrawShadowedText(
            new Rect(styleX, y, 50 * scale, 20),
            style,
            labelStyle,
            scale
        );

        // Attack bonus
        UIFonts.DrawShadowedText(
            new Rect(attackX - 40 * scale, y, 40 * scale, 20),
            FormatBonus(attackBonus),
            valueStyle,
            scale
        );

        // Defence bonus
        UIFonts.DrawShadowedText(
            new Rect(defenceX - 40 * scale, y, 40 * scale, 20),
            FormatBonus(defenceBonus),
            valueStyle,
            scale
        );

        // Strength bonus (special formatting for magic)
        string strengthText;
        if (isMagicRow)
        {
            // Magic damage as percentage
            Player player = FindPlayer();
            if (player != null)
            {
                float magicDamage = player.bonuses.other.magicDamage;
                int magicPercent = Mathf.RoundToInt((magicDamage - 1f) * 100f);
                strengthText = FormatBonus(magicPercent) + "%";
            }
            else
            {
                strengthText = "+0%";
            }
        }
        else
        {
            strengthText = FormatBonus(strengthBonus);
        }

        UIFonts.DrawShadowedText(
            new Rect(strengthX - 40 * scale, y, 40 * scale, 20),
            strengthText,
            valueStyle,
            scale
        );
    }

    /// <summary>
    /// Draw column header.
    /// </summary>
    private void DrawColumnHeader(string text, float x, float y, float scale, TextAnchor alignment)
    {
        GUIStyle style = new GUIStyle(columnHeaderStyle);
        style.alignment = alignment;
        style.font = UIFonts.VT323;

        UIFonts.DrawShadowedText(
            new Rect(x - (alignment == TextAnchor.MiddleRight ? 40 * scale : 0), y, 40 * scale, 20),
            text,
            style,
            scale
        );
    }

    /// <summary>
    /// Draw the Other Bonuses section with max hit and prayer bonus.
    /// Each on its own row.
    /// </summary>
    private void DrawOtherBonuses(Player player, float x, float y, float scale)
    {
        float labelX = x + 10 * scale;
        float valueX = x + 194 * scale; // Right-aligned
        float currentY = y;

        // Calculate max hits
        int baseMaxHit = CalculateMaxHit(player, false);
        int prayerMaxHit = CalculateMaxHit(player, true);

        // Row 1: Max Hit
        UIFonts.DrawShadowedText(
            new Rect(labelX, currentY, 100 * scale, 20),
            "Max Hit:",
            labelStyle,
            scale
        );
        UIFonts.DrawShadowedText(
            new Rect(valueX - 50 * scale, currentY, 50 * scale, 20),
            baseMaxHit.ToString(),
            valueStyle,
            scale
        );
        currentY += 20 * scale;

        // Row 2: Max w/ Prayer (only show if different from base)
        if (prayerMaxHit != baseMaxHit)
        {
            UIFonts.DrawShadowedText(
                new Rect(labelX, currentY, 120 * scale, 20),
                "Max w/ Prayer:",
                labelStyle,
                scale
            );
            UIFonts.DrawShadowedText(
                new Rect(valueX - 50 * scale, currentY, 50 * scale, 20),
                prayerMaxHit.ToString(),
                valueStyle,
                scale
            );
            currentY += 20 * scale;
        }

        // Row 3: Prayer Bonus
        UIFonts.DrawShadowedText(
            new Rect(labelX, currentY, 100 * scale, 20),
            "Prayer:",
            labelStyle,
            scale
        );
        UIFonts.DrawShadowedText(
            new Rect(valueX - 50 * scale, currentY, 50 * scale, 20),
            FormatBonus(player.bonuses.other.prayer),
            valueStyle,
            scale
        );
    }

    /// <summary>
    /// Calculate max hit for player.
    /// </summary>
    private int CalculateMaxHit(Player player, bool withPrayers)
    {
        if (player.equipment.weapon == null)
        {
            return 0; // No weapon = 0 max hit
        }

        // Create attack bonuses
        AttackBonuses bonuses = new AttackBonuses
        {
            styleBonus = 0,
            isAccurate = false,
            styleStrengthBonus = 0,
            voidMultiplier = 1.0f,
            gearMeleeMultiplier = 1.0f,
            gearMageMultiplier = 1.0f,
            gearRangeMultiplier = 1.0f,
            attackStyle = "slash",
            magicBaseSpellDamage = 0f,
            overallMultiplier = 1.0f,
            effectivePrayers = null,
            isSpecialAttack = false
        };

        // Calculate prayer effects if requested
        if (withPrayers && player.prayerController != null)
        {
            bonuses.effectivePrayers = new EffectivePrayers();

            // Get active prayers based on weapon type
            if (player.equipment.weapon.weaponType == AttackStyleType.MELEE)
            {
                Prayer strength = player.prayerController.MatchGroup(PrayerGroup.STRENGTH);
                if (strength != null && strength.isActive)
                {
                    bonuses.effectivePrayers.strength = strength;
                }
            }
            else if (player.equipment.weapon.weaponType == AttackStyleType.RANGED)
            {
                Prayer range = player.prayerController.MatchGroup(PrayerGroup.ACCURACY);
                if (range != null && range.isActive)
                {
                    bonuses.effectivePrayers.range = range;
                }
            }
            else if (player.equipment.weapon.weaponType == AttackStyleType.MAGIC)
            {
                Prayer magic = player.prayerController.MatchGroup(PrayerGroup.ACCURACY);
                if (magic != null && magic.isActive)
                {
                    bonuses.effectivePrayers.magic = magic;
                }
            }
        }

        // Calculate max hit using weapon's formula
        float maxHit = 0;

        if (player.equipment.weapon is MeleeWeapon)
        {
            maxHit = CalculateMeleeMaxHit(player, bonuses);
        }
        else if (player.equipment.weapon is RangedWeapon)
        {
            maxHit = CalculateRangedMaxHit(player, bonuses);
        }
        else if (player.equipment.weapon is MagicWeapon)
        {
            maxHit = CalculateMagicMaxHit(player, bonuses);
        }

        return Mathf.FloorToInt(maxHit);
    }

    /// <summary>
    /// Calculate melee max hit (inline formula from MeleeWeapon.cs).
    /// </summary>
    private float CalculateMeleeMaxHit(Player player, AttackBonuses bonuses)
    {
        // Calculate strength level with prayers
        float prayerMultiplier = 1.0f;
        if (bonuses.effectivePrayers?.strength != null)
        {
            if (bonuses.effectivePrayers.strength.type == PrayerType.PIETY)
                prayerMultiplier = 1.23f;
        }

        AttackStyle style = player.equipment.weapon.GetAttackStyle();
        int styleStrengthBonus = AttackStylesController.Instance.GetStrengthBonus(style);

        float strengthLevel = Mathf.Floor(
            (Mathf.Floor(player.currentStats.strength * prayerMultiplier) +
             styleStrengthBonus + 8) * bonuses.voidMultiplier);

        int equipmentBonus = player.bonuses.other.meleeStrength;

        return Mathf.Floor(
            Mathf.Floor((strengthLevel * (equipmentBonus + 64) + 320f) / 640f) *
            bonuses.gearMeleeMultiplier *
            bonuses.overallMultiplier);
    }

    /// <summary>
    /// Calculate ranged max hit (inline formula from RangedWeapon.cs).
    /// </summary>
    private float CalculateRangedMaxHit(Player player, AttackBonuses bonuses)
    {
        float prayerMultiplier = 1.0f;
        if (bonuses.effectivePrayers?.range != null)
        {
            if (bonuses.effectivePrayers.range.type == PrayerType.RIGOUR)
                prayerMultiplier = 1.23f;
        }

        int styleBonus = bonuses.isAccurate ? 3 : 0;

        float rangedStrength =
            Mathf.Floor(Mathf.Floor(player.currentStats.range) * prayerMultiplier + styleBonus + 8) *
            bonuses.voidMultiplier;

        return Mathf.Floor(
            Mathf.Floor(0.5f + ((rangedStrength * (player.bonuses.other.rangedStrength + 64)) / 640f) *
                        bonuses.gearRangeMultiplier) * 1f); // Damage multiplier = 1 for base calc
    }

    /// <summary>
    /// Calculate magic max hit (inline formula from MagicWeapon.cs).
    /// </summary>
    private float CalculateMagicMaxHit(Player player, AttackBonuses bonuses)
    {
        // Default spell damage if not set
        if (bonuses.magicBaseSpellDamage == 0)
        {
            bonuses.magicBaseSpellDamage = 20; // Default value
        }

        return Mathf.Floor(bonuses.magicBaseSpellDamage * player.bonuses.other.magicDamage);
    }

    /// <summary>
    /// Format bonus with + or - sign.
    /// </summary>
    private string FormatBonus(int bonus)
    {
        if (bonus > 0)
            return "+" + bonus;
        else if (bonus < 0)
            return bonus.ToString();
        else
            return "+0";
    }

    private Player FindPlayer()
    {
        Player player = Object.FindAnyObjectByType<Player>();
        if (player == null)
        {
            Debug.LogWarning("[StatsPanel] No player found!");
        }
        return player;
    }
}
