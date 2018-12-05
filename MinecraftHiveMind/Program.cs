using System;
using MinecraftNetwork;

namespace MinecraftHiveMind
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using (var minecraft = new MinecraftClient("localhost", 25565))
            {
                try
                {
                    minecraft.Login("testuser");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: {0}", e.Message);
                    Console.ResetColor();
                    Console.WriteLine(e.StackTrace);
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[End of execution.]");
            Console.ReadKey(true);
        }
    }
}
