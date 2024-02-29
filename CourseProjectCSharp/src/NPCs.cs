using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public interface INpc
{
    string NpcID();
    void Configure(CombatObject combatObject);
}

public class EmptyNpc : INpc
{
    public string NpcID() => "npc_empty";
    public void Configure(CombatObject combatObject)
    {
        combatObject.NpcID = NpcID();
    }
}

public class GlobalRoot : INpc
{
    public string NpcID() => "npc_global_root";
    public void Configure(CombatObject combatObject)
    {
        combatObject.NpcID = NpcID();
    }
}

public class EncounterRoot : INpc
{
    public string NpcID() => "npc_encounter_root";
    public void Configure(CombatObject combatObject)
    {
        combatObject.NpcID = NpcID();
    }
}

public class PlayerTeam : INpc
{
    public string NpcID() => "npc_player_team";
    public void Configure(CombatObject combatObject)
    {
        combatObject.NpcID = NpcID();
        combatObject.IsOnPlayersTeam = true;
    }
}

public class EnemyTeam : INpc
{
    public string NpcID() => "npc_enemy_team";
    public void Configure(CombatObject combatObject)
    {
        combatObject.NpcID = NpcID();
    }
}

public class TestUnit : INpc
{
    public string NpcID() => "npc_test_unit";
    public void Configure(CombatObject combatObject)
    {
        combatObject.NpcID = NpcID();
        combatObject.IsTargetable = true;
        combatObject.Resources.SetMaxHP(100);
        combatObject.ScheduleSpell(new SpellScheduleTest());
    }
}