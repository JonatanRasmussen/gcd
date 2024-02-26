using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

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
/// Positional data for a unit or an area-based attack
/// </summary>
public class Position
{
    public static readonly Position Empty = new(CombatObject.Empty);
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
    public static readonly Targeting Empty = new(CombatObject.Empty);
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

public class CombatResources
{
    public static readonly CombatResources Empty = new(CombatObject.Empty);
    public CombatObject CombatObject { get; }
    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }

    public CombatResources(CombatObject combatObject)
    {
        CombatObject = combatObject;
        MaxHP = 0;
        CurrentHP = 0;
    }

    public static CombatResources SpellResourceCosts()
    {
        return new(CombatObject.Empty)
        {
            MaxHP = 0,
            CurrentHP = 0
        };
    }

    public void CopyFromTemplate(CombatResources resources)
    {
        MaxHP = resources.MaxHP;
        CurrentHP = resources.CurrentHP;
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
/// A unit that is part of combat and targetable by spells
/// </summary>
public class CombatObject
{
    public static readonly CombatObject Empty = new(null);
    public static readonly float DefaultPlayerHP = 1000;
    public Position Position { get; private set; }
    public Targeting Targeting { get; private set; }
    public CombatResources Resources { get; }
    public TimeSpan TimeAlive { get; private set; }
    public bool IsOnFirstServerTick { get; private set; }
    public bool IsEnemy { get; private set; }
    public bool HasHP { get; private set; }
    public float MaxHP { get; set; }
    public float CurrentHP { get; private set; }
    public List<ScheduledSpell> ScheduledSpells { get; set; }
    public List<CombatObject> Children { get; }
    public CombatObject Parent { get; private set; }
    public CombatObject(CombatObject? parent)
    {
        Position = new(this);
        Targeting = new(this);
        Resources = new(this);
        TimeAlive = TimeSpan.Zero;
        IsEnemy = false;
        HasHP = false;
        MaxHP = 1;
        CurrentHP = MaxHP;
        ScheduledSpells = new();
        Children = new();
        Parent = parent ?? this;
    }

    public void VisitDescendants(Action<CombatObject> action)
    {
        action(this); // Perform action on current object
        foreach (CombatObject child in Children)
        {
            child.VisitDescendants(action); // Recursively act on each child
        }
    }

    public List<CombatObject> FindMatches(Func<CombatObject, bool> condition)
    {
        List<CombatObject> matchingCombatObjects = new();
        void addActionIfMatch(CombatObject combatObject)
        {
            if (condition(combatObject))
            {
                matchingCombatObjects.Add(combatObject);
            }
        }
        VisitDescendants(addActionIfMatch); // Resursively search each child
        return matchingCombatObjects;
    }

    public List<ScheduledSpell> FindMatchingSpells(Func<ScheduledSpell, bool> condition)
    {
        List<ScheduledSpell> matchingSpells = new List<ScheduledSpell>();
        void addSpellIfMatch(CombatObject combatObject)
        {
            foreach (ScheduledSpell scheduledSpell in combatObject.ScheduledSpells)
            {
                if (condition(scheduledSpell))
                {
                    matchingSpells.Add(scheduledSpell);
                }
            }
            foreach (CombatObject child in combatObject.Children)
            {
                addSpellIfMatch(child); // Recursively search each child
            }
        }
        addSpellIfMatch(this); // Start search from this CombatObject
        return matchingSpells;
    }

    public void SpawnChild(INpc childTemplate)
    {
        CombatObject child = new(this);
        ScheduledSpell scheduledSpell = childTemplate.OpeningSpell();
        child.ScheduleSpell(scheduledSpell);
        Resources.CopyFromTemplate(childTemplate.Resources());
        ScheduleSpell(childTemplate.OpeningSpell());
    }

    public void ScheduleSpell(ScheduledSpell scheduledSpell)
    {
        scheduledSpell.AssignSource(this);
        ScheduledSpells.Add(scheduledSpell);
    }

    public void IncrementTimeAlive(TimeSpan deltaTime)
    {
        IsOnFirstServerTick = false;
        TimeAlive += deltaTime;
    }


    public void IncrementSpellTimers(TimeSpan deltaTime)
    {
        foreach (ScheduledSpell scheduledSpell in ScheduledSpells)
        {
            scheduledSpell.IncrementTimer(deltaTime);
            scheduledSpell.UpdateCastStatus();
        }
    }

    public void ClearFinishedSpells()
    {
        // Traverse the list backwards to safely remove elements without causing index errors
        for (int i = ScheduledSpells.Count - 1; i >= 0; i--)
        {
            ScheduledSpell scheduledSpell = ScheduledSpells[i];
            if (scheduledSpell.SpellCastIsFinished())
            {
                ScheduledSpells.Remove(scheduledSpell);
            }
        }
    }

    public bool IsRoot()
    {
        return Parent == this;
    }
}

public enum SpellCastStatus
{
    CastNotStarted,
    CastIsReady,
    CastInProgress,
    CastCanceled,
    ChannelInProgress,
    ChannelCanceled,
    CastSuccesful,
    CastFailed,
}

public class ScheduledSpell
{
    public static readonly ScheduledSpell Empty = new(EmptySpell.Empty);
    public CombatObject Source { get; private set; }
    public ISpell Spell { get; }
    public TimeSpan ActivationTimeStamp { get; private set; }
    public TimeSpan SpellTimer { get; private set; }
    public bool TimerIsPaused { get; private set; }
    public SpellCastStatus SpellCastStatus { get; private set; }

    public ScheduledSpell(ISpell spell)
    {
        Source = CombatObject.Empty;
        Spell = spell;
        ActivationTimeStamp = TimeSpan.Zero;
        SpellTimer = TimeSpan.Zero;
        TimerIsPaused = false;
        SpellCastStatus = SpellCastStatus.CastNotStarted;
    }

    public void AssignSource(CombatObject source)
    {
        Source = source;
    }

    public void DelayActivation(TimeSpan delay)
    {
        ActivationTimeStamp += delay;
    }

    public void IncrementTimer(TimeSpan deltaTime)
    {
        if (!TimerIsPaused)
        {
            SpellTimer += deltaTime;
        }
    }

    public void UpdateCastStatus()
    {
        bool timeStampReached = SpellTimer >= ActivationTimeStamp;
        bool spellNotStarted = SpellCastStatus == SpellCastStatus.CastNotStarted;
        if (timeStampReached && spellNotStarted)
        {
            SpellCastStatus = SpellCastStatus.CastIsReady;
        }
    }

    public bool SpellCastIsReady()
    {
        return SpellCastStatus switch
        {
            SpellCastStatus.CastIsReady => true,
            _ => false,
        };
    }

    public bool SpellCastIsFinished()
    {
        return SpellCastStatus switch
        {
            SpellCastStatus.CastSuccesful or
            SpellCastStatus.CastFailed => true,
            _ => false,
        };
    }

    public void SetCastStatusSuccess()
    {
        SpellCastStatus = SpellCastStatus.CastSuccesful;
    }

    public void SetCastStatusFailed()
    {
        SpellCastStatus = SpellCastStatus.CastFailed;
    }
}

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
    public static readonly SpellDetails Empty = new();
    public static readonly float DefaultMaxRange = 99999;
    public CombatResources ResourceCosts { get; }
    public float Range { get; }
    public TimeSpan CastTime { get; }
    public TimeSpan Duration { get; }
    public TimeSpan Cooldown { get; }
    public float GcdModifier { get; }
    public float SpellModifier { get; }
    public List<SpellFlags> Flags { get; }


    public SpellDetails()
    {
        ResourceCosts = CombatResources.SpellResourceCosts();
        Range = DefaultMaxRange;
        CastTime = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        Cooldown = TimeSpan.Zero;
        GcdModifier = 1;
        SpellModifier = 1;
        Flags = new();
    }
}

/// <summary>
/// Contains combat units and controls the flow of attacks
/// </summary>
public class CombatEncounter
{
    public event Action<CombatPacket>? OnAttackStart;
    public event Action<CombatPacket>? OnAttackComplete;
    public static readonly CombatEncounter Empty = new();
    public static readonly int UpdatesPerSecond = 20;
    public TimeSpan EncounterTimer { get; private set; }
    public TimeSpan UpdateInterval { get; }
    public CombatObject Root { get; private set; }
    public bool GameIsPaused { get; private set; }
    public bool EncounterIsPaused { get; private set; }
    private readonly CombatManager combatManager;
    private readonly int defaultPoolSize = 100;

