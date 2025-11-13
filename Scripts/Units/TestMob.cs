using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Test mob for verifying combat mechanics, prayers, and projectiles.
/// Fully configurable from Unity inspector.
/// </summary>
public class TestMob : Mob
{
    [Header("=== TEST MOB CONFIGURATION ===")]

    [Header("Damage Calculation Mode")]
    [Tooltip("When to calculate damage for projectiles")]
    public DamageCalculationMode damageMode = DamageCalculationMode.OnCast;

    public enum DamageCalculationMode
    {
        OnCast,   // Prayer must be active when attack starts (default OSRS)
        OnImpact  // Prayer can be activated while projectile is in flight
    }

    [Header("Attack Styles")]
    [Tooltip("Which attack styles this mob can use (randomly selects if multiple)")]
    public bool useMelee = true;
    public bool useRange = false;
    public bool useMagic = false;

    [Header("Attack Properties")]
    [Tooltip("Base max hit for all attacks")]
    [Range(1, 50)]
    public int maxHit = 10;

    [Tooltip("Attack speed in ticks (4 = 2.4s)")]
    [Range(2, 10)]
    public int attackSpeedTicks = 4;

    [Tooltip("Attack range in tiles")]
    [Range(1, 10)]
    public int attackRangeTiles = 1;

    [Header("Visual Settings")]
    [Tooltip("Projectile color for ranged attacks")]
    public Color rangedProjectileColor = Color.green;

    [Tooltip("Projectile color for magic attacks")]
    public Color magicProjectileColor = Color.blue;

    [Tooltip("Projectile size")]
    [Range(0.1f, 2f)]
    public float projectileSize = 0.5f;

    [Header("Debug Info")]
    [SerializeField] private string currentAttackStyle = "";
    [SerializeField] private bool lastAttackWasBlocked = false;

    // Available attack styles based on inspector settings
    private List<string> availableStyles = new List<string>();

    protected override void Start()
    {
        base.Start();

        // Set mob name if not already set
        if (string.IsNullOrEmpty(mobName))
        {
            mobName = "Test Mob";
        }

        // Initialize weapons based on inspector settings
        InitializeWeapons();

        // Auto-aggro for easy testing
        if (autoAggroPlayer)
        {
            SetAggressiveToPlayer();
        }
    }

    /// <summary>
    /// Initialize weapons based on inspector configuration.
    /// </summary>
    private void InitializeWeapons()
    {
        // Clear any existing weapons
        weapons.Clear();
        availableStyles.Clear();

        // Create melee weapon if enabled
        if (useMelee)
        {
            TestMeleeWeapon meleeWeapon = new TestMeleeWeapon();
            meleeWeapon.maxHit = maxHit;
            meleeWeapon.attackSpeed = attackSpeedTicks;
            meleeWeapon.attackRange = 1; // Melee is always range 1
            weapons["slash"] = meleeWeapon;
            availableStyles.Add("slash");

            Debug.Log($"[TEST MOB] Created MELEE weapon: {maxHit} max hit");
        }

        // Create ranged weapon if enabled
        if (useRange)
        {
            TestRangedWeapon rangedWeapon = new TestRangedWeapon();
            rangedWeapon.maxHit = maxHit;
            rangedWeapon.attackSpeed = attackSpeedTicks;
            rangedWeapon.attackRange = attackRangeTiles;
            rangedWeapon.projectileColor = rangedProjectileColor;
            rangedWeapon.projectileSize = projectileSize;
            rangedWeapon.checkPrayerOnHit = (damageMode == DamageCalculationMode.OnImpact);
            weapons["range"] = rangedWeapon;
            availableStyles.Add("range");

            Debug.Log($"[TEST MOB] Created RANGE weapon: {maxHit} max hit, {attackRangeTiles} range, prayer check on {damageMode}");
        }

        // Create magic weapon if enabled
        if (useMagic)
        {
            TestMagicWeapon magicWeapon = new TestMagicWeapon();
            magicWeapon.maxHit = maxHit;
            magicWeapon.attackSpeed = attackSpeedTicks;
            magicWeapon.attackRange = attackRangeTiles;
            magicWeapon.projectileColor = magicProjectileColor;
            magicWeapon.projectileSize = projectileSize;
            magicWeapon.checkPrayerOnHit = (damageMode == DamageCalculationMode.OnImpact);
            magicWeapon.baseSpellDamage = maxHit;
            weapons["magic"] = magicWeapon;
            availableStyles.Add("magic");

            Debug.Log($"[TEST MOB] Created MAGIC weapon: {maxHit} max hit, {attackRangeTiles} range, prayer check on {damageMode}");
        }

        // Default to first available style
        if (availableStyles.Count > 0)
        {
            attackStyle = availableStyles[0];
            currentAttackStyle = attackStyle;
        }
    }

