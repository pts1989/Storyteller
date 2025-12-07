using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using OllamaSharp.Models.Chat;
using OpenAI.Assistants;
using OpenAI.Chat;
using OpenAI.Files;
using Storyteller.Core;
using Storyteller.Demos.internalUtilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Storyteller.Demos
{
    public class MCPDEMO
    {
        public MCPDEMO()
        {
        }

        public async Task RunAsync(bool showOrchistrationCallback, bool showOrchistrationHistory)
		{
           
            string allowedDirectory = Path.Combine(Environment.CurrentDirectory, "Output");

            await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
            {
                Name = "filesystem",
                Command = "npx",
                Arguments = [
                    "-y",
                    "@modelcontextprotocol/server-filesystem",
                    allowedDirectory,
                    Environment.CurrentDirectory
                    ],
            }));
            var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            foreach (var tool in tools)
            {
                 Console.WriteLine($"{tool.Name}: {tool.Description}");
            }
            Kernel kernel = await KernelFactory.CreateKernelForModel();
            var test = tools.Select(aiFunction => aiFunction.AsKernelFunction());
            kernel.Plugins.AddFromFunctions("filesystem", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
            OpenAIPromptExecutionSettings executionSettings = new()
            {
                Temperature = 0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
            };



            // Agent 1: The Hero
            var hero = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Aragorn_The_Hero",
                Description = "A brave and noble warrior who takes the lead.",
                Arguments = new KernelArguments(executionSettings),
                Instructions =
                    """
                    You are Aragorn, a noble and courageous warrior.
                    Your personality: direct, protective, and a natural leader.
                    Your speaking style: formal and solemn.
                    Your role: Assess threats, protect your companions, and suggest bold, direct actions. Always take the lead when there is doubt.
                    ALWAYS Write your file in the 'Output' folder
                    ALWAYS Write your thoughts as files to the filesystem tool. Use Aragorn_The_Hero_Thoughts.txt to store your thoughts. Create the file if needed
                    """
            };

            // Agent 2: The Thief
            var thief = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Lila_The_Thief",
                Description = "A sarcastic and practical thief who makes sharp remarks.",
                Arguments = new KernelArguments(executionSettings),
                Instructions =
                    """
                    You are Lila, a quick-witted and sarcastic thief.
                    Your personality: cynical, practical, and deeply mistrustful.
                    Your speaking style: sharp, witty, and informal.
                    Your role: Point out risks, look for traps, and always consider what there is to gain. You question everything and everyone.
                    ALWAYS Write your file in the 'Output' folder
                    ALWAYS Write your thoughts as files to the filesystem tool. Use Lila_The_Thief_Thoughts.txt to store your thoughts. Create the file if needed
                    """
            };

            // Agent 3: The Mage
            var mage = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Zoltan_The_Mage",
                Description = "A wise but cryptic magician who speaks in riddles.",
                Arguments = new KernelArguments(executionSettings),
                Instructions =
                    """
                    You are Zoltan, an ancient, wise, and cryptic mage.
                    Your personality: contemplative, detached, and mysterious.
                    Your speaking style: speaks in metaphors, riddles, and abstract observations.
                    Your role: Sense the unseen magical forces at play. Ponder the deeper meaning of events and offer cryptic but insightful advice.
                    ALWAYS Write your file in the 'Output' folder
                    ALWAYS Write your thoughts as files to the filesystem tool. Use Zoltan_The_Mage_Thoughts.txt to store your thoughts. Create the file if needed
                    """
            };

            // --- Orchestration ---
            ChatHistory history = [];

            ValueTask ResponseCallback(Microsoft.SemanticKernel.ChatMessageContent response)
            {
                history.Add(response);
                return ValueTask.CompletedTask;
            }

            var orchestration = new GroupChatOrchestration(
                // Use a round-robin manager with a maximum of 5 turns for this interaction.
                new RoundRobinGroupChatManager { MaximumInvocationCount = 5 },
                hero, thief, mage)
            {
                ResponseCallback = ResponseCallback
            };

            // The initial scenario to kick off the group chat.
            string userInput = "You stand before a giant, sealed stone door. A strange purple light pulses from the cracks. What do you do?";

            Console.WriteLine($"\nSCENARIO: \"{userInput}\"\n");
            Console.WriteLine("--- Group Chat Results ---");

            // Create a runtime environment and pass it to the orchestration.
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();

            OrchestrationResult<string> results = await orchestration.InvokeAsync(userInput, runtime);

            // Display the final summary from the orchestration.
            string output = await results.GetValueAsync(TimeSpan.FromSeconds(300));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n# FINAL RESULT:\n{output}");
            Console.ResetColor();

            // Display the full conversation history.
            if (showOrchistrationHistory)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("\n\nORCHESTRATION HISTORY\n");

                foreach (Microsoft.SemanticKernel.ChatMessageContent message in history)
                {
                    AIHelpers.WriteAgentChatMessage(message);
                }
            }

            await runtime.RunUntilIdleAsync();
            Console.ResetColor();


        }

        
    }
}