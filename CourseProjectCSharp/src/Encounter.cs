using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

/// <summary>
/// Contains combat units and controls the flow of attacks
/// </summary>
public class Encounter
{
    public event Action<CombatPacket>? OnAttackStart;
    public event Action<CombatPacket>? OnAttackComplete;
    public static readonly int UpdatesPerSecond = 2;
    public CombatObject Root { get; private set; }
    public CombatObject PlayerTeam { get; private set; }
    public CombatObject EnemyTeam { get; private set; }
    public TimeSpan EncounterTimer { get; private set; }
    public TimeSpan UpdateInterval { get; }
    public bool EncounterIsPaused { get; private set; }
    private readonly SpellRegistry spellRegistry;
    private readonly CombatManager combatManager;
    private readonly int defaultPoolSize = 100;

    public Encounter(CombatObject globalRoot)
    {
        Root = globalRoot.SpawnChild(new EncounterRoot());
        PlayerTeam = Root.SpawnChild(new PlayerTeam());
        EnemyTeam = Root.SpawnChild(new EnemyTeam());
        EncounterTimer = TimeSpan.Zero;
        UpdateInterval = CalculateUpdateInterval(UpdatesPerSecond);
        EncounterIsPaused = false;
        combatManager = new(defaultPoolSize);
        spellRegistry = new();
    }

    public void ProcessCombat()
    {
        Console.WriteLine(EncounterTimer);
        /* foreach (var scheduledSpell in PlayerTeam.Children[0].ScheduledSpells)
        {
            Console.WriteLine(scheduledSpell.Spell.Spell);
        } */
        ProcessSpells();
        ClearFinishedSpells();
        EncounterTimer += UpdateInterval;
        AnnounceUpdatedTimer();
    }

    public bool GameIsOngoing()
    {
        TimeSpan timeLimit = TimeSpan.FromSeconds(10);
        return EncounterTimer <= timeLimit;
    }

    public void AnnounceUpdatedTimer()
    {
        Root.VisitDescendants(script => script.IncrementTimeAlive(UpdateInterval));
        Root.VisitDescendants(script => script.IncrementSpellTimers(UpdateInterval));
    }

    public void ClearFinishedSpells()
    {
        Root.VisitDescendants(script => script.ClearFinishedSpells());
    }

    public void ProcessSpells()
    {
        List<ScheduledSpell> readySpells = FetchSpellsReadyToBeCast();
        foreach (ScheduledSpell readySpell in readySpells)
        {
            AttemptSpellCast(readySpell);
        }
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
        packet.Targets = spell.Targeting().Execute(source.Targeting, Root);
        Console.WriteLine($"Npc '{source.NpcID}' casts '{spell.SpellID()}'");
        ProcessPacket(packet);
        combatManager.ReleasePacket(packet);
    }

    public void CastSpellByID(CombatObject source, string spellID)
    {
        ISpell spell = spellRegistry.CreateSpellInstance(spellID);
        CastSpell(source, spell);
    }

    public List<CombatObject> FindDescendantByNpcID(CombatObject parent, string npcID)
    {
        bool condition(CombatObject combatObject) => combatObject.HasName(npcID);
        return parent.FindMatches(condition);
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
        Source = CombatObject.CreateEmpty();
        Spell = new EmptySpell();
        Targets = new();
    }

    public void ResetPacket()
    {
        Source = CombatObject.CreateEmpty();
        Spell = new EmptySpell();
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