    public CombatEncounter()
    {
        EncounterTimer = TimeSpan.Zero;
        UpdateInterval = CalculateUpdateInterval(UpdatesPerSecond);
        Root = new(null);
        GameIsPaused = false;
        EncounterIsPaused = false;
        combatManager = new CombatManager(defaultPoolSize);
    }

    public void ProcessCombat()
    {
        if (!GameIsPaused)
        {
            ProcessSpells();
            ClearFinishedSpells();
            EncounterTimer += UpdateInterval;
            AnnounceUpdatedTimer();
        }
    }

    public void AnnounceUpdatedTimer()
    {
        Root.VisitDescendants(script => script.IncrementTimeAlive(UpdateInterval));
        Root.VisitDescendants(script => script.IncrementSpellTimers(UpdateInterval));
    }

    public void ProcessSpells()
    {
        List<ScheduledSpell> readySpells = FetchSpellsReadyToBeCast();
        foreach (ScheduledSpell readySpell in readySpells)
        {
            AttemptSpellCast(readySpell);
        }
    }

    public void ClearFinishedSpells()
    {
        Root.VisitDescendants(script => script.ClearFinishedSpells());
    }

    public List<ScheduledSpell> FetchSpellsReadyToBeCast()
    {
        static bool condition(ScheduledSpell scheduledSpell) => scheduledSpell.SpellCastIsReady();
        return Root.FindMatchingSpells(condition);
    }

