
using System;

internal static class Dbg
{
    internal static void Inf(string format)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(format + "\n");
    }

    internal static void Wrn(string format)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(format + "\n");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    internal static void Err(string format)
    {
        throw new ArgumentException(format);
    }

    internal static void Ex(Exception e)
    {
        throw e;
    }
}
