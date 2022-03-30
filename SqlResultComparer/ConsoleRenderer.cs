using System;
using System.Text.RegularExpressions;

namespace SqlResultComparer
{
    internal static class ConsoleRenderer
    {
        public static void Render(string[] lines, string[] parameternames, string[] parametervalues)
        {
            foreach (var line in lines)
            {
                if (Regex.Match(line, "<>").Success)
                {
                    foreach (var fi in line.Split('|'))
                    {
                        if (Regex.Match(fi, "<>").Success)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.BackgroundColor = ConsoleColor.Red;
                        }
                        else if (Regex.Match(fi, "=").Success) Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(fi);
                        Console.ResetColor();
                        Console.Write('|');
                    }
                    Console.WriteLine();
                }
                else
                {
                    if (Regex.Match(line, "[<>]").Success) Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (Regex.Match(line, "=").Success) Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(line);
                }
                Console.ResetColor();
            }
        }
    }
}