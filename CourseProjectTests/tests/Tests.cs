namespace TestingNameSpace;

using GlobalNameSpace;
using System;
using Xunit;

public class InfoParsingTests
{
    [Fact]
    public void InfoParsingTest1()
    {
        Assert.Equal(1, 1);
    }
    [Fact]
    public void InfoParsingTest2()
    {
        Assert.Equal(2, 2);
    }
    [Fact]
    public void AddNumbersTest()
    {
        int sum = Program.AddNumbers(2,3);
        Assert.Equal(5, sum);
    }
}