using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public enum AttackFlags
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

public class AnimationSystem
{
    public void HandleAttackStart(CombatPacket packet)
    {
        // Start attack animation based on the details in the packet
    }
}

public class AudioSystem
{
    public void HandleAttackStart(CombatPacket packet)
    {
        // Start attack animation based on the details in the packet
    }
}

/// <summary>
/// Configurations for specifying the targets of an attack
/// </summary>
public class Targeting
{
    public static readonly Targeting Empty = new(CombatObject.Empty);
    public CombatObject Source { get; }
    public List<CombatObject> TargetedCombatObjects { get; private set; }
    public List<Position> TargetedPositions { get; private set; }
    public string Seed { get; private set; }
    public List<CombatObject> SelectedTargets { get; private set; }

    public Targeting(CombatObject source)
    {
        Source = source;
        TargetedCombatObjects = new();
        TargetedPositions = new();
        Seed = string.Empty;
        SelectedTargets = new();
    }
}

/// <summary>
/// Positional data for a unit or an area-based attack
/// </summary>
public class Position
{
    public static readonly Position Empty = new();
    public float X { get; private set; }
    public float Y { get; private set; }
    public int Realm { get; private set; } // Units can't see/interact cross-realm
    public bool AffectedByAoE { get; private set; }
    private static readonly int defaultX = 9999;
    private static readonly int defaultY = 9999;

