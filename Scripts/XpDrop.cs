using System.Collections.Generic;

/// <summary>
/// Tracks experience points gained from combat actions.
/// SDK Reference: XpDrop.ts
/// </summary>
[System.Serializable]
public class XpDrop
{
    public string skill;
    public float xp;

    public XpDrop(string skill, float xp)
    {
        this.skill = skill;
        this.xp = xp;
    }
}

/// <summary>
/// Aggregates XP drops by skill for batch granting.
/// SDK Reference: XpDropAggregator in XpDrop.ts
/// </summary>
public class XpDropAggregator : Dictionary<string, float>
{
    public void AddXp(XpDrop drop)
    {
        if (!ContainsKey(drop.skill))
        {
            this[drop.skill] = 0;
        }
        this[drop.skill] += drop.xp;
    }

    public void Clear()
    {
        base.Clear();
    }
}
