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

public class Targeting
{
    public List<CombatObject> Targets { get; set; }
    public Position? TargetLocation { get; set; } // For area attacks
    public ITargetStrategy? TargetStrategy { get; set; } // For area attacks
    public string SelectionInput { get; set; } // For area attacks

    public Targeting()
    {
        Targets = new();
        TargetLocation = null;
        TargetStrategy = null;
        SelectionInput = string.Empty;
    }

    public List<CombatObject> GetTargets()
    {
        return Targets;
    }

    public void SetTargets(List<CombatObject> combatObjects)
    {
        Targets = combatObjects;
    }

    public void FindTargets(List<CombatObject> combatObjects)
    {
        Targets = TargetStrategy?.Execute(SelectionInput, combatObjects) ?? new();
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

public interface ITargetStrategy
{
    string SpellID();
    List<CombatObject> Execute(string input, List<CombatObject> combatObjects);
}

public class TargetSelection0000 : ITargetStrategy
{
    public string SpellID() => "T0000";

    public List<CombatObject> Execute(string input, List<CombatObject> combatObjects)
    {
        return combatObjects;
    }
}

public class Spell
{
    public CombatObject? Attacker { get; set; } // Source
    public Targeting Targeting { get; set; }
    public ISpellEffect? SpellEffect { get; set; }
    public TimeSpan ActivationTimeStamp { get; set; }
    public TimeSpan? InternalTimer { get; set; }
    public TimeSpan TimeOffset { get; set; }
    public bool IsActive { get; set; }

    public Spell()
    {
        Attacker = null;
        Targeting = new();
        SpellEffect = null;
        ActivationTimeStamp = TimeSpan.Zero;  //
        InternalTimer = null;  //Internal timer starts at 0, regardless of external timer
        TimeOffset = TimeSpan.Zero;  //TimeOffset == ActivationTimeStamp unless cast gets delayed
        IsActive = false;
    }

    public void UpdateInternalTimers(TimeSpan externalTimer)
    {
        if (InternalTimer == null)  //Run this the first time the method is called
        {
            TimeOffset = externalTimer;
        }
        InternalTimer = externalTimer - TimeOffset; //Keep internal timer at a fixed offset
    }

    public List<CombatObject> GetTargets()
    {
        return Targeting.GetTargets();
    }

    public void SetTargets(List<CombatObject> combatObjects)
    {
        Targeting.SetTargets(combatObjects);
    }

    public void FindTargets(List<CombatObject> combatObjects)
    {
        Targeting.FindTargets(combatObjects);
    }
}

public class SpellSchedule
{
    public List<Spell> SpellList { get; set; }
    public TimeSpan? InternalTimer { get; set; }
    public TimeSpan TimeOffset { get; set; }
    private List<Spell> SpellsReadyForCasting { get; set; }

    public SpellSchedule()
    {
        SpellList = new();
        InternalTimer = null;  //Internal timer starts at 0, regardless of external timer
        TimeOffset = TimeSpan.Zero;  //Fixed offset from external timer. Is positive
        SpellsReadyForCasting = new();
    }

    public List<Spell> FetchReadySpells(TimeSpan externalTimer)
    {
        UpdateSpellActivityStatus(externalTimer);
        UpdateSpellTimers(externalTimer);
        SpellsReadyForCasting.Clear();
        foreach (var spell in SpellList)
        {
            if (spell.IsActive)
            {
                SpellsReadyForCasting.Add(spell);
            }
        };
        return SpellsReadyForCasting;
    }

    private void UpdateSpellActivityStatus(TimeSpan externalTimer)
    {
        TimeSpan timeStart = InternalTimer ?? TimeSpan.Zero; //before updating the internal timer
        UpdateInternalTimer(externalTimer);
        TimeSpan timeEnd = InternalTimer ?? TimeSpan.Zero; //after updating the internal timer
        foreach (var spell in SpellList)
        {
            if (spell.ActivationTimeStamp >= timeStart && spell.ActivationTimeStamp < timeEnd)
            {
                spell.IsActive = true;
            }
        };
    }

    private void UpdateSpellTimers(TimeSpan externalTimer)
    {
        foreach (var spell in SpellList)
        {
            if (spell.IsActive)
            {
                spell.UpdateInternalTimers(externalTimer);
            }
        };
    }

    private void UpdateInternalTimer(TimeSpan externalTimer)
    {
        if (InternalTimer == null)  //Run this the first time the method is called
        {
            TimeOffset = externalTimer;
        }
        InternalTimer = externalTimer - TimeOffset; //Keep internal timer at a fixed offset
    }
}

public class SpellCasterObject
{
    public CombatSystem CombatSystem { get; set; }
    public CombatObject Caster { get; set; }
    public List<Spell> SpellQueue { get; set; }
    public SpellSchedule? SpellSchedule { get; set; }
    public SpellCasterObject(CombatSystem combatSystem, CombatObject caster)
    {
        CombatSystem = combatSystem;
        Caster = caster;
        SpellQueue = new();
        SpellSchedule = null;
    }

    public void ExecuteSpellQueue(List<CombatObject> combatObjects, TimeSpan encounterTimer)
    {
        RefreshSpellQueue(encounterTimer);
        foreach (var spell in SpellQueue)
        {
            spell.FindTargets(combatObjects);
            if (SpellCastIsValid(spell))
            {
                CombatSystem.StartAttack(spell);
            }
        }
        ClearFinishedSpellsFromQueue();
    }

    private void RefreshSpellQueue(TimeSpan encounterTimer)
    {
        if (SpellSchedule == null)
        {
            ListenForPlayerInput();
        }
        else
        {
            SpellQueue.AddRange(SpellSchedule.FetchReadySpells(encounterTimer));
        }
    }

    private void ClearFinishedSpellsFromQueue()
    {
        // Iterate in reverse order to safely remove elements during iteration over the list
        for (var i = SpellQueue.Count - 1; i >= 0; i--)
        {
            var spell = SpellQueue[i];
            if (!spell.IsActive)
            {
                SpellQueue.RemoveAt(i);
            }
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

public class CombatEncounter
{
    public CombatSystem CombatSystem { get; set; }
    public List<CombatObject> CombatObjects { get; set; }
    public List<SpellCasterObject> SpellCasterObjects { get; set; }
    public TimeSpan EncounterTimer { get; set; }
    public TimeSpan UpdateInterval { get; set; }

    public CombatEncounter(int updatesPerSecond)
    {
        CombatSystem = new();
        CombatObjects = new();
        SpellCasterObjects = new();
        EncounterTimer = TimeSpan.Zero;
        UpdateInterval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / updatesPerSecond);
    }

    public void ProcessCombat()
    {
        foreach (var spellCasterObject in SpellCasterObjects)
        {
            spellCasterObject.ExecuteSpellQueue(CombatObjects, EncounterTimer);
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


public interface ISpellEffect
{
    string SpellID();
    void Execute(CombatPacket packet);
}

public class Spell0000 : ISpellEffect
{
    public string SpellID() => "Spell0000";

    public void Execute(CombatPacket packet)
    {
        // Empty
    }
}

public class Spell0001 : ISpellEffect
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
    public Targeting? Targeting { get; set; }
    public List<CombatObject> Targets { get; set; }
    public ISpellEffect? SpellEffect { get; set; }
    public bool AttackIsSuccesful { get; set; } // Determines if the attack was successful
    public AttackType AttackType { get; set; }
    // Additional fields as needed

    public CombatPacket()
    {
        Attacker = null;
        Targeting = null;
        Targets = new();
        SpellEffect = null;
    }

    public void LoadSpell(Spell spell)
    {
        Attacker = spell.Attacker;
        Targeting = spell.Targeting;
        Targets = spell.GetTargets();
        SpellEffect = spell.SpellEffect;
    }

    public void ResetPacket()
    {
        Attacker = null;
        Targeting = null;
        SpellEffect = null;
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

    public void StartAttack(Spell spell)
    {
        var packet = combatManager.RequestPacket();
        packet.Attacker = spell.Attacker;
        packet.Targeting = spell.Targeting;
        packet.SpellEffect = spell.SpellEffect;
        packet.AttackType = AttackType.Melee;

        OnAttackStart?.Invoke(packet);

        // Fill in other details and handle the attack logic

        HandleAttack(packet);

        OnAttackComplete?.Invoke(packet);
        combatManager.ReleasePacket(packet); // Release the packet when done
    }

    private void HandleAttack(CombatPacket packet)
    {
        packet.SpellEffect?.Execute(packet);
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
        return true; // PlaceholderTest
    }
}
