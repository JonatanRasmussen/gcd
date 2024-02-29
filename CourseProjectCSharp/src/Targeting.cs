using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

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