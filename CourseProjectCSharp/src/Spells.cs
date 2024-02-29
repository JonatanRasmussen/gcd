using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public interface ISpell
{
    string SpellID();
    ISpellTargeting Targeting();
    SpellDetails Details();
    List<ISpellEffect> Effects();
}

public class EmptySpell : ISpell
{
    public string SpellID() => "spell_empty";
    public ISpellTargeting Targeting() => new EmptySpellTargeting();
    public SpellDetails Details() => new();
    public List<ISpellEffect> Effects() => new();
}

public class SpellScheduleTest : ISpell
{
    public string SpellID() => "spell_schedule_test01";
    public ISpellTargeting Targeting() => new TargetSelf();
    public SpellDetails Details() => new();
    public List<ISpellEffect> Effects() => new()
    {
        new CastSpell(new AoEDamageTest(), TimeSpan.FromSeconds(2)),
        new CastSpell(new AoEDamageTest(), TimeSpan.FromSeconds(4)),
        new CastSpell(new AoEDamageTest(), TimeSpan.FromSeconds(9)),
    };
}

public class AoEDamageTest : ISpell
{
    public string SpellID() => "spell_aoe_damage_test01";
    public ISpellTargeting Targeting() => new TargetAllEnemies();
    public SpellDetails Details() => new();
    public List<ISpellEffect> Effects() => new()
    {
        new DealDamage(3),
        new DealDamage(7),
    };
}