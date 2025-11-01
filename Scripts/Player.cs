using UnityEngine;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Player character with full movement system, equipment, and combat.
/// Matches SDK's Player.ts implementation.
/// SDK Reference: Player.ts
/// 
/// CRITICAL IMPLEMENTATION NOTES:
/// 
/// 1. THREE POSITION SYSTEM:
///    - location: True tile position (where player actually is on server)
///    - destinationLocation: Where user clicked (target)
///    - pathTargetLocation: Where we're actually pathing to (may differ from destination)
///    - perceivedLocation: Visual position for smooth interpolation
/// 
/// 2. TICK SYSTEM:
///    - movementStep(): Called every game tick (0.6s) - updates true position
///    - ClientTick(): Called at fixed 50Hz rate - updates visual position
///    - Visual movement is interpolated between ticks for smoothness
/// 
/// 3. PATH SYSTEM:
///    - path array stores next 2-3 tiles
///    - Only corners (direction changes) are stored
///    - Each PathStep has position, run flag, and direction
/// </summary>
/// 

/// <summary>
/// Container for all equipped items.
/// SDK Reference: UnitEquipment in Unit.ts
/// </summary>
[System.Serializable]
public class PlayerEquipment
{
    public Weapon weapon;
    public Equipment offhand;
    public Equipment helmet;
    public Equipment necklace;
    public Equipment chest;
    public Equipment legs;
    public Equipment feet;
    public Equipment gloves;
    public Equipment ring;
    public Equipment cape;
    public Equipment ammo;
}




public class Player : Unit
{

    #region Position System (CRITICAL)

    // NOTE: gridPosition is inherited from Entity (same as location)
    // This is the TRUE tile position - where the player actually is on the server

    /// <summary>
    /// Where the user clicked / wants to go.
    /// SDK Reference: Player.ts line 51
    /// This may not be pathable (e.g., clicked on a wall)
    /// </summary>
    public Vector2Int destinationLocation;

    /// <summary>
    /// Where we're actually pathing to (after collision/entity checks).
    /// May be null if no valid path exists.
    /// SDK Reference: Player.ts line 53
    /// </summary>
    public Vector2Int? pathTargetLocation;

    #endregion

    protected override void Awake()
    {
        base.Awake();

        // CRITICAL: Players NEVER block mob movement
        // This is a key OSRS mechanic - mobs can walk through players
        // SDK Reference: In Pathing.ts, when mobToAvoid is null (player movement), 
        // mob collisions aren't checked at all
        consumesSpace = false;
        autoRetaliate = false; // Players don't auto-retaliate
    }

    #region Path System

    /// <summary>
    /// Represents a single step in the player's movement path.
    /// SDK Reference: Player.ts lines 65-69 (path array with run property)
    /// </summary>
    public class PathStep
    {
        public Vector2Int position;
        public bool run;
        public float direction;  // Angle in radians

        public PathStep(Vector2Int pos, bool run, float direction)
        {
            this.position = pos;
            this.run = run;
            this.direction = direction;
        }
    }

    /// <summary>
    /// Queue of tiles to move through.
    /// SDK Reference: Player.ts line 65
    /// 
    /// OPTIMIZATION: Only stores corners (direction changes) + last tile
    /// This is built in moveTowardsDestination() and consumed in ClientTick()
    /// </summary>
    public List<PathStep> path = new List<PathStep>();

    #endregion

    #region Rotation System

    // ROTATION SYSTEM EXPLANATION:
    // The player rotates smoothly over time, not instantly.
    // Three angles track this:

    /// <summary>
    /// Angle the player faces when idle (no target, no movement).
    /// SDK Reference: Player.ts line 273 (private restingAngle)
    /// </summary>
    private float restingAngle = 0;

    /// <summary>
    /// Target angle the player wants to face.
    /// SDK Reference: Player.ts line 274 (private nextAngle)
    /// Set to direction of movement or angle to aggro target
    /// </summary>
    private float nextAngle = 0;

    /// <summary>
    /// Current interpolated angle (smoothly moves toward nextAngle).
    /// SDK Reference: Player.ts line 276 (private _angle)
    /// </summary>
    private float _angle = 0;

    /// <summary>
    /// Track tick percent from last frame to calculate rotation delta.
    /// SDK Reference: Player.ts line 278
    /// </summary>
    private float lastTickPercent = 0;

    // ROTATION RATE CONSTANTS
    // SDK Reference: Player.ts lines 49-53
    // Player can rotate 64 JAU per client tick
    // 50 client ticks per second
    // 512 JAU per radian
    const int PLAYER_ROTATION_RATE_JAU = 64;
    const int CLIENT_TICKS_PER_SECOND = 50;
    const int JAU_PER_RADIAN = 512;
    const float RADIANS_PER_TICK = ((float)(CLIENT_TICKS_PER_SECOND * PLAYER_ROTATION_RATE_JAU) / JAU_PER_RADIAN) * 0.6f;

    // EPSILON - "Close enough" threshold for position checking
    // SDK Reference: Player.ts line 57
    const float EPSILON = 0.1f;

    #endregion

    #region Movement State

    /// <summary>
    /// Is the player running or walking?
    /// SDK Reference: Player.ts line 48
    /// </summary>
    public bool running = true;

    #endregion

    #region Client Tick System

    // FIXED: Synchronized client ticks with game ticks
    // Client ticks are now deterministic: exactly 30 per game tick
    private int currentClientTickInGameTick = 0;  // Which client tick are we on (0-29)
    private const int CLIENT_TICKS_PER_GAME_TICK = 30;
    private const float CLIENT_TICK_INTERVAL = 0.02f; // 20ms = 50fps (for reference only)

    #endregion

    #region Debug Fields

    [Header("Debug Info")]
    public Vector2Int gridPosition_Debug;
    public Vector2Int destinationLocation_Debug;
    public Vector2 perceivedLocation_Debug;
    public int pathCount_Debug;
    public int lastMoveDistance_Debug;
    public bool initialized_Debug;

