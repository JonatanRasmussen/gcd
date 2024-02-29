using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public class SpellScheduleTest : ISpell
{
    public string SpellID() => "spell_schedule_test";
    public ISpellTargeting Targeting() => EmptySpellTargeting.Empty;
    public SpellDetails Details() => SpellDetails.Empty;
    public List<ISpellEffect> Effects() => new()
    {
        new CastSpell(new AoEDamageTest(), TimeSpan.FromSeconds(2)),
        new CastSpell(new AoEDamageTest(), TimeSpan.FromSeconds(4)),
        new CastSpell(new AoEDamageTest(), TimeSpan.FromSeconds(9)),
    };
}

public class AoEDamageTest : ISpell
{
    public string SpellID() => "spell_2";
    public ISpellTargeting Targeting() => new TargetAllEnemies();
    public SpellDetails Details() => SpellDetails.Empty;
    public List<ISpellEffect> Effects() => new()
    {
        new DealDamage(3),
        new DealDamage(7),
    };
}