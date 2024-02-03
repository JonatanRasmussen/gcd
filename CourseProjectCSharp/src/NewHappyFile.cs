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
/// Configurations for determining the targets of an attack
/// </summary>
public class Targeting
{
    public static readonly Targeting Empty = new();
    public List<CombatObject> Targets { get; set; }
    public List<Position> TargetLocation { get; set; }
    public ITargetStrategy TargetStrategy { get; set; }
    public string TargetStrategyInput { get; set; }

    public Targeting()
    {
        Targets = new();
        TargetLocation = new();
        TargetStrategy = new EmptyTargetStrategy();
        TargetStrategyInput = string.Empty;
    }

    public void SelectTargets(List<CombatObject> combatObjects)
    {
        Targets = TargetStrategy?.Execute(TargetStrategyInput, combatObjects) ?? new();
    }
}

/// <summary>
/// Positional data for a unit or an area-based attack
/// </summary>
public class Position
{
    public static readonly Position Empty = new();
    public float X { get; set; }
    public float Y { get; set; }
    public int Realm { get; set; } // Units can't see/interact cross-realm
    public bool AffectedByAoE { get; set; }
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
/// Time-based trigger to initiate scheduled spell casts
/// </summary>
public class TimeTracker
{
    public static readonly TimeTracker Empty = new();
    public TimeSpan? InternalTimer { get; set; }
    public TimeSpan Offset { get; set; }
    public TimeSpan? ActivationTimeStamp { get; private set; }
    public bool ActivationTimeStampIsNow { get; set; }
    private TimeSpan? timeStart = null;
    private TimeSpan timeEnd = TimeSpan.Zero;

    public TimeTracker()
    {
        InternalTimer = null;  //Starts at 0, regardless of external timer
        Offset = TimeSpan.Zero;
        ActivationTimeStamp = null; //TimeStamp 00:00 is triggered on SECOND update, not first
    }

    public static TimeTracker CreateFromTimeStamp(int minutes, int seconds, int milliseconds)
    {
        TimeTracker timeTracker = new();
        timeTracker.SetActivation(minutes, seconds, milliseconds);
        return timeTracker;
    }

    public void Update(TimeSpan externalTimer)
    {
        if (InternalTimer == null)  //Run this the first time the method is called
        {
            Offset = externalTimer;
            UpdateInternalTimer(externalTimer);
        }
        else if (ActivationTimeStamp != null) //Run this if an ActivationTimeStamp exists and is unreached
        {
            timeStart = InternalTimer ?? TimeSpan.Zero; //before updating the internal timer
            UpdateInternalTimer(externalTimer);
            timeEnd = InternalTimer ?? TimeSpan.Zero; //after updating the internal timer
        }
        else  // Increment internal timer to keep up with updated external timer
        {
            UpdateInternalTimer(externalTimer);
        }
    }

    public void ScheduleNewActivationTime(TimeSpan nextActivation)
    {
        ActivationTimeStamp += nextActivation;
    }

    public void RemoveActivationTime()
    {
        ActivationTimeStamp = null;
        timeStart = null;
    }

    public bool IsActivationTimeStampReached()
    {
        if (ActivationTimeStamp != null && timeStart != null)
        {
            return (ActivationTimeStamp >= timeStart && ActivationTimeStamp < timeEnd);
        }
        return false;
    }

    private void SetActivation(int minutes, int seconds, int milliseconds)
    {
        int totalMilliseconds = milliseconds + 1000 * (seconds + 60 * minutes);
        ActivationTimeStamp = TimeSpan.FromMilliseconds(totalMilliseconds);
    }

    private void UpdateInternalTimer(TimeSpan externalTimer)
    {
        // Increment internal timer to keep up with external timer
        InternalTimer = externalTimer - Offset;
    }
}

/// <summary>
/// Algorithm or logic for selecting the targets of an attack
/// </summary>
public interface ITargetStrategy
{
    string SpellID();
    List<CombatObject> Execute(string input, List<CombatObject> combatObjects);
}

public class EmptyTargetStrategy : ITargetStrategy
{
    public string SpellID() => "T0000";

    public List<CombatObject> Execute(string input, List<CombatObject> combatObjects)
    {
        return combatObjects;
    }
}

/// <summary>
/// Configuration for an attack
/// </summary>
public class Spell
{
    public static readonly Spell Empty = new();
    public CombatObject Attacker { get; set; } // Source
    public Targeting Targeting { get; set; }
    public ISpellEffect SpellEffect { get; set; }
    public int ExecutionCycle { get; set; }