    // Set to true to enable detailed movement logging
    private bool ENABLE_POSITION_DEBUG = false;

    [Header("Debug - Client Ticks")]
    public int clientTicksThisGameTick = 0;
    public int totalClientTicks = 0;
    public float lastTickPercent_Debug = 0f;
    public int lastClientTickInGameTick_Debug = 0;

    #endregion

    /// <summary>
    /// Calculate total weight from inventory and equipment.
    /// SDK Reference: Player.weight getter in Player.ts lines 256-264
    /// 
    /// Used for run energy drain calculations.
    /// Max weight: 64kg (affects run energy drain)
    /// </summary>
    public float GetWeight()
    {
        List<Item> allItems = new List<Item>();

        // Add equipment
        if (equipment.weapon != null) allItems.Add(equipment.weapon);
        if (equipment.offhand != null) allItems.Add(equipment.offhand);
        if (equipment.helmet != null) allItems.Add(equipment.helmet);
        if (equipment.necklace != null) allItems.Add(equipment.necklace);
        if (equipment.chest != null) allItems.Add(equipment.chest);
        if (equipment.legs != null) allItems.Add(equipment.legs);
        if (equipment.feet != null) allItems.Add(equipment.feet);
        if (equipment.gloves != null) allItems.Add(equipment.gloves);
        if (equipment.ring != null) allItems.Add(equipment.ring);
        if (equipment.cape != null) allItems.Add(equipment.cape);
        if (equipment.ammo != null) allItems.Add(equipment.ammo);

        // Add inventory items
        foreach (Item item in inventory)
        {
            if (item != null)
            {
                allItems.Add(item);
            }
        }

        // Sum weights
        float totalWeight = 0f;
        foreach (Item item in allItems)
        {
            totalWeight += item.Weight;
        }

        // Clamp between 0 and 64kg
        return Mathf.Clamp(totalWeight, 0f, 64f);
    }

    #region Inventory & Equipment

    [Header("Inventory & Equipment")]
    [Tooltip("Player inventory - 28 slots")]
    public Item[] inventory = new Item[28];

    [Tooltip("Equipped items")]
    public PlayerEquipment equipment = new PlayerEquipment();

    [Tooltip("Cached equipment bonuses")]
    private UnitBonuses cachedBonuses = null;

    [Header("Eating System")]
    [Tooltip("Food and potion consumption controller")]
    public Eating eats;

    #endregion

    #region Inventory Management

    /// <summary>
    /// Get array of open (empty) inventory slot indices.
    /// SDK Reference: Player.openInventorySlots() in Player.ts
    /// </summary>
    public int[] GetOpenInventorySlots()
    {
        List<int> openSlots = new List<int>();
        for (int i = 0; i < 28; i++)
        {
            if (inventory[i] == null)
            {
                openSlots.Add(i);
            }
        }
        return openSlots.ToArray();
    }

    /// <summary>
    /// Swap two inventory positions.
    /// SDK Reference: Player.swapItemPositions()
    /// </summary>
    public void SwapItemPositions(int pos1, int pos2)
    {
        Item temp = inventory[pos1];
        inventory[pos1] = inventory[pos2];
        inventory[pos2] = temp;
    }

    #endregion

    #region Equipment Management

    /// <summary>
    /// Called when equipment changes. Recalculates bonuses.
    /// SDK Reference: Player.equipmentChanged() in Player.ts lines 187-235
    /// </summary>
    public void EquipmentChanged()
    {
        InterruptCombat();

        // Collect all equipped items
        List<Equipment> gear = new List<Equipment>();
        if (equipment.weapon != null) gear.Add(equipment.weapon);
        if (equipment.offhand != null) gear.Add(equipment.offhand);
        if (equipment.helmet != null) gear.Add(equipment.helmet);
        if (equipment.necklace != null) gear.Add(equipment.necklace);
        if (equipment.chest != null) gear.Add(equipment.chest);
        if (equipment.legs != null) gear.Add(equipment.legs);
        if (equipment.feet != null) gear.Add(equipment.feet);
        if (equipment.gloves != null) gear.Add(equipment.gloves);
        if (equipment.ring != null) gear.Add(equipment.ring);
        if (equipment.cape != null) gear.Add(equipment.cape);

        // Handle ammo slot with compatibility check
        // SDK Reference: Player.equipmentChanged() in Player.ts lines 201-209
        if (equipment.ammo != null)
        {
            bool addAmmo = false;

            // Check weapon compatibility (for ranged weapons)
            if (equipment.weapon != null)
            {
                ItemName[] compatibleAmmo = equipment.weapon.GetCompatibleAmmo();
                if (compatibleAmmo != null && compatibleAmmo.Length > 0)
                {
                    // Check if this ammo is compatible
                    foreach (ItemName ammoName in compatibleAmmo)
                    {
                        if (equipment.ammo.itemName == ammoName)
                        {
                            addAmmo = true;
                            break;
                        }
                    }
                }
            }

            // Blessings always work (even without weapon)
            if (equipment.ammo is Ammo ammoItem && ammoItem.GetAmmoType() == AmmoType.BLESSING)
            {
                addAmmo = true;
            }

            if (addAmmo)
            {
                gear.Add(equipment.ammo);
            }
        }

        // Update bonuses with synergies
        // SDK Reference: Player.equipmentChanged() in Player.ts lines 195-209
        // This allows items like Crystal armor to boost Bowfa
        foreach (Equipment item in gear)
        {
            if (item != null)
            {
                item.UpdateBonuses(gear);
            }
        }

        // Recalculate bonuses from all gear
        cachedBonuses = UnitBonuses.Empty();
        foreach (Equipment item in gear)
        {
            if (item != null && item.bonuses != null)
            {
                cachedBonuses = UnitBonuses.Merge(cachedBonuses, item.bonuses);
            }
        }

        // Check for complete set effects
        // SDK Reference: Player.equipmentChanged() in Player.ts lines 211-234
        List<SetEffect> allSetEffects = new List<SetEffect>();
        foreach (Equipment equipmentPiece in gear)
        {
            if (equipmentPiece != null)
            {
                SetEffect setEffect = equipmentPiece.GetEquipmentSetEffect();
                if (setEffect != null)
                {
                    allSetEffects.Add(setEffect);
                }
            }
        }

        // Find complete sets (avoid checking same set twice)
        List<SetEffect> completeSetEffects = new List<SetEffect>();
        System.Collections.Generic.HashSet<SetEffectType> checkedSets = new System.Collections.Generic.HashSet<SetEffectType>();

        foreach (SetEffect setEffect in allSetEffects)
        {
            if (setEffect == null) continue;

            SetEffectType effectType = setEffect.GetEffectName();

            // Skip if already checked this set type
            if (checkedSets.Contains(effectType))
                continue;

            checkedSets.Add(effectType);

            // Check if set is complete
            if (setEffect.IsComplete(gear))
            {
                completeSetEffects.Add(setEffect);
                Debug.Log($"[PLAYER] Set effect active: {effectType}");
            }
        }

        // TODO: Store complete set effects for damage calculations
        // setEffects = completeSetEffects; // Add this field to Unit.cs later

        // Update our bonuses reference
        bonuses = cachedBonuses;

        Debug.Log($"[PLAYER] Equipment changed. Active sets: {completeSetEffects.Count}");
    }

