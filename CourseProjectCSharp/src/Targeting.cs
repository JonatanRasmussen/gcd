using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public interface ISpellTargeting
{
    string TargetingID();
    List<CombatObject> Execute(Targeting destination, CombatObject root);
}

public class EmptySpellTargeting : ISpellTargeting
{
    public string TargetingID() => "targeting_empty";
    public List<CombatObject> Execute(Targeting destination, CombatObject root)
    {
        return CombatObject.CreateEmpty().Children;
    }
}

public class TargetSelf : ISpellTargeting
{
    public string TargetingID() => "targeting_self";
    public List<CombatObject> Execute(Targeting destination, CombatObject root)
    {
        return new() {destination.Source};
    }
}

public class TargetAllEnemies : ISpellTargeting
{
    public string TargetingID() => "targeting_all_enemies";
    public List<CombatObject> Execute(Targeting destination, CombatObject root)
    {
        return root.FindMatches(destination.Source.MemberOfEnemyTeam());
    }
}