    public Spell()
    {
        Attacker = CombatObject.Empty;
        Targeting = new();
        SpellEffect = new EmptySpellEffect();
        ExecutionCycle = 0;
    }

    public static Spell CreateFromSpellEffect(CombatObject attacker, ISpellEffect spellEffect)
    {
        return new()
        {
            Attacker = attacker,
            Targeting = spellEffect.DefaultTargeting(),
            SpellEffect = spellEffect,
        };
    }

    public void IncrementExecutionCycle()
    {
        ExecutionCycle += 1;
    }
}

/// <summary>
/// Configuration for scheduling an upcoming attack via a timer
/// </summary>
public class ScheduledSpell
{
    public static readonly ScheduledSpell Empty = new();
    public Spell Spell { get; set; }
    public TimeTracker Timer { get; set; }
    public int Casts { get; set; }

    public ScheduledSpell()
    {
        Spell = new();
        Timer = new();
        Casts = 0;
    }

    public void ScheduleFollowUpSpellEffects()
    {
        List<TimeSpan> followUpEffects = Spell.SpellEffect.FollowUpTimeStamps();
        if (Casts < followUpEffects.Count)
        {
            TimeSpan timeStamp = followUpEffects[Casts];
            Timer.ScheduleNewActivationTime(timeStamp);
            Casts += 1;
        }
        else
        {
            Timer.RemoveActivationTime();
        }
    }
}

public class SpellScheduleGenerator
{
    public static readonly string Empty = string.Empty;
    public CombatObject Attacker { get; set; }
    public List<ScheduledSpell> ScheduledSpells { get; set; }
    public int NeinNein { get; set; }
    private readonly int nein = 9;

    public SpellScheduleGenerator()
    {
        Attacker = CombatObject.Empty;
        ScheduledSpells = new();
        NeinNein = 99;
    }

    public void Schedule(ISpellEffect spellEffect, int minute, int second, int millisecond)
    {
        ScheduledSpells.Add(
            new()
            {
                Spell = Spell.CreateFromSpellEffect(Attacker, spellEffect),
                Timer = TimeTracker.CreateFromTimeStamp(minute, second, millisecond),
            }
        );
    }
}

/// <summary>
/// Handles whether or not an attack should be initiated
/// </summary>
public class SpellCasterObject
{
    public static readonly SpellCasterObject Empty = new();
    public CombatObject Attacker { get; set; }
    public List<ScheduledSpell> ScheduledSpells { get; set; }
    public List<Spell> SpellQueue { get; set; }
    public SpellCasterObject()
    {
        Attacker = CombatObject.Empty;
        ScheduledSpells = new();
        SpellQueue = new();
    }

    public void GenerateSpellQueue(TimeSpan externalTimer)
    {
        SpellQueue.Clear();
        ListenForPlayerInput();
        foreach (var scheduledSpell in ScheduledSpells)
        {
            scheduledSpell.Timer.Update(externalTimer);;
            while (scheduledSpell.Timer.IsActivationTimeStampReached())
            {
                SpellQueue.Add(scheduledSpell.Spell);
                scheduledSpell.ScheduleFollowUpSpellEffects();
            }
        }
    }

    public void ValidateSpellQueue(List<CombatObject> combatObjects)
    {
        // Iterate backwards to safely remove elements
        for (int i = SpellQueue.Count - 1; i >= 0; i--)
        {
            var spell = SpellQueue[i];
            spell.Targeting.SelectTargets(combatObjects);
            if (!SpellCastIsValid(spell))
            {
                SpellQueue.RemoveAt(i);
            }
        }
    }

    public void ExecuteSpellQueue(CombatSystem combatSystem)
    {
        foreach (var spell in SpellQueue)
        {
            combatSystem.StartAttack(spell);
            spell.IncrementExecutionCycle();
        }
    }

    private static bool SpellCastIsValid(Spell spell)
    {
        return true;
    }

    private static void ListenForPlayerInput()
    {
        //Empty;
    }
}

/// <summary>
/// Contains combat units and controls the flow of attacks
/// </summary>
public class CombatEncounter
{
    public static readonly CombatEncounter Empty = new();
    public CombatSystem CombatSystem { get; set; }
    public List<CombatObject> CombatObjects { get; set; }
    public List<SpellCasterObject> SpellCasterObjects { get; set; }
    public TimeSpan EncounterTimer { get; set; }
    public int UpdatesPerSecond { get; private set; }
    public bool CombatInProgress { get; set; }
    private readonly int defaultUpdateRate = 30;
    private TimeSpan updateInterval;