    public void AttemptSpellCast(ScheduledSpell scheduledSpell)
    {
        bool alwaysTrue = true; //TODO: Check cast requirements
        if (alwaysTrue)
        {
            scheduledSpell.SetCastStatusSuccess();
            CastSpell(scheduledSpell.Source, scheduledSpell.Spell);
        }
        else
        {
            scheduledSpell.SetCastStatusFailed();
        }
    }

    public void CastSpell(CombatObject source, ISpell spell)
    {
        CombatPacket packet = combatManager.RequestPacket();
        packet.Source = source;
        packet.Spell = spell;
        ProcessPacket(packet);
        combatManager.ReleasePacket(packet);
    }

    public void CastSpellByID(CombatObject source, string spellID)
    {
        ISpell spell = SpellRegistry.CreateSpellInstance(spellID);
        CastSpell(source, spell);
    }

    public void ProcessPacket(CombatPacket packet)
    {
        OnAttackStart?.Invoke(packet);
        foreach (ISpellEffect effect in packet.Spell.Effects())
        {
            effect.Execute(packet);
        }
        OnAttackComplete?.Invoke(packet);
    }

    private static TimeSpan CalculateUpdateInterval(int updatesPerSecond)
    {
        return TimeSpan.FromTicks(TimeSpan.TicksPerSecond / updatesPerSecond);
    }
}

/// <summary>
/// Configurations for an ongoing attack
/// </summary>
public class CombatPacket
{

    public CombatObject Source { get; set; }
    public ISpell Spell { get; set; }
    public List<CombatObject> Targets { get; set; }

    public CombatPacket()
    {
        Source = CombatObject.Empty;
        Spell = EmptySpell.Empty;
        Targets = new();
    }