    /// <summary>
    /// Get equipment bonuses (cached).
    /// SDK Reference: Player.bonuses getter in Player.ts
    /// </summary>
    public UnitBonuses GetBonuses()
    {
        if (cachedBonuses == null)
        {
            cachedBonuses = UnitBonuses.Empty();
        }
        return cachedBonuses;
    }

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        // Initialize positions
        destinationLocation = gridPosition;
        pathTargetLocation = gridPosition;
        perceivedLocation = new Vector2(gridPosition.x, gridPosition.y);

        // Initialize visual position to match starting grid position
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GridToWorld(gridPosition);

            // Register with GridManager
            GridManager.Instance.RegisterEntity(this);

            Debug.Log($"[PLAYER] Spawned at {gridPosition}");
        }

        // Initialize stats
        SetStats();

        // Verify stats are PlayerStats (safety check)
        if (!(stats is PlayerStats))
        {
            Debug.LogError($"[PLAYER] Stats type is {stats.GetType().Name}, expected PlayerStats! Re-initializing.");
            SetStats();
        }

        // Initialize equipment bonuses
        cachedBonuses = UnitBonuses.Empty();
        bonuses = cachedBonuses;

        // Initialize eating system
        eats = new Eating();
        eats.player = this;

        if (GetComponent<PlayerInput>() == null)
        {
            gameObject.AddComponent<PlayerInput>();
        }

        if (GetComponent<ProjectileRenderer>() == null)
        {
            gameObject.AddComponent<ProjectileRenderer>();
        }

        if (GetComponent<HitsplatRenderer>() == null)
        {
            gameObject.AddComponent<HitsplatRenderer>();
        }

        // Initialize prayer controller
        prayerController = new PrayerController(this);

        if (inventory[0] == null)
        {
            // Add a test weapon
            //Weapon testWeapon = new Weapon();
            //testWeapon.itemName = ItemName.DRAGON_SCIMITAR;
            //inventory[0] = testWeapon;

            // Add test armor
            Equipment testHelm = new Equipment();
            testHelm.itemName = ItemName.ARMADYL_HELMET;
            testHelm.slot = EquipmentSlot.HELMET;
            inventory[1] = testHelm;

            // Add test food
            Food testFood = new Food();
            testFood.itemName = ItemName.KARAMBWAN;
            testFood.healAmount = 18;
            inventory[2] = testFood;

            Debug.Log("[Player] Added test items to inventory");
        }

        initialized_Debug = true;
    }

    void Update()
    {
        if (WorldManager.Instance == null || GridManager.Instance == null)
            return;

        // FIXED: Synchronized client ticks with game ticks
        // Calculate how many client ticks SHOULD have elapsed based on game tick progress
        float gameTickProgress = WorldManager.Instance.GetTickPercent();
        int targetClientTick = Mathf.Min(CLIENT_TICKS_PER_GAME_TICK - 1,
                                         Mathf.FloorToInt(gameTickProgress * CLIENT_TICKS_PER_GAME_TICK));

        // Run any missing client ticks to catch up
        while (currentClientTickInGameTick <= targetClientTick)
        {
            // Calculate tick percent based on which client tick this is
            // This ensures smooth 0->1 progression over exactly 30 client ticks
            float tickPercent = (float)currentClientTickInGameTick / (float)CLIENT_TICKS_PER_GAME_TICK;

            ClientTick(tickPercent);
            currentClientTickInGameTick++;
            clientTicksThisGameTick++;
            totalClientTicks++;

            // Debug tracking
            lastTickPercent_Debug = tickPercent;
            lastClientTickInGameTick_Debug = currentClientTickInGameTick;
        }

        // Update debug displays
        gridPosition_Debug = gridPosition;
        destinationLocation_Debug = destinationLocation;
        perceivedLocation_Debug = perceivedLocation;
        pathCount_Debug = path.Count;
    }

    // CRITICAL: Override Entity's LateUpdate to prevent position conflict
    void LateUpdate()
    {
        // We handle our own position from perceivedLocation
        // Do NOT call base.LateUpdate() which would interfere

        if (GridManager.Instance == null)
            return;

        // Set visual position from perceivedLocation (SDK-style)
        Vector3 worldPos = new Vector3(
            perceivedLocation.x * GridManager.Instance.tileSize,
            0f,
            perceivedLocation.y * GridManager.Instance.tileSize
        );
        transform.position = worldPos;

        // Apply rotation to face movement/combat direction
        float rotationRadians = GetPerceivedRotation(WorldManager.Instance.GetTickPercent());

        // Convert to degrees and apply
        // Note: In Unity, Y rotation of 0 = facing +Z (north)
        // The SDK's angle system: 0 = west, increasing clockwise
        // So we need to add 90 degrees to convert SDK angles to Unity
        float rotationDegrees = rotationRadians * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0, rotationDegrees, 0);
    }

    #endregion

    #region Stats Setup

    public override void SetStats()
    {
        // TODO: Load from Settings or save data
        // For now, use default 99s
        stats = new PlayerStats
        {
            attack = 99,
            strength = 99,
            defence = 99,
            range = 99,
            magic = 99,
            hitpoint = 99,
            prayer = 99,
            agility = 99,        // Affects run energy regeneration
            run = 10000,         // Full run energy (displayed as 100%)
            specialAttack = 100  // Full special attack energy
        };

        currentStats = stats.Clone();
    }

    #endregion

    #region Combat Implementation

    /// <summary>
    /// Get attack range from equipped weapon.
    /// </summary>
    public override int GetAttackRange()
    {
        if (equipment.weapon != null)
            return equipment.weapon.attackRange;
        return 1; // Unarmed is melee
    }

    /// <summary>
    /// Get attack speed from equipped weapon.
    /// </summary>
    public override int GetAttackSpeed()
    {
        if (equipment.weapon != null)
            return equipment.weapon.attackSpeed;
        return 4; // Unarmed is 4 ticks
    }

    /// <summary>
    /// Perform attack with equipped weapon or unarmed.
    /// </summary>
    public override void PerformAttack()
    {
        if (aggro == null || aggro.IsDying())
            return;

        if (equipment.weapon != null)
        {
            // Calculate equipment bonuses
            AttackBonuses bonuses = new AttackBonuses
            {
                styleBonus = 0, // Could add attack style bonuses
                isAccurate = false, // Could check attack style
                gearMeleeMultiplier = 1.0f,
                gearRangeMultiplier = 1.0f,
                gearMageMultiplier = 1.0f,
                voidMultiplier = 1.0f,
                overallMultiplier = 1.0f
            };

            // Check for special attack
            if (useSpecialAttack && equipment.weapon.HasSpecialAttack())
            {
                PlayerStats pStats = currentStats as PlayerStats;
                if (pStats != null && pStats.specialAttack >= equipment.weapon.GetSpecialAttackDrain())
                {
                    // Use special attack
                    equipment.weapon.SpecialAttack(this, aggro, bonuses);
                    pStats.specialAttack -= equipment.weapon.GetSpecialAttackDrain();
                    useSpecialAttack = false; // Turn off after use

                    Debug.Log($"[COMBAT] Used special attack! Energy: {pStats.specialAttack}%");
                }
                else
                {
                    // Not enough special energy, use normal attack
                    equipment.weapon.Attack(this, aggro, bonuses);
                    Debug.Log("[COMBAT] Not enough special attack energy!");
                }
            }
            else
            {
                // Normal attack
                equipment.weapon.Attack(this, aggro, bonuses);
            }
        }
        else
        {
            // Unarmed combat (punch)
            Debug.Log("[COMBAT] Unarmed punch!");

            // Create simple melee projectile for punch
            AttackBonuses bonuses = new AttackBonuses
            {
                attackStyle = "crush",
                styleBonus = 0
            };

            // Calculate unarmed damage (very basic)
            int maxHit = 1; // Unarmed is weak
            int damage = Random.Range(0, maxHit + 1);

            // Create projectile
            Projectile punchProjectile = new Projectile(
                null, // No weapon
                damage,
                this,
                aggro,
                "crush",
                new ProjectileOptions { hidden = true } // Melee is invisible
            );

            aggro.AddProjectile(punchProjectile);
        }
    }

    /// <summary>
    /// Regenerate special attack energy.
    /// SDK Reference: PlayerRegenTimer.specRegen() in PlayerRegenTimers.ts
    /// </summary>
    private int specRegenTimer = 50; // 50 ticks = 30 seconds

    private void RegenerateSpecialAttack()
    {
        PlayerStats pStats = currentStats as PlayerStats;
        if (pStats == null) return;

        specRegenTimer--;
        if (specRegenTimer <= 0)
        {
            specRegenTimer = 50; // Reset timer

            // Restore 10% special attack energy
            pStats.specialAttack = Mathf.Min(100, pStats.specialAttack + 10);
        }
    }

    /// <summary>
    /// Track experience gains.
    /// </summary>
    public override void GrantXp(XpDrop xpDrop)
    {
        Debug.Log($"[XP] Gained {xpDrop.xp:F1} {xpDrop.skill} experience!");

        // TODO: Add to actual XP tracking system
        // TODO: Create visual XP drop
    }

    /// <summary>
    /// Toggle special attack on/off.
    /// </summary>
    public bool useSpecialAttack = false;

    public void ToggleSpecialAttack()
    {
        useSpecialAttack = !useSpecialAttack;
        Debug.Log($"[COMBAT] Special attack: {(useSpecialAttack ? "ON" : "OFF")}");
    }

    #endregion

    #region Movement - User Input

    /// <summary>
    /// Called when user clicks to move to a location.
    /// SDK Reference: Player.ts lines 313-364
    /// 
    /// IMPORTANT LOGIC:
    /// 1. Clears aggro (stops combat)
    /// 2. Resets pathTargetLocation
    /// 3. Handles clicking on STATIC entities (finds adjacent pathable tile)
    /// 4. Ignores NPCs/Units - player can walk through them
    /// 5. Sets destinationLocation
    /// </summary>
    public void MoveTo(int x, int y)
    {
        Vector2Int clickedPos = new Vector2Int(x, y);

        Debug.Log($"[PLAYER-MOVETO] Clicked on ({x}, {y}), currently at {gridPosition}");

        // Clear combat when moving
        // SDK Reference: Player.ts line 314
        InterruptCombat();

        // Clear any manual spell cast
        // SDK Reference: Player.ts line 316
        // TODO: manualSpellCastSelection = null;

        // Reset path target
        pathTargetLocation = null;

        // FIX: If clicking on current position, stop movement but let visual interpolation complete
        if (clickedPos == gridPosition)
        {
            Debug.Log($"[PLAYER-MOVETO] Clicked on current position - stopping movement");
            destinationLocation = gridPosition;
            // DON'T clear path - let ClientTick finish the visual interpolation
            // DON'T snap perceivedLocation - let it smoothly complete its movement
            return;
        }

        // CRITICAL FIX: Only check for STATIC entities, NOT Units/NPCs!
        // Players can walk through NPCs, so we should ignore them when clicking
        // SDK behavior: Players walk through mobs
        List<Entity> clickedOnEntities = Collision.CollideableEntitiesAtPoint(clickedPos, 1);

        // Filter out Units - we only care about static blocking entities
        List<Entity> staticEntities = new List<Entity>();
        foreach (Entity entity in clickedOnEntities)
        {
            if (!(entity is Unit))  // Ignore all Units (NPCs and Players)
            {
                staticEntities.Add(entity);
            }
        }

        if (staticEntities.Count > 0)
        {
            // Clicked on a STATIC entity (wall, tree, etc) - need to find adjacent pathable tile
            Entity clickedEntity = staticEntities[0];
            int maxDist = Mathf.CeilToInt(clickedEntity.size / 2f);

            // Scan around entity for pathable tiles
            List<Vector2Int> bestTiles = new List<Vector2Int>();
            float bestDistance = 9999f;

            for (int yOff = -maxDist; yOff < maxDist; yOff++)
            {
                for (int xOff = -maxDist; xOff < maxDist; xOff++)
                {
                    Vector2Int potentialPos = new Vector2Int(x + xOff, y + yOff);

                    // Check if this tile is free of STATIC entities (ignore Units)
                    List<Entity> entitiesHere = Collision.CollideableEntitiesAtPoint(potentialPos, 1);

                    // Filter to only static entities
                    bool hasStaticEntity = false;
                    foreach (Entity e in entitiesHere)
                    {
                        if (!(e is Unit))
                        {
                            hasStaticEntity = true;
                            break;
                        }
                    }

                    if (!hasStaticEntity)
                    {
                        float distance = Pathing.Dist(potentialPos.x, potentialPos.y, x, y);

                        if (distance <= bestDistance)
                        {
                            if (bestTiles.Count > 0 && distance < bestDistance)
                            {
                                // Found a closer tile, clear previous list
                                bestDistance = distance;
                                bestTiles.Clear();
                            }
                            bestTiles.Add(potentialPos);
                        }
                    }
                }
            }

            // Pick the closest tile to our current position
            if (bestTiles.Count > 0)
            {
                Vector2Int winner = bestTiles
                    .OrderBy(tile => Pathing.Dist(tile.x, tile.y, gridPosition.x, gridPosition.y))
                    .First();
                destinationLocation = winner;
                Debug.Log($"[PLAYER-MOVETO] Clicked on static entity, moving to adjacent tile: {winner}");
            }
            else
            {
                // No valid tile found, use original click position anyway
                destinationLocation = clickedPos;
            }
        }
        else
        {
            // Clicked on empty tile or tile with NPC (which we can walk through)
            // SDK Reference: Player.ts line 363 - but we add walkability validation

            // Check if clicked tile is actually walkable
            Tile clickedTile = GridManager.Instance.GetTileAt(clickedPos);

            if (clickedTile != null && clickedTile.isWalkable)
            {
                // Tile is walkable and has no static entities - use directly
                destinationLocation = clickedPos;
                Debug.Log($"[PLAYER-MOVETO] Moving directly to clicked position: {destinationLocation}");
            }
            else
            {
                // Clicked tile is unwalkable - find nearest walkable tile
                Vector2Int? nearestWalkable = FindNearestWalkableTile(clickedPos, maxRadius: 3);

                if (nearestWalkable.HasValue)
                {
                    destinationLocation = nearestWalkable.Value;
                    Debug.Log($"[PLAYER-MOVETO] Clicked unwalkable tile {clickedPos}, redirecting to nearest walkable: {destinationLocation}");
                }
                else
                {
                    // No walkable tiles nearby - keep current position (don't move)
                    destinationLocation = gridPosition;
                    Debug.LogWarning($"[PLAYER-MOVETO] Clicked unwalkable tile {clickedPos} with no nearby walkable tiles - ignoring click");
                }
            }
        }
    }

    /// <summary>
    /// Stop combat and clear aggro.
    /// SDK Reference: Player.ts line 310
    /// </summary>
    public void InterruptCombat()
    {
        aggro = null;
        // TODO: Clear any queued actions
    }

    /// <summary>
    /// Find the nearest walkable tile to a target position.
    /// Scans outward from the target in increasing radius.
    /// SDK Reference: Similar to Player.moveTo entity scanning logic in Player.ts lines 321-361
    /// </summary>
    /// <param name="targetPos">The position we want to reach (may be unwalkable)</param>
    /// <param name="maxRadius">Maximum search radius in tiles</param>
    /// <returns>Nearest walkable tile, or null if none found</returns>
    private Vector2Int? FindNearestWalkableTile(Vector2Int targetPos, int maxRadius = 3)
    {
        // Check if target itself is walkable first
        Tile targetTile = GridManager.Instance.GetTileAt(targetPos);
        if (targetTile != null && targetTile.isWalkable)
        {
            // Also check for static entities blocking it
            List<Entity> staticEntities = Collision.CollideableEntitiesAtPoint(targetPos, 1)
                .Where(e => !(e is Unit)).ToList();

            if (staticEntities.Count == 0)
            {
                return targetPos; // Target is walkable and unblocked
            }
        }

        // Scan outward in increasing radius to find walkable tiles
        List<Vector2Int> walkableTiles = new List<Vector2Int>();
        float bestDistance = 9999f;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            // Scan in a square pattern at this radius
            for (int yOff = -radius; yOff <= radius; yOff++)
            {
                for (int xOff = -radius; xOff <= radius; xOff++)
                {
                    // Only check tiles on the outer edge of this radius
                    if (Mathf.Abs(xOff) != radius && Mathf.Abs(yOff) != radius)
                        continue;

                    Vector2Int potentialPos = new Vector2Int(targetPos.x + xOff, targetPos.y + yOff);

                    // Check if tile exists and is walkable
                    Tile tile = GridManager.Instance.GetTileAt(potentialPos);
                    if (tile == null || !tile.isWalkable)
                        continue;

                    // Check for static entities blocking it (ignore Units/NPCs)
                    List<Entity> entitiesHere = Collision.CollideableEntitiesAtPoint(potentialPos, 1);
                    bool hasStaticEntity = false;
                    foreach (Entity e in entitiesHere)
                    {
                        if (!(e is Unit))
                        {
                            hasStaticEntity = true;
                            break;
                        }
                    }

                    if (!hasStaticEntity)
                    {
                        // This tile is walkable! Calculate distance to target
                        float distToTarget = Pathing.Dist(potentialPos.x, potentialPos.y, targetPos.x, targetPos.y);

                        if (distToTarget <= bestDistance)
                        {
                            if (walkableTiles.Count > 0 && distToTarget < bestDistance)
                            {
                                // Found closer tiles, clear previous list
                                bestDistance = distToTarget;
                                walkableTiles.Clear();
                            }
                            walkableTiles.Add(potentialPos);
                        }
                    }
                }
            }

            // If we found walkable tiles at this radius, pick the closest to player
            if (walkableTiles.Count > 0)
            {
                Vector2Int winner = walkableTiles
                    .OrderBy(tile => Pathing.Dist(tile.x, tile.y, gridPosition.x, gridPosition.y))
                    .First();
                return winner;
            }
        }

        // No walkable tiles found within radius
        return null;
    }

    #endregion

    #region Movement - Game Tick (Server)

    /// <summary>
    /// Called every game tick (0.6s) to update player position.
    /// SDK Reference: Player.ts lines 656-661
    /// 
    /// TICK SEQUENCE:
    /// 1. Activate prayers
    /// 2. Pick up ground items if standing on one
    /// 3. Determine where to move (if not frozen)
    /// 4. Execute movement (update gridPosition)
    /// 5. Update click markers
    /// </summary>
    public override void MovementStep()
    {
        // FIXED: Reset client tick counter for new game tick
        // This ensures we get exactly 30 client ticks per game tick
        if (ENABLE_POSITION_DEBUG)
        {
            Debug.Log($"[PLAYER-MOVEMENT] Game Tick #{WorldManager.Instance.GetTickCounter()} completed with {clientTicksThisGameTick} client ticks");
        }

        currentClientTickInGameTick = 0;
        clientTicksThisGameTick = 0;

        if (IsDying())
        {
            return;
        }

        // TODO: Activate prayers
        // SDK Reference: Player.ts line 658
        // ActivatePrayers();

        // TODO: Pick up ground items if seeking one
        // SDK Reference: Player.ts line 660
        // TakeSeekingItem();

        // Determine destination and move (if not frozen)
        if (!IsFrozen())
        {
            DetermineDestination();
            MoveTowardsDestination();
        }

        // TODO: Update path marker visualization
        // SDK Reference: Player.ts line 665
        // UpdatePathMarker();

        // Tick down frozen timer
        frozen--;
    }

    /// <summary>
    /// Determine where the player should path to this tick.
    /// SDK Reference: Player.ts lines 366-446
    /// 
    /// LOGIC:
    /// - If has aggro target: path toward target (unless under it)
    /// - If seeking item: path to item location
    /// - Otherwise: use destinationLocation from MoveTo()
    /// </summary>
    private void DetermineDestination()
    {
        if (aggro != null)
        {
            // TODO: Combat movement logic
            // SDK Reference: Player.ts lines 367-428
            // For now, just stop if we have aggro
            destinationLocation = gridPosition;
            return;
        }

        // TODO: Item seeking logic
        // SDK Reference: Player.ts lines 429-431
        // if (seekingItem != null)
        // {
        //     destinationLocation = seekingItem.groundLocation;
        // }
    }

    /// <summary>
    /// Create path and move toward destination.
    /// SDK Reference: Player.ts lines 533-600
    /// 
    /// CRITICAL PROCESS:
    /// 1. Calculate run energy drain/regen
    /// 2. Use Pathing.Path() to get next position
    /// 3. Update gridPosition (true position)
    /// 4. Build path array with corners only
    /// 5. Update nextAngle for rotation
    /// </summary>
    private void MoveTowardsDestination()
    {
        // Update next angle for rotation
        nextAngle = GetTargetAngle();

        // Check if already at destination
        if (destinationLocation.x == gridPosition.x &&
            destinationLocation.y == gridPosition.y)
        {
            pathTargetLocation = null;
            if (ENABLE_POSITION_DEBUG)
            {
                Debug.Log($"[PLAYER-TICK] Already at destination {gridPosition}");
            }
            return;
        }

        int speed = running ? 2 : 1;

        if (ENABLE_POSITION_DEBUG)
        {
            Debug.Log($"[PLAYER-TICK] === MOVEMENT STEP ===");
            Debug.Log($"[PLAYER-TICK] Current: {gridPosition}, Destination: {destinationLocation}");
            Debug.Log($"[PLAYER-TICK] Running: {running}, Speed: {speed}");
        }

        // Use pathfinding to get movement path
        // CRITICAL FIX: Pass null for player movement so NPCs don't block
        Pathing.MoveResult moveResult = Pathing.Path(
            gridPosition,
            destinationLocation,
            speed,
            null);  // CHANGED: Pass null for players - they ignore NPC collisions!

        pathTargetLocation = moveResult.destination;

        if (moveResult.path == null || moveResult.path.Count == 0)
        {
            if (ENABLE_POSITION_DEBUG)
            {
                Debug.Log($"[PLAYER-TICK] No path found or no movement needed!");
            }
            return;
        }

        if (ENABLE_POSITION_DEBUG)
        {
            Debug.Log($"[PLAYER-TICK] Tiles to traverse THIS TICK: {moveResult.path.Count}: {string.Join(" -> ", moveResult.path)}");
            Debug.Log($"[PLAYER-TICK] Moving to position: {moveResult.position}");
        }

        // Store original position
        Vector2Int oldPosition = gridPosition;

        // Update position
        gridPosition = moveResult.position;

        if (ENABLE_POSITION_DEBUG)
        {
            Debug.Log($"[PLAYER-TICK] Moving from {oldPosition} to {gridPosition}");
        }

        int tilesMovedThisTick = Mathf.Abs(gridPosition.x - oldPosition.x) +
                                 Mathf.Abs(gridPosition.y - oldPosition.y);
        lastMoveDistance_Debug = tilesMovedThisTick;

        if (ENABLE_POSITION_DEBUG)
        {
            Debug.Log($"[PLAYER-TICK] ACTUALLY MOVED {tilesMovedThisTick} TILES");
        }

        // Build visual path from the tiles we're ACTUALLY traversing this tick
        List<PathStep> newTiles = new List<PathStep>();

        // moveResult.path now contains ONLY the tiles we're moving through THIS TICK
        for (int idx = 0; idx < moveResult.path.Count; idx++)
        {
            Vector2Int pos = moveResult.path[idx];
            Vector2Int prevPos = idx == 0 ? oldPosition : moveResult.path[idx - 1];

            float direction = Pathing.Angle(prevPos.x, prevPos.y, pos.x, pos.y);

            PathStep step = new PathStep(
                pos,
                running && moveResult.path.Count >= 2,
                direction
            );

            newTiles.Add(step);
        }

        // Filter to corners only (direction changes) + last tile
        List<PathStep> corners = new List<PathStep>();
        for (int i = 0; i < newTiles.Count; i++)
        {
            bool isLastTile = (i == newTiles.Count - 1);
            bool isDirectionChange = (i < newTiles.Count - 1) &&
                                    (newTiles[i].direction != newTiles[i + 1].direction);

            if (isLastTile || isDirectionChange)
            {
                corners.Add(newTiles[i]);
            }
        }

        // Remove redundant first corner if same direction as second
        if (corners.Count > 1 && corners[1].direction == corners[0].direction)
        {
            corners.RemoveAt(0);
        }

        // Add corners to path queue
        path.AddRange(corners);
        if (ENABLE_POSITION_DEBUG)
        {
            Debug.Log($"[PLAYER-TICK] Added {corners.Count} corners to visual path");
            Debug.Log($"[PLAYER-TICK] Total path queue size: {path.Count}");
        }

        // Update next angle
        nextAngle = GetTargetAngle();
    }
    #endregion

    #region Movement - Client Tick (Visual Interpolation)

    /// <summary>
    /// Called at fixed 50Hz to smoothly interpolate visual position.
    /// SDK Reference: Player.ts lines 476-531
    /// 
    /// CRITICAL: This is what makes movement look smooth!
    /// - Runs at exactly 30 ticks per game tick (50Hz)
    /// - Interpolates from perceivedLocation to next path tile
    /// - Updates perceivedLocation (NOT gridPosition)
    /// - Handles rotation interpolation
    /// 
    /// FIXED: Now uses deterministic tick percent (0/30, 1/30, ... 29/30)
    /// </summary>
    public void ClientTick(float tickPercent)
    {
        // Based on: https://github.com/dennisdev/rs-map-viewer/blob/master/src/mapviewer/webgl/npc/Npc.ts#L115

        if (path.Count == 0)
        {
            // Not moving, stay at current position
            // But ensure perceived location matches grid position
            if (Vector2.Distance(perceivedLocation, new Vector2(gridPosition.x, gridPosition.y)) > EPSILON)
            {
                // Snap to grid if we're close but not exact
                perceivedLocation = new Vector2(gridPosition.x, gridPosition.y);
            }
            return;
        }

        float x = perceivedLocation.x;
        float y = perceivedLocation.y;
        Vector2Int nextTile = path[0].position;
        bool run = path[0].run;

        // Get current rotation angle
        float currentAngle = GetPerceivedRotation(tickPercent);

        // Calculate movement speed
        // SDK Reference: Player.ts lines 487-491
        // FIXED: Use deterministic speed based on client ticks per game tick
        const float baseMovementSpeed = 1f / (float)CLIENT_TICKS_PER_GAME_TICK; // 1/30 tile per client tick
        float movementSpeed = baseMovementSpeed;

        // ROTATION AFFECTS SPEED
        // If we need to rotate, move at half speed
        // SDK Reference: Player.ts lines 494-497
        bool canRotate = true; // Could be disabled in certain situations
        if (currentAngle != nextAngle && canRotate)
        {
            movementSpeed = baseMovementSpeed / 2f;
            if (ENABLE_POSITION_DEBUG) Debug.Log($"[CLIENT-TICK] Must rotate, half speed");
        }

        // PATH LENGTH AFFECTS SPEED (warping for long paths)
        // SDK Reference: Player.ts lines 498-507
        if (path.Count == 3)
        {
            movementSpeed = baseMovementSpeed * 1.5f;
            if (ENABLE_POSITION_DEBUG) Debug.Log($"[CLIENT-TICK] Path length 3, 1.5x speed");
        }
        else if (path.Count > 3)
        {
            movementSpeed = baseMovementSpeed * 2f;
            if (ENABLE_POSITION_DEBUG) Debug.Log($"[CLIENT-TICK] Path length 4+, 2x speed (warp)");
        }

        // RUNNING DOUBLES SPEED
        // SDK Reference: Player.ts line 508-511
        if (run)
        {
            movementSpeed *= 2f;
            if (ENABLE_POSITION_DEBUG) Debug.Log($"[CLIENT-TICK] Running, doubled speed");
        }

        // Interpolate toward next tile
        // SDK Reference: Player.ts lines 512-523
        float diffX = Mathf.Abs(x - nextTile.x);
        float diffY = Mathf.Abs(y - nextTile.y);

        if (diffX > EPSILON || diffY > EPSILON)
        {
            // Move toward target
            if (x < nextTile.x)
                x = Mathf.Min(x + movementSpeed, nextTile.x);
            else if (x > nextTile.x)
                x = Mathf.Max(x - movementSpeed, nextTile.x);

            if (y < nextTile.y)
                y = Mathf.Min(y + movementSpeed, nextTile.y);
            else if (y > nextTile.y)
                y = Mathf.Max(y - movementSpeed, nextTile.y);
        }

        // Update perceived position
        perceivedLocation = new Vector2(x, y);

        // Check if we reached the tile
        // SDK Reference: Player.ts lines 524-530
        diffX = Mathf.Abs(x - nextTile.x);
        diffY = Mathf.Abs(y - nextTile.y);

        if (diffX < EPSILON && diffY < EPSILON)
        {
            // Snap to exact tile
            perceivedLocation.x = nextTile.x;
            perceivedLocation.y = nextTile.y;

            if (ENABLE_POSITION_DEBUG)
            {
                Debug.Log($"[CLIENT-TICK] Reached tile {nextTile}, removing from path");
            }

            // Remove this tile from path
            path.RemoveAt(0);

            // Update resting angle if path is complete
            if (path.Count == 0)
            {
                restingAngle = nextAngle;
                if (ENABLE_POSITION_DEBUG) Debug.Log($"[CLIENT-TICK] Path complete");
            }
            else
            {
                // Update target angle for next tile
                nextAngle = GetTargetAngle();
            }
        }
    }

    #endregion

    #region Rotation System

    /// <summary>
    /// Get the current visual rotation angle (smoothly interpolated).
    /// SDK Reference: Player.ts lines 280-298
    /// 
    /// ROTATION SYSTEM:
    /// - Player can only rotate at RADIANS_PER_TICK speed
    /// - Uses shortest angle distance to determine direction
    /// - Smoothly interpolates toward nextAngle
    /// </summary>
    public float GetPerceivedRotation(float tickPercent)
    {
        // Calculate turn amount this frame
        // SDK Reference: Player.ts line 291
        float turnAmount = RADIANS_PER_TICK * Mathf.Max(0, tickPercent - lastTickPercent);
        lastTickPercent = tickPercent;

        // Calculate shortest angle distance
        // SDK Reference: Player.ts lines 292-293
        // Uses wrap-around logic to find shortest rotation direction
        float diff = (nextAngle - _angle + Mathf.PI * 2) % (Mathf.PI * 2);
        float direction = (diff - Mathf.PI) > 0 ? -1 : 1;

        // Rotate toward nextAngle
        // SDK Reference: Player.ts lines 294-298
        if (diff >= turnAmount)
        {
            _angle += turnAmount * direction;
        }
        else
        {
            _angle = nextAngle;
        }

        return _angle;
    }

    /// <summary>
    /// Get the target angle the player should face.
    /// SDK Reference: Player.ts lines 300-311
    /// 
    /// PRIORITY:
    /// 1. Face aggro target (if in combat)
    /// 2. Face movement direction (if moving)
    /// 3. Face resting angle (if idle)
    /// </summary>
    private float GetTargetAngle()
    {
        // Face aggro target
        if (aggro != null)
        {
            float angle = Pathing.Angle(
                perceivedLocation.x + size / 2f,
                perceivedLocation.y - size / 2f,
                aggro.gridPosition.x + aggro.size / 2f,
                aggro.gridPosition.y - aggro.size / 2f
            );
            return -angle; // Negate for Unity's coordinate system
        }

        // Face movement direction
        if (path.Count > 0)
        {
            float angle = Pathing.Angle(
                perceivedLocation.x, perceivedLocation.y,
                path[0].position.x, path[0].position.y
            );
            return -angle;
        }

        // Face resting angle (idle)
        return restingAngle;
    }

    #endregion

    #region Combat (TODO: Implement Later)

    // TODO: Combat system
    // SDK Reference: Player.ts lines 448-474 (attackIfPossible)
    // SDK Reference: Player.ts lines 603-617 (attack)

    public override void AttackStep()
    {
        base.AttackStep();

        // Process food/potion consumption
        eats.TickFood(this);

        // Regenerate special attack energy
        RegenerateSpecialAttack();

        // TODO: Implement player combat
        // For now, just call base implementation
    }

    #endregion

    #region Eating System (TODO: Implement Later)

    // TODO: Eating system
    // SDK Reference: Player.ts lines 87 (eats: Eating)
    // SDK Reference: Eating.ts for full implementation

    #endregion

    #region Prayer System (TODO: Implement Later)

    // TODO: Prayer system
    // SDK Reference: Player.ts lines 619-621 (activatePrayers)
    // SDK Reference: PrayerController.ts for full implementation

    #endregion

    #region Utility Methods

    public override string UnitName()
    {
        return "Player";
    }

    #endregion

    #region Dead State

    /// <summary>
    /// Handle death state.
    /// SDK Reference: Player.ts line 602
    /// </summary>
    public override void Dead()
    {
        base.Dead();
        perceivedLocation = new Vector2(gridPosition.x, gridPosition.y);
        destinationLocation = gridPosition;
    }

    #endregion

}