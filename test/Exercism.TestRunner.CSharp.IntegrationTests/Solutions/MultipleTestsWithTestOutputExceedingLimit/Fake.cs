using System;
using System.Diagnostics;

public static class Fake
{
    public static int Add(int x, int y)
    {
        Trace.WriteLine(new string('a', 498) + 'b' + 'c' + 'd');
        return x + y;
    }

    public static int Sub(int x, int y) => x - y;

    public static int Mul(int x, int y)
    {
        Debug.WriteLine("Maximum not exceeded");
        return x * y;
    }
}