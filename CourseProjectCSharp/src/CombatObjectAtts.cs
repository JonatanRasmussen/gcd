using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

/// <summary>
/// Positional data for a unit or an area-based attack
/// </summary>
public class Position
{
    public CombatObject Source { get; }
    public float X { get; private set; }
    public float Y { get; private set; }
    public int Realm { get; private set; } // Units can't see/interact cross-realm
    public bool AffectedByAoE { get; private set; }
    private static readonly int defaultX = 9999;
    private static readonly int defaultY = 9999;

    public Position(CombatObject source)
    {
        Source = source;
        X = defaultX;
        Y = defaultY;
        Realm = 0;
        AffectedByAoE = true;
    }

    public void Update(float x, float y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Configurations for specifying the targets of an attack
/// </summary>
public class Targeting
{
    public CombatObject Source { get; }
    public List<CombatObject> Targets { get; private set; }
    public List<Position> AreaTargets { get; private set; }
    public string Seed { get; private set; }

    public Targeting(CombatObject source)
    {
        Source = source;
        Targets = new();
        AreaTargets = new();
        Seed = string.Empty;
    }
}

public class Resources
{
    public CombatObject Source { get; }
    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }

    public Resources(CombatObject source)
    {
        Source = source;
        MaxHP = 0;
        CurrentHP = 0;
    }

    public static Resources CreateEmpty()
    {
        return new(CombatObject.CreateEmpty());
    }

    public void SetMaxHP(float newMaxHP)
    {
        RaiseMaxHP(newMaxHP);
        IncreaseCurrentHP(newMaxHP);
    }

    public void RaiseMaxHP(float increasedHP)
    {
        MaxHP += increasedHP;
    }

    public void LowerMaxHP(float reducedHP)
    {
        MaxHP -= reducedHP;
    }

    public void IncreaseCurrentHP(float healing)
    {
        CurrentHP += healing;
    }

    public void ReduceCurrentHP(float damage)
    {
        CurrentHP -= damage;
        Console.WriteLine($"{Source.NpcID} received {damage} damage! HP is now: {CurrentHP}");
    }
}