using System;
using System.Collections.Generic;
namespace GlobalNameSpace;

public class Program
{
    public static int AddNumbers(int a, int b)
    {
        return a + b;
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!"); // I am happy now :)
        CombatObject globalRoot = CombatObject.CreateEmpty();
        Encounter encounter = new(globalRoot);
        encounter.PlayerTeam.SpawnChild(new TestUnit());
        CombatObject child = encounter.EnemyTeam.SpawnChild(new TestUnit());
        child.IsOnPlayersTeam = true;
        while (encounter.GameIsOngoing())
        {
            encounter.ProcessCombat();
        }
    }
}
