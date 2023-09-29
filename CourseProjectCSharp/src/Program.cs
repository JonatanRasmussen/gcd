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

    static void Main(string[] args)
    {
        Console.WriteLine($"{AddNumbers(1, 2)}");
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