    public Position()
    {
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
/// Contains combat units and controls the flow of attacks
/// </summary>
public class CombatEncounter
{
    public static readonly CombatEncounter Empty = new();
    public CombatObject Root { get; private set; }
    public CombatSystem CombatSystem { get; private set; }
    public TimeSpan EncounterTimer { get; private set; }
    public bool GameIsPaused { get; private set; }
    public bool EncounterIsPaused { get; private set; }
    public int UpdatesPerSecond { get; private set; }
    private readonly int defaultUpdatesPerSecond = 100;
    private TimeSpan updateInterval;

    public CombatEncounter()
    {
        Root = new();
        CombatSystem = new();
        EncounterTimer = TimeSpan.Zero;
        GameIsPaused = false;
        EncounterIsPaused = false;
        SetUpdatesPerSecond(defaultUpdatesPerSecond);
    }

    public void ProcessCombat()
    {
        if (!GameIsPaused)
        {
            Root.VisitAllScheduledSpells(script => script.AttemptSpellCast(EncounterTimer, CombatSystem));
            Root.VisitDescendants(script => script.IncrementTimeSpentInEncounter(updateInterval));
            EncounterTimer += updateInterval;
        }
    }

    public void SetUpdatesPerSecond(int updatesPerSecond)
    {
        UpdatesPerSecond = updatesPerSecond;
        updateInterval = CalculateUpdateInterval(updatesPerSecond);
    }

    private static TimeSpan CalculateUpdateInterval(int updatesPerSecond)
    {
        return TimeSpan.FromTicks(TimeSpan.TicksPerSecond / updatesPerSecond);
    }
}

/// <summary>
/// A unit that is part of combat and targetable by spells
/// </summary>
public class CombatObject
{
    public static readonly CombatObject Empty = new();
    public static readonly float DefaultPlayerHP = 1000;
    public Position Position { get; private set; }
    public TimeSpan TimeSpentInEncounter { get; private set; }
    public bool IsPlayerControlled { get; private set; }
    public bool IsEnemy { get; private set; }
    public bool IsInCombat { get; private set; }
    public bool HasHP { get; private set; }
    public float MaxHP { get; set; }
    public float CurrentHP { get; private set; }
    public List<Spell> ScheduledSpells { get; set; }
    public CombatObject Parent { get; private set; }
    public List<CombatObject> Children { get; private set; }
    public CombatObject()
    {
        Position = Position.Empty;
        TimeSpentInEncounter = TimeSpan.Zero;
        IsEnemy = false;
        IsInCombat = true;
        HasHP = false;
        MaxHP = 1;
        CurrentHP = MaxHP;
        ScheduledSpells = new();
        Parent = Empty;
        Children = new();
    }

    public void VisitDescendants(Action<CombatObject> action)
    {
        action(this); // Perform action on current object
        foreach (CombatObject child in Children)
        {
            child.VisitDescendants(action); // Recursively act on each child
        }
    }

    public void VisitAllScheduledSpells(Action<Spell> action)
    {
        foreach (Spell script in ScheduledSpells)
        {
            action(script); // Apply action to each CombatScript in current object
        }
        foreach (CombatObject child in Children)
        {
            child.VisitAllScheduledSpells(action); // Recursively repeat on children
        }
    }

    public void SpawnChild(INpc childTemplate)
    {
        CombatObject child = new()
        {
            Parent = this,
        };
        child.ScheduleSpell(childTemplate.Spell());
    }

    public void ScheduleSpell(Spell spell)
    {
        spell.OffsetActivationTimeStamp(TimeSpentInEncounter);
        ScheduledSpells.Add(spell);
    }

    public void IncrementTimeSpentInEncounter(TimeSpan deltaTime)
    {
        TimeSpentInEncounter += deltaTime;
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
    }
}

public class Spell
{
    public static readonly Spell Empty = new(CombatObject.Empty, EmptySpellEffect.Empty, TimeSpan.Zero);
    public CombatObject Source { get; }
    public Targeting Destination { get; }
    public ISpellEffect SpellEffect { get; }
    public TimeSpan ActivationTimeStamp { get; private set; }
    public bool HasBeenCast { get; private set; }

    public Spell(CombatObject source, ISpellEffect spellEffect, TimeSpan timeStamp)
    {
        Source = source;
        Destination = new(Source);
        SpellEffect = spellEffect;
        ActivationTimeStamp = timeStamp;
        HasBeenCast = false;
    }

    public void AttemptSpellCast(TimeSpan encounterTime, CombatSystem combatSystem)
    {
        if (encounterTime > ActivationTimeStamp && !HasBeenCast)
        {
            HasBeenCast = true;
            combatSystem.CastSpell(this);
        }
    }

    public void OffsetActivationTimeStamp(TimeSpan offset)
    {
        ActivationTimeStamp += offset;
    }
}

/// <summary>
/// Utility functions for applying a spell effect to its targets
/// </summary>
public static class SpellEffectTemplate
{
    public static void DealDamage(List<CombatObject> targets, float damage)
    {
        foreach (var target in targets)
        {
            target.ReduceCurrentHP(damage);
        }
    }
}

public static class TargetingTemplate
{
    public static List<CombatObject> TargetAllEnemies(List<CombatObject> possibleTargets, CombatObject attacker)
    {
        List<CombatObject> enemies = new();
        foreach (var target in possibleTargets)
        {
            if (target.IsEnemy != attacker.IsEnemy)
            {
                enemies.Add(target);
            }
        }
        return enemies;
    }
}

public interface INpc
{
    string NpcID();
    float BaseHP();
    Spell Spell();
}

public class EmptyNpc : INpc
{
    public string NpcID() => "Npc0000";
    public float BaseHP() => 9999;
    public Spell Spell() => GlobalNameSpace.Spell.Empty;
}

public class Npc0001 : INpc
{
    public string NpcID() => "Npc0001";
    public float BaseHP() => 20;
    public Spell Spell() => GlobalNameSpace.Spell.Empty;
}

/// <summary>
/// Algorithm or logic specifying the behavior of a spell
/// </summary>
public interface ISpellEffect
{
    string SpellID();
    List<TimeSpan> FollowUpTimeStamps();
    Targeting SelectTargets(Targeting destination, CombatObject root);
    void ExecuteSpellEffect(CombatPacket packet);
}

public class EmptySpellEffect : ISpellEffect
{
    public static readonly ISpellEffect Empty = new EmptySpellEffect();
    public string SpellID() => "Spell0000";
    public List<TimeSpan> FollowUpTimeStamps() => new();
    public Targeting SelectTargets(Targeting destination, CombatObject root)
    {
        return Targeting.Empty;
    }
    public void ExecuteSpellEffect(CombatPacket packet)
    {
        // Empty
    }
}

public class Spell0001 : ISpellEffect
{
    public string SpellID() => "Spell0001";
    public List<TimeSpan> FollowUpTimeStamps() => new();
    public Targeting SelectTargets(Targeting destination, CombatObject root)
    {
        return Targeting.Empty;
    }
    public void ExecuteSpellEffect(CombatPacket packet)
    {
        // Empty
    }
}

public class Spell0002 : ISpellEffect
{
    public string SpellID() => "Spell0002";
    public List<TimeSpan> FollowUpTimeStamps() => new();
    private readonly float damage = 10;
    public Targeting SelectTargets(Targeting destination, CombatObject root)
    {
        return Targeting.Empty;
    }
    public void ExecuteSpellEffect(CombatPacket packet)
    {
        SpellEffectTemplate.DealDamage(packet.Targets, damage);
    }
}

/// <summary>
/// Configurations for an ongoing attack
/// </summary>
public class CombatPacket
{
    public CombatObject Source { get; private set; }
    public Targeting Destination { get; private set; }
    public List<CombatObject> Targets { get; private set; }
    public ISpellEffect SpellEffect { get; private set; }
    public int ExecutionCycle { get; private set; }
    public bool AttackIsSuccesful { get; private set; }
    public AttackFlags AttackType { get; private set; }

    public CombatPacket()
    {
        Source = CombatObject.Empty;
        Destination = Targeting.Empty;
        Targets = Destination.TargetedCombatObjects;
        SpellEffect = EmptySpellEffect.Empty;
        ExecutionCycle = 0;
    }

    public void LoadSpell(Spell spell)
    {
        Source = spell.Source;
        Destination = spell.Destination;
        Targets = spell.Destination.TargetedCombatObjects;
        SpellEffect = spell.SpellEffect;
    }

    public void ResetPacket()
    {
        Source = CombatObject.Empty;
        Destination = Targeting.Empty;
        Targets = Destination.TargetedCombatObjects;
        SpellEffect = EmptySpellEffect.Empty;
    }
}

/// <summary>
/// Manages and stores ongoing attack packets
/// </summary>
public class CombatManager
{
    private readonly Queue<CombatPacket> packetPool;

    public CombatManager(int poolSize)
    {
        packetPool = new Queue<CombatPacket>(poolSize);
        // Initialize the pool with pre-allocated combat packets
        // We are reusing combat packets instead of creating new ones to avoid memory fragmentation
        // Inspired by www.youtube.com/watch?v=ltO_rMJJdHE&t=651s
        for (int i = 0; i < poolSize; i++)
        {
            packetPool.Enqueue(new CombatPacket());
        }
    }

    public CombatPacket RequestPacket()
    {
        if (packetPool.Count > 0)
        {
            return packetPool.Dequeue(); // Get an available packet
        }
        else
        {
            return new CombatPacket(); // Optionally create a new one if pool is empty
        }
    }

    public void ReleasePacket(CombatPacket packet)
    {
        packet.ResetPacket();
        packetPool.Enqueue(packet); // Return packet to the pool
    }
}

/// <summary>
/// Processes incoming attack packet / combat packets
/// </summary>
public class CombatSystem
{
    public event Action<CombatPacket>? OnAttackStart;
    public event Action<CombatPacket>? OnAttackComplete;

    private readonly CombatManager combatManager;

    public CombatSystem()
    {
        combatManager = new CombatManager(10); // Example pool size
    }

    public void CastSpell(Spell spell)
    {
        var packet = combatManager.RequestPacket();
        packet.LoadSpell(spell);
        HandleAttack(packet);
        combatManager.ReleasePacket(packet);
    }

    private void HandleAttack(CombatPacket packet)
    {
        OnAttackStart?.Invoke(packet);
        packet.SpellEffect.ExecuteSpellEffect(packet);
        OnAttackComplete?.Invoke(packet);
    }
}
