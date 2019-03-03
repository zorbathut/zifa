
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
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(format + "\n");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    internal static void Ex(Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(e.ToString());
        Console.ForegroundColor = ConsoleColor.Gray;
    }
}
