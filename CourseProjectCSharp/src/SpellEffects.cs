using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

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

public class CastSpell : ISpellEffect
{
    public ScheduledSpell ScheduledSpell { get; set; }
    public CastSpell(ISpell spell, TimeSpan delay)
    {
        ScheduledSpell = new(spell);
        ScheduledSpell.DelayActivation(delay);
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