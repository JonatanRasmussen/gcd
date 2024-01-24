using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

using System;
using System.Collections.Generic;

public enum AttackType
{
    Melee,
    Ranged,
    Magic,
    AreaOfEffect
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

public class Position
{
    public float X { get; set; }
    public float Y { get; set; }
    public bool HasHitbox { get; set; }

    public Position(float x, float y)
    {
        X = x;
        Y = y;
        HasHitbox = true;
    }
}

public class SpellConfig
{
    public CombatObject? Attacker { get; set; } // Source
    public List<CombatObject> Targets { get; set; }
    public Position? TargetLocation { get; set; } // For area attacks
    public ISpellStrategy? Spell { get; set; }

    public SpellConfig()
    {
        Attacker = null;
        Targets = new();
        TargetLocation = null;
        Spell = null;
    }
}

public class SpellSchedule
{
    public Dictionary<TimeSpan, SpellConfig> Script { get; set; }

    public SpellSchedule()
    {
        Script = new();
    }

    public List<SpellConfig> TriggerScriptedSpells(TimeSpan encounterTimer)
    {
        // TODO: Incomplete code
        List<SpellConfig> triggeredSpells = new()
        {
            Script[encounterTimer]
        };
        return triggeredSpells;
    }
}

public class SpellCasterObject
{
    public CombatEncounter CombatEncounter { get; set; }
    public CombatSystem CombatSystem { get; set; }
    public CombatObject ActorObject { get; set; }
    public List<SpellConfig> SpellConfigQueue { get; set; }
    public SpellSchedule? SpellSchedule { get; set; }
    public SpellCasterObject(CombatEncounter combatEncounter, CombatSystem combatSystem, CombatObject actorObject)
    {
        CombatEncounter = combatEncounter;
        CombatSystem = combatSystem;
        ActorObject = actorObject;
        SpellConfigQueue = new();
        SpellSchedule = null;
    }

    public void RefreshSpellQueue(TimeSpan encounterTimer)
    {
        if (SpellSchedule == null)
        {
            ListenForPlayerInput();
        }
        else
        {
            List<SpellConfig> newSpellConfigs = SpellSchedule.TriggerScriptedSpells(encounterTimer);
            SpellConfigQueue.AddRange(newSpellConfigs);
        }
    }

    public void ExecuteSpellQueue()
    {
        foreach (var spellConfig in SpellConfigQueue)
        {
            if (SpellCastIsValid(spellConfig))
            {
                CombatSystem.StartAttack(spellConfig);
            }
        }
    }

    private static bool SpellCastIsValid(SpellConfig spellConfig)
    {
        return true;
    }

    private static void ListenForPlayerInput()
    {
        //Empty;
    }
}

public class CombatEncounter
{
    public CombatSystem CombatSystem { get; set; }
    public List<CombatObject> CombatObjects { get; set; }
    public List<SpellCasterObject> CombatActors { get; set; }
    public TimeSpan EncounterTimer { get; set; }
    public TimeSpan UpdateInterval { get; set; } // Renamed from TickRate

    public CombatEncounter(int updatesPerSecond)
    {
        CombatSystem = new();
        CombatObjects = new();
        CombatActors = new();
        EncounterTimer = TimeSpan.Zero;
        UpdateInterval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / updatesPerSecond);
    }

    public void ProcessCombat()
    {
        foreach (var combatActors in CombatActors)
        {
            combatActors.RefreshSpellQueue(EncounterTimer);
            combatActors.ExecuteSpellQueue();
        }
        EncounterTimer += UpdateInterval;
    }
}

public class CombatObject
{
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


public interface ISpellStrategy
{
    string SpellID();
    void Execute(CombatPacket packet);
}

public class Spell0000 : ISpellStrategy
{
    public string SpellID() => "Spell0000";

    public void Execute(CombatPacket packet)
    {
        // Empty
    }
}

public class Spell0001 : ISpellStrategy
{
    public string SpellID() => "Spell000";
    private readonly float damage = 10;
    public void Execute(CombatPacket packet)
    {
        SpellTemplate.DealDamage(packet.Targets, damage);
    }
}

public class CombatPacket
{
    public CombatObject? Attacker { get; set; } // Source
    public List<CombatObject> Targets { get; set; }
    public Position? TargetLocation { get; set; } // For area attacks
    public ISpellStrategy? Spell { get; set; }
    public bool AttackIsSuccesful { get; set; } // Determines if the attack was successful
    public AttackType AttackType { get; set; }
    // Additional fields as needed

    public CombatPacket()
    {
        Targets = new();
        ResetPacket();
    }

    public void LoadSpellCastParameters(SpellConfig spellCastParameters)
    {
        Attacker = spellCastParameters.Attacker;
        Targets = spellCastParameters.Targets;
        TargetLocation = spellCastParameters.TargetLocation;
        Spell = spellCastParameters.Spell;
    }

    public void ResetPacket()
    {
        Attacker = null;
        Targets.Clear();
        TargetLocation = null;
        Spell = null;
    }
}

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

public class CombatSystem
{
    public event Action<CombatPacket>? OnAttackStart;
    public event Action<CombatPacket>? OnAttackAnimationStart;
    public event Action<CombatPacket>? OnProjectileLaunch;
    public event Action<CombatPacket>? OnAttackProgress;
    public event Action<CombatPacket>? OnAttackHit;
    public event Action<CombatPacket>? OnAttackMiss;
    public event Action<CombatPacket>? OnAttackEnd;
    public event Action<CombatPacket>? OnAttackComplete;

    private readonly CombatManager combatManager;

    public CombatSystem()
    {
        combatManager = new CombatManager(10); // Example pool size
    }

    public void StartAttack(SpellConfig spellCastParameters)
    {
        var packet = combatManager.RequestPacket();
        packet.Attacker = spellCastParameters.Attacker;
        packet.Targets = spellCastParameters.Targets;
        packet.TargetLocation = spellCastParameters.TargetLocation;
        packet.Spell = spellCastParameters.Spell;
        packet.AttackType = AttackType.Melee;

        OnAttackStart?.Invoke(packet);

        // Fill in other details and handle the attack logic

        HandleAttack(packet);

        OnAttackComplete?.Invoke(packet);
        combatManager.ReleasePacket(packet); // Release the packet when done
    }

    private void HandleAttack(CombatPacket packet)
    {
        packet.Spell?.Execute(packet);
        OnAttackAnimationStart?.Invoke(packet);
        // Handle animation logic

        if (packet.AttackType == AttackType.Ranged)
        {
            OnProjectileLaunch?.Invoke(packet);
            // Handle projectile launch logic
        }

        OnAttackProgress?.Invoke(packet);
        // Handle ongoing attack logic

        var hitSuccessful = DetermineHit(packet);
        if (hitSuccessful)
        {
            OnAttackHit?.Invoke(packet);
            // Handle hit logic
        }
        else
        {
            OnAttackMiss?.Invoke(packet);
            // Handle miss logic
        }

        OnAttackEnd?.Invoke(packet);
        // Handle attack end logic
    }

    private bool DetermineHit(CombatPacket packet)
    {
        // Implement hit determination logic
        return true; // Placeholder
    }
}
