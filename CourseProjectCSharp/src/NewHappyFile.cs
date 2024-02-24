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
/// Time-based trigger to initiate scheduled spell casts
/// </summary>
public class TimeTracker
{
    public static readonly TimeTracker Empty = new();
    public TimeSpan? InternalTimer { get; private set; }
    public TimeSpan Offset { get; private set; }
    public TimeSpan? ActivationTimeStamp { get; private set; }
    public bool ActivationTimeStampIsNow { get; private set; }
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
        ActivationTimeStamp = InternalTimer + nextActivation;
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
/// Configuration for an attack
/// </summary>
public class Spell
{
    public static readonly ISpellEffect EmptySpellEffect = new EmptySpellEffect();
    public static readonly Spell Empty = new Spell(CombatObject.Empty, EmptySpellEffect);
    public CombatObject Source { get; }
    public Targeting Destination { get; }
    public ISpellEffect SpellEffect { get; }
    public int ExecutionCycle { get; private set; }

    public Spell(CombatObject source, ISpellEffect spellEffect)
    {
        Source = source;
        Destination = new(Source);
        SpellEffect = spellEffect;
        ExecutionCycle = 0;
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
    public static readonly ISpellSchedule EmptySpellSchedule = new EmptySpellSchedule();
    public static readonly ScheduledSpell Empty = new(Spell.Empty, TimeTracker.Empty);
    public Spell Spell { get; }
    public TimeTracker Timer { get; }
    public int Casts { get; private set; }

    public ScheduledSpell(Spell spell, TimeTracker timer)
    {
        Spell = spell;
        Timer = timer;
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
    public CombatObject Source { get; }
    public List<ScheduledSpell> ScheduledSpells { get; }

    public SpellScheduleGenerator()
    {
        Source = CombatObject.Empty;
        ScheduledSpells = new();
    }

    public void Schedule(ISpellEffect spellEffect, int minute, int second, int millisecond)
    {
        Spell spell = new Spell(Source, spellEffect);
        TimeTracker timer = TimeTracker.CreateFromTimeStamp(minute, second, millisecond);
        ScheduledSpell scheduledSpell = new(spell, timer);
        ScheduledSpells.Add(scheduledSpell);
    }
}

public interface ISpellSchedule
{
    List<Tuple<int, int, int, ISpellEffect>> Load();
}

public class EmptySpellSchedule : ISpellSchedule
{
    public List<Tuple<int, int, int, ISpellEffect>> Load()
    {
        return new();
    }
}

public class SpellSchedule0001 : ISpellSchedule
{
    public List<Tuple<int, int, int, ISpellEffect>> Load()
    {
        return new List<Tuple<int, int, int, ISpellEffect>>
        {
            new (0, 0, 0, new EmptySpellEffect()),
            new (0, 2, 500, new EmptySpellEffect()),
        };
    }
}

/// <summary>
/// Handles whether or not an attack should be initiated
/// </summary>
public class SpellCasterObject
{
    public static readonly SpellCasterObject Empty = new(CombatObject.Empty, ScheduledSpell.EmptySpellSchedule);
    public CombatObject Caster { get; }
    public ISpellSchedule SpellSchedule { get; }
    public List<ScheduledSpell> ScheduledSpells { get; private set; }
    public List<Spell> SpellQueue { get; private set; }
    public SpellCasterObject(CombatObject caster, ISpellSchedule spellSchedule)
    {
        Caster = caster;
        SpellSchedule = spellSchedule;
        ScheduledSpells = new();
        SpellQueue = new();
    }

    public void PopulateSpellQueue(TimeSpan externalTimer)
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

    public void ValidateSpellQueue(List<CombatObject> possibleTargets)
    {
        // Iterate backwards to safely remove elements
        for (int i = SpellQueue.Count - 1; i >= 0; i--)
        {
            var spell = SpellQueue[i];
            var destination = spell.Destination;
            spell.SpellEffect.SelectTargets(destination, possibleTargets);
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

    public void LoadScheduledSpells()
    {
        var schedule = SpellSchedule.Load();
        foreach (var config in schedule)
        {
            int minute = config.Item1;
            int second = config.Item2;
            int millisecond = config.Item3;
            ISpellEffect spellEffect = config.Item4;
            Schedule(Caster, minute, second, millisecond, spellEffect);
        }
    }

    public void Schedule(CombatObject source, int minute, int second, int millisecond, ISpellEffect spellEffect)
    {
        Spell spell = new Spell(source, spellEffect);
        TimeTracker timer = TimeTracker.CreateFromTimeStamp(minute, second, millisecond);
        ScheduledSpell scheduledSpell = new(spell, timer);
        ScheduledSpells.Add(scheduledSpell);
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
    public CombatSystem CombatSystem { get; private set; }
    public List<CombatObject> CombatObjects { get; private set; }
    public List<SpellCasterObject> SpellCasterObjects { get; private set; }
    public TimeSpan EncounterTimer { get; private set; }
    public int UpdatesPerSecond { get; private set; }
    public bool CombatInProgress { get; private set; }
    private readonly int defaultUpdatesPerSecond = 100;
    private TimeSpan updateInterval;

    public CombatEncounter()
    {
        CombatSystem = new();
        CombatObjects = new();
        SpellCasterObjects = new();
        EncounterTimer = TimeSpan.Zero;
        SetUpdatesPerSecond(defaultUpdatesPerSecond);
    }

    public void ProcessCombat()
    {
        foreach (var spellCasterObject in SpellCasterObjects)
        {
            spellCasterObject.PopulateSpellQueue(EncounterTimer);
            spellCasterObject.ValidateSpellQueue(CombatObjects);
            spellCasterObject.ExecuteSpellQueue(CombatSystem);
        }
        EncounterTimer += updateInterval;
    }

    public void AddPlayer(ISpellSchedule spellSchedule)
    {
        CombatObject combatObject = new()
        {
            IsHostile = true,
            MaxHP = CombatObject.DefaultPlayerHP,
        };
        SpellCasterObject spellCasterObject = new(combatObject, spellSchedule);
        CombatObjects.Add(combatObject);
        SpellCasterObjects.Add(spellCasterObject);
    }

    public void AddEnemy(INpc npc)
    {
        CombatObject combatObject = new()
        {
            IsHostile = true,
            MaxHP = npc.BaseHP(),
        };
        SpellCasterObject spellCasterObject = new(combatObject, npc.SpellSchedule());
        CombatObjects.Add(combatObject);
        SpellCasterObjects.Add(spellCasterObject);
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
    public static readonly float DefaultPlayerHP = 1000;
    public Position Position { get; private set; }
    public bool IsHostile { get; set; }
    public bool IsInCombat { get; private set; }
    public bool HasHP { get; private set; }
    public float MaxHP { get; set; }
    public float CurrentHP { get; private set; }
    public CombatObject Parent { get; private set; }
    public CombatObject()
    {
        Position = Position.Empty;
        IsHostile = false;
        IsInCombat = true;
        HasHP = false;
        MaxHP = 1;
        CurrentHP = MaxHP;
        Parent = Empty;
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
            if (target.IsHostile != attacker.IsHostile)
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
    ISpellSchedule SpellSchedule();
}

public class EmptyNpc : INpc
{
    public string NpcID() => "Npc0000";
    public float BaseHP() => 9999;
    public ISpellSchedule SpellSchedule() => new EmptySpellSchedule();
}

public class Npc0001 : INpc
{
    public string NpcID() => "Npc0001";
    public float BaseHP() => 20;
    public ISpellSchedule SpellSchedule() => new SpellSchedule0001();
}

/// <summary>
/// Algorithm or logic specifying the behavior of a spell
/// </summary>
public interface ISpellEffect
{
    string SpellID();
    List<TimeSpan> FollowUpTimeStamps();
    Targeting SelectTargets(Targeting destination, List<CombatObject> possibleTargets);
    void ExecuteSpellEffect(CombatPacket packet);
}

public class EmptySpellEffect : ISpellEffect
{
    public string SpellID() => "Spell0000";
    public List<TimeSpan> FollowUpTimeStamps() => new();
    public Targeting SelectTargets(Targeting destination, List<CombatObject> possibleTargets)
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
    public Targeting SelectTargets(Targeting destination, List<CombatObject> possibleTargets)
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
    public Targeting SelectTargets(Targeting destination, List<CombatObject> possibleTargets)
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
        SpellEffect = Spell.EmptySpellEffect;
        ExecutionCycle = 0;
    }

    public void LoadSpell(Spell spell)
    {
        Source = spell.Source;
        Destination = spell.Destination;
        Targets = spell.Destination.TargetedCombatObjects;
        SpellEffect = spell.SpellEffect;
        ExecutionCycle = spell.ExecutionCycle;
    }

    public void ResetPacket()
    {
        Source = CombatObject.Empty;
        Destination = Targeting.Empty;
        Targets = Destination.TargetedCombatObjects;
        SpellEffect = Spell.EmptySpellEffect;
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
        packet.SpellEffect.ExecuteSpellEffect(packet);
        OnAttackComplete?.Invoke(packet);
    }
}
