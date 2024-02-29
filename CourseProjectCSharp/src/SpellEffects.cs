using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

/// <summary>
/// Algorithm or logic specifying the behavior of a spell
/// </summary>
public interface ISpellEffect
{
    string EffectID();
    void Execute(CombatPacket packet);
}

public class EmptySpellEffect : ISpellEffect
{
    public string EffectID() => "effect_empty";
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
    public string EffectID() => "effect_deal_damage";
    public void Execute(CombatPacket packet)
    {
        Console.WriteLine($"{Damage} damage is being dealt to {packet.Targets.Count} targets");
        foreach (CombatObject target in packet.Targets)
        {
            target.Resources.ReduceCurrentHP(Damage);
        }
    }
}

public class SpawnChild : ISpellEffect
{
    public INpc Npc { get; set; }
    public SpawnChild(INpc npc)
    {
        Npc = npc;
    }
    public string EffectID() => "effect_spawn_child";
    public void Execute(CombatPacket packet)
    {
        Console.WriteLine($"Child {Npc.NpcID()} is being spawned for {packet.Targets.Count} targets");
        foreach (CombatObject target in packet.Targets)
        {
            target.SpawnChild(Npc);
        }
    }
}

public class CastSpell : ISpellEffect
{
    public ISpell Spell { get; set; }
    public TimeSpan Delay { get; set; }
    public CastSpell(ISpell spell, TimeSpan delay)
    {
        Spell = spell;
        Delay = delay;
    }
    public string EffectID() => "effect_cast_spell";
    public void Execute(CombatPacket packet)
    {
        Console.WriteLine($"Spell {Spell.SpellID()} is being scheduled for {packet.Targets.Count} targets");
        if (packet.Targets.Count == 1) // Reuse existing spell instance
        {
            ScheduledSpell scheduledSpell = packet.Targets[0].ScheduleSpell(Spell);
            scheduledSpell.DelayActivation(Delay);

        }
        else // Avoid all targets taking ownership of the same spell instance
        {
            foreach (CombatObject target in packet.Targets)
            {
                Type spellType = Spell.GetType(); // Create a new instance of Spell's class
                ISpell spell = Activator.CreateInstance(spellType) as ISpell ?? new EmptySpell();
                ScheduledSpell scheduledSpell = target.ScheduleSpell(spell);
                scheduledSpell.DelayActivation(Delay);
            }
        }
    }
}