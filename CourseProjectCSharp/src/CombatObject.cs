using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

/// <summary>
/// A unit that is part of combat and targetable by spells
/// </summary>
public class CombatObject
{
    public static readonly string DefaultNpcID = "default_npc_id";
    public string NpcID { get; set; }
    public Position Position { get; private set; }
    public Targeting Targeting { get; private set; }
    public Resources Resources { get; private set; }
    public TimeSpan TimeAlive { get; private set; }
    public bool IsOnPlayersTeam { get; set; }
    public bool IsTargetable { get; set; }
    public List<ScheduledSpell> ScheduledSpells { get; set; }
    public List<CombatObject> Children { get; }
    public CombatObject Parent { get; private set; }
    public CombatObject(CombatObject? parent)
    {
        NpcID = DefaultNpcID;
        Position = new(this);
        Targeting = new(this);
        Resources = new(this);
        TimeAlive = TimeSpan.Zero;
        IsTargetable = false;
        ScheduledSpells = new();
        Children = new();
        if (parent == null)
        {
            Parent = this;
            IsOnPlayersTeam = false;
        }
        else
        {
            Parent = parent;
            IsOnPlayersTeam = parent.IsOnPlayersTeam;
        }
    }

    public static CombatObject CreateEmpty()
    {
        return new CombatObject(null);
    }

    public void VisitDescendants(Action<CombatObject> action)
    {
        action(this); // Perform action on current object
        foreach (CombatObject child in Children)
        {
            child.VisitDescendants(action); // Recursively act on each child
        }
    }

    public List<CombatObject> FindMatches(Func<CombatObject, bool> condition)
    {
        List<CombatObject> matchingCombatObjects = new();
        void addActionIfMatch(CombatObject combatObject)
        {
            if (condition(combatObject))
            {
                matchingCombatObjects.Add(combatObject);
            }
        }
        VisitDescendants(addActionIfMatch); // Resursively search each child
        return matchingCombatObjects;
    }

    public List<ScheduledSpell> FindMatchingSpells(Func<ScheduledSpell, bool> condition)
    {
        List<ScheduledSpell> matchingSpells = new List<ScheduledSpell>();
        void addSpellIfMatch(CombatObject combatObject)
        {
            foreach (ScheduledSpell scheduledSpell in combatObject.ScheduledSpells)
            {
                if (condition(scheduledSpell))
                {
                    matchingSpells.Add(scheduledSpell);
                }
            }
            foreach (CombatObject child in combatObject.Children)
            {
                addSpellIfMatch(child); // Recursively search each child
            }
        }
        addSpellIfMatch(this); // Start search from this CombatObject
        return matchingSpells;
    }

    public CombatObject SpawnChild(INpc npc)
    {
        CombatObject child = new(this);
        Children.Add(child);
        npc.Configure(this);
        return child;
    }

    public ScheduledSpell ScheduleSpell(ISpell spell)
    {
        ScheduledSpell scheduledSpell = new(this, spell);
        ScheduledSpells.Add(scheduledSpell);
        return scheduledSpell;
    }

    public void IncrementTimeAlive(TimeSpan deltaTime)
    {
        TimeAlive += deltaTime;
    }

    public void IncrementSpellTimers(TimeSpan deltaTime)
    {
        foreach (ScheduledSpell scheduledSpell in ScheduledSpells)
        {
            scheduledSpell.IncrementTimer(deltaTime);
            scheduledSpell.UpdateCastStatus();
        }
    }

    public void ClearFinishedSpells()
    {
        // Traverse the list backwards to safely remove elements without causing index errors
        for (int i = ScheduledSpells.Count - 1; i >= 0; i--)
        {
            ScheduledSpell scheduledSpell = ScheduledSpells[i];
            if (scheduledSpell.SpellCastIsFinished())
            {
                ScheduledSpells.Remove(scheduledSpell);
            }
        }
    }

    public bool HasName(string name)
    {
        return NpcID == name;
    }

    public bool IsRoot()
    {
        return Parent == this;
    }

    public Func<CombatObject, bool> MemberOfEnemyTeam()
    {
        return combatObject => combatObject.IsOnPlayersTeam != IsOnPlayersTeam && combatObject.IsTargetable;
    }

    public Func<CombatObject, bool> MemberOfAlliedTeam()
    {
        return combatObject => combatObject.IsOnPlayersTeam == IsOnPlayersTeam && combatObject.IsTargetable;
    }
}