    public CombatEncounter()
    {
        CombatSystem = new();
        CombatObjects = new();
        SpellCasterObjects = new();
        EncounterTimer = TimeSpan.Zero;
        SetUpdatesPerSecond(defaultUpdateRate);
    }

    public void ProcessCombat()
    {
        foreach (var spellCasterObject in SpellCasterObjects)
        {
            spellCasterObject.GenerateSpellQueue(EncounterTimer);
            spellCasterObject.ValidateSpellQueue(CombatObjects);
            spellCasterObject.ExecuteSpellQueue(CombatSystem);
        }
        EncounterTimer += updateInterval;
    }

    public void SetUpdatesPerSecond(int updatesPerSecond)
    {
        UpdatesPerSecond = updatesPerSecond;
        updateInterval = CalculateUpdateInterval();
    }

    private TimeSpan CalculateUpdateInterval()
    {
        return TimeSpan.FromTicks(TimeSpan.TicksPerSecond / UpdatesPerSecond);
    }
}


/// <summary>
/// A unit that is part of combat and targetable by spells
/// </summary>
public class CombatObject
{
    public static readonly CombatObject Empty = new();
    public Position? Position { get; set; }
    public bool IsInCombat { get; set; }
    public bool HasHP { get; set; }
    public float MaxHP { get; set; }
    public float CurrentHP { get; set; }
    public CombatObject? Parent { get; set; }
    public CombatObject()
    {
        HasHP = false;
        MaxHP = 1;
        CurrentHP = MaxHP;
        Parent = null;
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

/// <summary>
/// Utility functions for applying a spell effect to its targets
/// </summary>
public static class SpellTemplate
{
    public static void DealDamage(List<CombatObject> targets, float damage)
    {
        foreach (var target in targets)
        {
            target.ReduceCurrentHP(damage);
        }
    }
}

/// <summary>
/// Algorithm or logic specifying the behavior of a spell
/// </summary>
public interface ISpellEffect
{
    string SpellID();
    List<TimeSpan> FollowUpTimeStamps();
    Targeting DefaultTargeting();
    void Execute(CombatPacket packet);
}

public class EmptySpellEffect : ISpellEffect
{
    public string SpellID() => "Spell0000";
    public List<TimeSpan> FollowUpTimeStamps() => new();
    public Targeting DefaultTargeting()
    {
        return Targeting.Empty;
    }
    public void Execute(CombatPacket packet)
    {
        // Empty
    }
}

public class Spell0001 : ISpellEffect
{
    public string SpellID() => "Spell0001";
    public List<TimeSpan> FollowUpTimeStamps() => new();
    public Targeting DefaultTargeting()
    {
        return Targeting.Empty;
    }
    public void Execute(CombatPacket packet)
    {
        // Empty
    }
}

public class Spell0002 : ISpellEffect
{
    public string SpellID() => "Spell0002";
    public List<TimeSpan> FollowUpTimeStamps() => new();
    private readonly float damage = 10;
    public Targeting DefaultTargeting()
    {
        return Targeting.Empty;
    }
    public void Execute(CombatPacket packet)
    {
        SpellTemplate.DealDamage(packet.Targets, damage);
    }
}

/// <summary>
/// Configurations for an ongoing attack
/// </summary>
public class CombatPacket
{
    public CombatObject? Attacker { get; set; }
    public Targeting? Targeting { get; set; }
    public List<CombatObject> Targets { get; set; }
    public ISpellEffect? SpellEffect { get; set; }
    public int ExecutionCycle { get; set; }
    public bool AttackIsSuccesful { get; set; }
    public AttackFlags AttackType { get; set; }

    public CombatPacket()
    {
        Attacker = null;
        Targeting = null;
        Targets = new();
        SpellEffect = null;
        ExecutionCycle = 0;
    }

    public void LoadSpell(Spell spell)
    {
        Attacker = spell.Attacker;
        Targeting = spell.Targeting;
        Targets = spell.Targeting.Targets;
        SpellEffect = spell.SpellEffect;
        ExecutionCycle = spell.ExecutionCycle;
    }

    public void ResetPacket()
    {
        Attacker = null;
        Targeting = null;
        SpellEffect = null;
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

    public void StartAttack(Spell spell)
    {
        var packet = combatManager.RequestPacket();
        packet.LoadSpell(spell);
        HandleAttack(packet);
        combatManager.ReleasePacket(packet);
    }

    private void HandleAttack(CombatPacket packet)
    {
        OnAttackStart?.Invoke(packet);
        packet.SpellEffect?.Execute(packet);
        OnAttackComplete?.Invoke(packet);
    }
}
