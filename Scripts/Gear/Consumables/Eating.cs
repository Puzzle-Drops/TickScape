using UnityEngine;

/// <summary>
/// Manages food and potion consumption delays.
/// SDK Reference: Eating.ts
/// 
/// OSRS EATING SYSTEM:
/// - Food delay: 3 ticks (1.8 seconds) between eating
/// - Potion delay: Shares food delay
/// - Combo delay: Karambwan has separate 3-tick delay
/// - Attack delay: +3 ticks added when eating
/// 
/// CONSUMPTION PATTERN:
/// 1. Player clicks food/potion (eatFood/drinkPotion)
/// 2. Item stored in currentFood/currentPotion
/// 3. Delay set to 3 ticks
/// 4. Next tick: tickFood() consumes item and applies effect
/// 5. Delay counts down until can eat again
/// 
/// TICK SEQUENCE EXAMPLE:
/// Tick 0: Click shark → currentFood = shark, foodDelay = 3
/// Tick 1: tickFood() eats shark (heal 20), foodDelay = 2
/// Tick 2: foodDelay = 1
/// Tick 3: foodDelay = 0
/// Tick 4: Can eat again (foodDelay <= 0)
/// </summary>
public class Eating
{
    public Player player;

    // Consumption delays (in ticks)
    public int foodDelay = 0;
    public int potionDelay = 0;
    public int comboDelay = 0;

    // Queued consumables (processed next tick)
    public Food currentFood;
    public Potion currentPotion;
    public Octbwan currentComboFood;

    /// <summary>
    /// Process food/potion consumption and tick down delays.
    /// Called every game tick from Player.attackStep().
    /// SDK Reference: Eating.tickFood() in Eating.ts lines 17-36
    /// 
    /// TICK ORDER:
    /// 1. Decrement all delays
    /// 2. Consume currentFood if queued
    /// 3. Consume currentPotion if queued
    /// 4. Consume currentComboFood if queued
    /// 5. Add attack delay for each consumption
    /// </summary>
    public void TickFood(Player player)
    {
        // Tick down delays
        foodDelay--;
        potionDelay--;
        comboDelay--;

        // Consume regular food
        if (currentFood != null)
        {
            currentFood.Eat(player);
            player.attackDelay += 3; // Add 3 ticks to attack delay
            currentFood = null;
        }

        // Consume potion
        if (currentPotion != null)
        {
            currentPotion.Drink(player);
            currentPotion = null;
        }

        // Consume combo food (Karambwan)
        if (currentComboFood != null)
        {
            currentComboFood.Eat(player);
            player.attackDelay += 3; // Add 3 ticks to attack delay
            currentComboFood = null;
        }
    }

    /// <summary>
    /// Can player eat food?
    /// SDK Reference: Eating.canEatFood() in Eating.ts lines 38-40
    /// </summary>
    public bool CanEatFood()
    {
        return foodDelay <= 0;
    }

    /// <summary>
    /// Can player drink potion?
    /// SDK Reference: Eating.canDrinkPotion() in Eating.ts lines 42-44
    /// </summary>
    public bool CanDrinkPotion()
    {
        return potionDelay <= 0;
    }

    /// <summary>
    /// Can player eat combo food (Karambwan)?
    /// SDK Reference: Eating.canEatComboFood() in Eating.ts lines 46-48
    /// </summary>
    public bool CanEatComboFood()
    {
        return comboDelay <= 0;
    }

    /// <summary>
    /// Queue food for consumption next tick.
    /// SDK Reference: Eating.eatFood() in Eating.ts lines 50-59
    /// 
    /// IMPORTANT: Food is consumed NEXT TICK, not immediately.
    /// This allows multiple clicks to queue different items.
    /// Pattern: food → potion → karambwan (forced by delay hierarchy)
    /// </summary>
    public void EatFood(Food food)
    {
        // Check if can eat
        if (!CanEatFood())
        {
            Debug.LogWarning("[EATING] Cannot eat yet - food delay active");
            return;
        }

        // Queue food for next tick
        currentFood = food;
        foodDelay = 3;

        // Consume from inventory
        if (currentFood != null)
        {
            currentFood.ConsumeItem(player);
        }

        Debug.Log($"[EATING] Queued food: {food.itemName}");
    }

    /// <summary>
    /// Queue potion for consumption next tick.
    /// SDK Reference: Eating.drinkPotion() in Eating.ts lines 61-71
    /// 
    /// IMPORTANT: Potions share food delay and potion delay.
    /// Both delays are set to 3 ticks when drinking.
    /// </summary>
    public bool DrinkPotion(Potion potion)
    {
        // Check if can drink
        if (!CanDrinkPotion())
        {
            //Debug.LogWarning("[EATING] Cannot drink yet - potion delay active");
            return false;
        }

        // Queue potion for next tick
        currentPotion = potion;
        foodDelay = 3;     // Shares food delay
        potionDelay = 3;   // Also sets potion delay

        // Decrement dose count
        if (currentPotion != null)
        {
            currentPotion.doses--;
        }

        Debug.Log($"[EATING] Queued potion: {potion.itemName}");
        return true;
    }

    /// <summary>
    /// Queue Karambwan for combo eating next tick.
    /// SDK Reference: Eating.eatComboFood() in Eating.ts lines 73-83
    /// 
    /// COMBO EATING MECHANIC:
    /// - Karambwan has separate comboDelay
    /// - Can be eaten same tick as regular food
    /// - Sets all three delays (food, potion, combo)
    /// - Allows "tick-perfect" 40+ HP healing
    /// </summary>
    public void EatComboFood(Octbwan octbwan)
    {
        // Check if can combo eat
        if (!CanEatComboFood())
        {
            Debug.LogWarning("[EATING] Cannot combo eat yet - combo delay active");
            return;
        }

        // Queue karambwan for next tick
        foodDelay = 3;
        potionDelay = 3;
        currentComboFood = octbwan;
        comboDelay = 3;

        // Consume from inventory
        if (currentComboFood != null)
        {
            currentComboFood.ConsumeItem(player);
        }

        Debug.Log($"[EATING] Queued combo food: Octbwan");
    }
}
