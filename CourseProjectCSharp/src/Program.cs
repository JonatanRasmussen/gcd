using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.IO;

namespace CourseProject;

class Program
{
    // Define a sample data object


    // Function definition
    static int AddNumbers(int a, int b) {
        return a + b;
    }

    PersonClass person = new PersonClass("John", "Doe", 30);
    // This is a single-line comment

    /*
        This is a multi-line comment.
        You can add more lines here.
    */

    static void Main(string[] args)
    {
        // Reading the JSON file into a string
        string fileName = "Type of assessment";
        string jsonText = File.ReadAllText($"infoDictTest/{fileName}.json");

        // Deserializing the JSON string into a Dictionary<string, int>
        Dictionary<string, int> myDict = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonText);

        // Create a list of key-value pairs from the dictionary
        var list = myDict.ToList();

        // Sort the list by value
        list.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

        // Print out the sorted entries
        foreach (var pair in list)
        {
            Console.WriteLine($"{pair.Key} => {pair.Value}");
        }
    }
}

public class PersonClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
    public PersonClass(string firstName, string lastName, int age)
    {
        FirstName = firstName;
        LastName = lastName;
        Age = age;
    }
}