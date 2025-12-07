using Microsoft.Extensions.Configuration;
using Storyteller.Core;
using Storyteller.Demos; // Belangrijk! Hiermee gebruik je de andere library.
using System;
using System.Threading.Tasks;
using static OllamaSharp.OllamaApiClient;

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
            var MCPDemo = new MCPDEMO();
            var MCPsequential = new MCPDemoSequential();
            IConfigurationRoot config = new ConfigurationBuilder()
           .AddUserSecrets<Program>()
           .Build();
            ConfigurationHelper.Initialize(config);

            bool exit = false;

            bool showOrchistrationCallback = true;
            bool showOrchistrationHistory = true;

            while (!exit)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                Console.WriteLine("============================================");
                Console.WriteLine("= Semantic Kernel Orchestration Showcase Setup=");
                Console.WriteLine("============================================");
                Console.WriteLine("What do you want to see besides results :\n");
                Console.WriteLine("1. Orchestration history");
                Console.WriteLine("2. Orchestration callbacks ");
                Console.WriteLine("3. Orchestration history and Orchestration callbacks ");
                Console.WriteLine("\n0. None");
                Console.Write("\nYour choice: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        showOrchistrationCallback = false;
                        showOrchistrationHistory = true;
                        exit = true;
                        break;
                    case "2":
                        showOrchistrationCallback = true;
                        showOrchistrationHistory = false;
                        exit = true;
                        break;
                    case "3":
                        showOrchistrationCallback = true;
                        showOrchistrationHistory = true;
                        exit = true;
                        break;
                    case "0":
                        showOrchistrationCallback = false;
                        showOrchistrationHistory = false;
                        exit = true;
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }
            }


            exit = false;
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
                Console.WriteLine("6. Group Chat Orchestration MCP Demo ");
                Console.WriteLine("7. Sequential Orchestration MCP Demo ");
                Console.WriteLine("\n0. Exit");
                Console.Write("\nYour choice: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        await concurrentDemo.RunAsync(showOrchistrationCallback, showOrchistrationHistory);
                        break;
                    case "2":
                        await sequentialDemo.RunAsync(showOrchistrationCallback, showOrchistrationHistory);
                        break;
                    case "3":
                        await groupChatDemo.RunAsync(showOrchistrationCallback, showOrchistrationHistory);
                        break;
                    case "4":
                        await handsoffDemo.RunAsync(showOrchistrationCallback, showOrchistrationHistory);
                        break;
                    case "5":
                        await magenticDemo.RunAsync(showOrchistrationCallback, showOrchistrationHistory);
                        break;
                    case "6":
                        await MCPDemo.RunAsync(showOrchistrationCallback, showOrchistrationHistory);
                        break;
                    case "7":
                        await MCPsequential.RunAsync(showOrchistrationCallback, showOrchistrationHistory);
                        break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }

                if (!exit)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine("\nPress any key to return to the menu...");
                    Console.ReadKey();
                }
            }
        }
    }
}