    public void ResetPacket()
    {
        Source = CombatObject.Empty;
        Spell = EmptySpell.Empty;
        Targets.Clear();
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
        // Initialize the pool with pre-allocated combat packets
        // We are reusing combat packets instead of creating new ones to avoid memory fragmentation
        // Inspired by www.youtube.com/watch?v=ltO_rMJJdHE&t=651s
        packetPool = new Queue<CombatPacket>(poolSize);
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

public static class SpellRegistry
{

    /* SpellRegistry.RegisterSpell(new EmptySpell());
    SpellRegistry.RegisterSpell(new Spell1());
    SpellRegistry.RegisterSpell(new Spell2()); */

    private static readonly Dictionary<string, Type> Spells = new();

    public static void RegisterSpell(ISpell spell)
    {
        Spells[spell.SpellID()] = spell.GetType();
    }

    public static ISpell CreateSpellInstance(string spellId)
    {
        if (Spells.TryGetValue(spellId, out Type ?spell))
        {
            return Activator.CreateInstance(spell) as ISpell ?? EmptySpell.Empty;
        }
        return EmptySpell.Empty;
    }
}

public interface INpc
{
    string NpcID();
    CombatResources Resources();
    ScheduledSpell OpeningSpell();
}

public class EmptyNpc : INpc
{
    public string NpcID() => "Npc0000";
    public CombatResources Resources() => CombatResources.Empty;
    public ScheduledSpell OpeningSpell() => ScheduledSpell.Empty;
}

/// <summary>
/// Algorithm or logic specifying the behavior of a spell
/// </summary>
public interface ISpellEffect
{
    void Execute(CombatPacket packet);
}

public class EmptySpellEffect : ISpellEffect
{
    public static readonly ISpellEffect Empty = new EmptySpellEffect();
    public void Execute(CombatPacket packet)
    {
        // Empty
    }
}

public class DealDamage : ISpellEffect
{
    public float Damage { get; set; }
    public DealDamage(float damage)
    {
        Damage = damage;
    }
    public void Execute(CombatPacket packet)
    {
        foreach (CombatObject target in packet.Targets)
        {
            target.Resources.ReduceCurrentHP(Damage);
        }
    }
}

public class SpawnChild : ISpellEffect
{
    public INpc Child { get; set; }
    public SpawnChild(INpc child)
    {
        Child = child;
    }
    public void Execute(CombatPacket packet)
    {
        foreach (CombatObject target in packet.Targets)
        {
            target.SpawnChild(Child);
        }
    }
}

public class ScheduleSpell : ISpellEffect
{
    public ScheduledSpell ScheduledSpell { get; set; }
    public ScheduleSpell(ScheduledSpell scheduledSpell)
    {
        ScheduledSpell = scheduledSpell;
    }
    public void Execute(CombatPacket packet)
    {
        if (packet.Targets.Count == 1) // Reuse existing spell instance
        {
            packet.Targets[0].ScheduleSpell(ScheduledSpell);
        }
        else // Avoid all targets taking ownership of the same spell instance
        {
            foreach (CombatObject target in packet.Targets)
            {
                string spellID = ScheduledSpell.Spell.SpellID();
                ISpell spell = SpellRegistry.CreateSpellInstance(spellID);
                ScheduledSpell scheduledSpell = new(spell);
                scheduledSpell.DelayActivation(ScheduledSpell.ActivationTimeStamp);
                target.ScheduleSpell(scheduledSpell);
            }
        }
    }
}

public interface ISpellTargeting
{
    List<CombatObject> Execute(Targeting destination, CombatObject root);
}

public class EmptySpellTargeting : ISpellTargeting
{
    public static readonly ISpellTargeting Empty = new EmptySpellTargeting();
    public List<CombatObject> Execute(Targeting destination, CombatObject root)
    {
        return CombatObject.Empty.Children;
    }
}

public class TargetAllEnemies : ISpellTargeting
{
    public List<CombatObject> Execute(Targeting destination, CombatObject root)
    {
        CombatObject source = destination.Source;
        Func<CombatObject, bool> condition;
        if (!source.IsEnemy) // source is allied, target enemies
        {
            condition = combatObject =>
                combatObject.IsEnemy &&
                combatObject.HasHP;
        }
        else // source is enemy, target allies
        {
            condition = combatObject =>
                !combatObject.IsEnemy &&
                combatObject.HasHP;
        }
        return root.FindMatches(condition);
    }
}

public interface ISpell
{
    string SpellID();
    ISpellTargeting Targeting();
    SpellDetails Details();
    List<ISpellEffect> Effects();
}

public class EmptySpell : ISpell
{
    public static readonly ISpell Empty = new EmptySpell();
    public string SpellID() => "empty_spell";
    public ISpellTargeting Targeting() => EmptySpellTargeting.Empty;
    public SpellDetails Details() => SpellDetails.Empty;
    public List<ISpellEffect> Effects() => new();
}

public class Spell1 : ISpell
{
    public static readonly ISpell Empty = new EmptySpell();
    public string SpellID() => "spell_1";
    public ISpellTargeting Targeting() => EmptySpellTargeting.Empty;
    public SpellDetails Details() => SpellDetails.Empty;
    public List<ISpellEffect> Effects() => new();
}