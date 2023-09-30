using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.IO;

namespace GlobalNameSpace;

public class Program
{
    public static int AddNumbers(int a, int b)
    {
        return a + b;
    }

    static void Main(string[] args)
    {
        NewGame newGame = new();
        newGame.PlayGame();
        Console.WriteLine("The program will now terminate. Thank you for playing!");
    }
}