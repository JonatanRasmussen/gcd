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
        CombatEncounter encounter = new();
        encounter.PlayerTeam.SpawnChild(new TestUnit());
        encounter.EnemyTeam.SpawnChild(new TestUnit());
    }
}