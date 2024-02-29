using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public class EncounterRoot : INpc
{
    public string NpcID() => "encounter_root";
    public CombatResources Resources() => CombatResources.Empty;
    public ScheduledSpell OpeningSpell() => ScheduledSpell.Empty;
}

public class PlayerTeam : INpc
{
    public string NpcID() => "player_team";
    public CombatResources Resources() => CombatResources.Empty;
    public ScheduledSpell OpeningSpell() => ScheduledSpell.Empty;
}

public class EnemyTeam : INpc
{
    public string NpcID() => "enemy_team";
    public CombatResources Resources() => CombatResources.Empty;
    public ScheduledSpell OpeningSpell() => ScheduledSpell.Empty;
}

public class TestUnit : INpc
{
    public string NpcID() => "enemy_team";
    public CombatResources Resources() => CombatResources.Empty;
    public ScheduledSpell OpeningSpell() => new(new SpellScheduleTest());
}