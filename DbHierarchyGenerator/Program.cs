using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using DbHierarchyGenerator.Models;

namespace DbHierarchyGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                bool buildHierarchy = args.Any() && args[0] == "-hierarchy";
                Bootstrapper.Initialize();

                var generator = new MainGenerator();
                generator.Generate(buildHierarchy);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine("Generation complete successful!");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                if (e.InnerException != null)
                    Console.WriteLine(e.InnerException.Message);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Press any key...");
                Console.ReadKey();
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
                Console.WriteLine("...Shutting down...");
            }
        }
    }
}
