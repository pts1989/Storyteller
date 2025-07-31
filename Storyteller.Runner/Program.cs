using Storyteller.Demos; // Belangrijk! Hiermee gebruik je de andere library.
using System;
using System.Threading.Tasks;

namespace Storyteller.Runner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var concurrentDemo = new ConcurrentDemo();
            var handsoffDemo = new HandoffDemo();
            var sequentialDemo = new SequentialDemo();
            var groupChatDemo = new GroupChatDemo();
            var magenticDemo = new MagenticDemo();


            bool exit = false;
            while (!exit)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                Console.WriteLine("============================================");
                Console.WriteLine("= Semantic Kernel Orchestration Showcase =");
                Console.WriteLine("============================================");
                Console.WriteLine("Choose a demo to run:\n");
                Console.WriteLine("1. Concurrent Orchestration (The Brainstorm)");
                Console.WriteLine("2. Sequential Orchestration (The Storyline) ");
                Console.WriteLine("3. Group Chat Orchestration (The Interactive Dialogue) ");
                Console.WriteLine("4. Handoffs Orchestration (The Layered Quest) ");
                Console.WriteLine("5. Agentic Orchestration (The Autonomous Writer) ");
                Console.WriteLine("\n0. Exit");
                Console.Write("\nYour choice: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        await concurrentDemo.RunAsync();
                        break;
                    case "2":
                        await sequentialDemo.RunAsync();
                        break;
                    case "3":
                        await groupChatDemo.RunAsync();
                        break;
                    case "4":
                        await handsoffDemo.RunAsync();
                        break;
                    case "5":
                        await magenticDemo.RunAsync();
                        break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Ongeldige keuze. Probeer opnieuw.");
                        break;
                }

                if (!exit)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine("\nDruk op een toets om terug te keren naar het menu...");
                    Console.ReadKey();
                }
            }
        }
    }
}