    /// <summary>
    /// Randomly select attack style from available options.
    /// SDK Reference: Mob.attackStyleForNewAttack() in Mob.ts
    /// </summary>
    public override string AttackStyleForNewAttack()
    {
        if (availableStyles.Count == 0)
        {
            Debug.LogWarning("[TEST MOB] No attack styles available!");
            return "slash";
        }

        // If only one style, use it
        if (availableStyles.Count == 1)
        {
            currentAttackStyle = availableStyles[0];
            return availableStyles[0];
        }

        // Randomly select from available styles
        int randomIndex = Random.Range(0, availableStyles.Count);
        string selectedStyle = availableStyles[randomIndex];

        currentAttackStyle = selectedStyle;

        // Log the selection for debugging
        Debug.Log($"[TEST MOB] Selected attack style: {selectedStyle.ToUpper()}");

        return selectedStyle;
    }

    /// <summary>
    /// Override perform attack to add debug logging.
    /// </summary>
    public override void PerformAttack()
    {
        if (aggro == null || aggro.IsDying())
            return;

        // Get current weapon
        if (!weapons.ContainsKey(attackStyle) || weapons[attackStyle] == null)
        {
            Debug.LogWarning($"[TEST MOB] No weapon for style: {attackStyle}");
            return;
        }

        Weapon weapon = weapons[attackStyle];

        // Check if attack will be blocked
        bool willBeBlocked = weapon.IsBlockable(this, aggro, attackStyle);
        lastAttackWasBlocked = willBeBlocked;

        if (willBeBlocked)
        {
            Debug.Log($"[TEST MOB] {attackStyle.ToUpper()} attack BLOCKED by prayer!");
            attackFeedback = AttackIndicator.BLOCKED;
        }
        else
        {
            Debug.Log($"[TEST MOB] {attackStyle.ToUpper()} attack hit!");
            attackFeedback = AttackIndicator.HIT;
        }

        // Execute attack
        base.PerformAttack();
    }

    /// <summary>
    /// Custom melee weapon for testing.
    /// </summary>
    private class TestMeleeWeapon : MeleeWeapon
    {
        public int maxHit = 10;

        public TestMeleeWeapon()
        {
            weaponType = AttackStyleType.MELEE;
            itemName = ItemName.DRAGON_SCIMITAR; // Dummy name
        }

        protected override float CalculateMaxHit(Unit from, Unit to, AttackBonuses bonuses)
        {
            // Simple fixed damage for testing
            return maxHit;
        }
    }

    /// <summary>
    /// Custom ranged weapon for testing.
    /// </summary>
    private class TestRangedWeapon : RangedWeapon
    {
        public int maxHit = 10;
        public Color projectileColor = Color.green;
        public float projectileSize = 0.5f;
        public bool checkPrayerOnHit = false;

        public TestRangedWeapon()
        {
            weaponType = AttackStyleType.RANGED;
            itemName = ItemName.TWISTED_BOW; // Dummy name
        }

        protected override float CalculateMaxHit(Unit from, Unit to, AttackBonuses bonuses)
        {
            // Simple fixed damage for testing
            return maxHit;
        }

        protected override void RegisterProjectile(Unit from, Unit to, AttackBonuses bonuses, ProjectileOptions options)
        {
            // Override projectile options with our test settings
            ProjectileOptions testOptions = new ProjectileOptions
            {
                hidden = false,
                checkPrayerAtHit = checkPrayerOnHit,
                color = projectileColor,
                size = projectileSize,
                modelPrefab = null, // Use colored sphere
            };

            // Create projectile with "range" style
            Projectile projectile = new Projectile(
                this,
                damageRoll,
                from,
                to,
                "range",
                testOptions
            );

            to.AddProjectile(projectile);

            Debug.Log($"[TEST MOB] Ranged projectile created: {projectile.damage} damage, checkPrayerAtHit={checkPrayerOnHit}");
        }
    }

    /// <summary>
    /// Custom magic weapon for testing.
    /// </summary>
    private class TestMagicWeapon : MagicWeapon
    {
        public int maxHit = 10;
        public Color projectileColor = Color.blue;
        public float projectileSize = 0.5f;
        public bool checkPrayerOnHit = false;

        public TestMagicWeapon()
        {
            weaponType = AttackStyleType.MAGIC;
            //itemName = ItemName.STAFF_OF_LIGHT; // Dummy name
        }

        protected override float CalculateMaxHit(Unit from, Unit to, AttackBonuses bonuses)
        {
            // Simple fixed damage for testing
            return maxHit;
        }

        protected override void RegisterProjectile(Unit from, Unit to, AttackBonuses bonuses, ProjectileOptions options)
        {
            // Override projectile options with our test settings
            ProjectileOptions testOptions = new ProjectileOptions
            {
                hidden = false,
                checkPrayerAtHit = checkPrayerOnHit,
                color = projectileColor,
                size = projectileSize,
                modelPrefab = null, // Use colored sphere
            };

            // Create projectile with "magic" style
            Projectile projectile = new Projectile(
                this,
                damageRoll,
                from,
                to,
                "magic",
                testOptions
            );

            to.AddProjectile(projectile);

            Debug.Log($"[TEST MOB] Magic projectile created: {projectile.damage} damage, checkPrayerAtHit={checkPrayerOnHit}");
        }
    }
}