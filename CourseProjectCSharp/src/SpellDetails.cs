using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public enum SpellFlags
{
    Casting,
    Channeling,
    Damage,
    Heal,
    OverTime,
    AoE,
    Tankable,
    Soakable,
    DistanceFalloff,
    Avoidable,
    FriendlyFire,
}

public class SpellDetails
{
    public static readonly float DefaultMaxRange = 99999;
    public Resources ResourceCosts { get; }
    public float Range { get; }
    public TimeSpan CastDelay { get; }
    public TimeSpan CastTime { get; }
    public TimeSpan Duration { get; }
    public TimeSpan Cooldown { get; }
    public float GcdModifier { get; }
    public float SpellModifier { get; }
    public List<SpellFlags> Flags { get; }


    public SpellDetails()
    {
        ResourceCosts = Resources.CreateEmpty();
        Range = DefaultMaxRange;
        CastDelay = TimeSpan.Zero;
        CastTime = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        Cooldown = TimeSpan.Zero;
        GcdModifier = 1;
        SpellModifier = 1;
        Flags = new();
    }
}