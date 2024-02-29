using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public class SpellRegistry
{
    public Dictionary<string, Type> Spells { get; }

    // Ensure default spells are registered only on the first access
    public SpellRegistry()
    {
        Spells = new();
        InitializeDefaultSpells();
    }

    public void InitializeDefaultSpells()
    {
        RegisterSpell(new EmptySpell());
        RegisterSpell(new SpellScheduleTest());
        RegisterSpell(new AoEDamageTest());
    }
    public void RegisterSpell(ISpell spell)
    {
        string spellId = spell.SpellID();
        Type spellType = spell.GetType();
        if (!Spells.TryGetValue(spellId, out Type? value))
        {
            Spells.Add(spellId, spellType); // Add new spell
        }
        else if (value == spellType)
        {
            Console.WriteLine($"Warning: Duplicate spell registration of {spellId}");
        }
        else
        {
            string? previousValue = value.FullName;
            string? newValue = spellType.FullName;
            Console.WriteLine($"Warning: Overwriting {spellId} {previousValue} with {spellId} {newValue}");
            Spells[spellId] = spellType; // Update existing spell
        }
    }

    public ISpell CreateSpellInstance(string spellId)
    {
        if (Spells.TryGetValue(spellId, out Type ?spell))
        {
            return Activator.CreateInstance(spell) as ISpell ?? new EmptySpell();
        }
        return new EmptySpell();
    }
}