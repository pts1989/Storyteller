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
    public class MCPDemoSequential
    {



        public MCPDemoSequential()
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
           //     Console.WriteLine($"{tool.Name}: {tool.Description}");
            }
            Kernel kernel = await KernelFactory.CreateKernelForModel();
            var test = tools.Select(aiFunction => aiFunction.AsKernelFunction());
            kernel.Plugins.AddFromFunctions("filesystem", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
            OpenAIPromptExecutionSettings executionSettings = new()
            {
                Temperature = 0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
            };



            var worldBuilder = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "World_Builder",
                Description = "Builds a rich, atmospheric scene based on the initial prompt.",
                Arguments = new KernelArguments(executionSettings),
                Instructions =
                     """
                    You are a master world-builder. Based on the user's input, describe a rich, atmospheric environment using vivid sensory details (sights, sounds, smells).
                    Do NOT include any characters or plot events. Your focus is solely on setting the scene.
                    ALWAYS Write your file in the 'Output' folder
                    ALWAYS Write details and thoughts into a file use the filesystem tool. Use World_Builder.txt to store your thoughts. Create the file if needed
                    """
            };

            var characterCreator = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Character_Creator",
                Description = "Creates a memorable character that fits within the previously established environment.",
                Arguments = new KernelArguments(executionSettings),
                Instructions =
                    """
                    You are a master character designer. Based on the environment described in the preceding text, introduce a single, memorable character.
                    Describe their appearance, their immediate goal or motivation, and a simple action they are performing within that scene. Ensure the character feels like a natural part of the world.
                    ALWAYS Write your file in the 'Output' folder
                    ALWAYS Write details and thoughts into a file use the filesystem tool. Use Character_Creator.txt to store your thoughts. Create the file if needed
                    """
            };

            var plotGenerator = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Plot_Generator",
                Description = "Introduces a conflict or inciting incident into the scene.",
                Arguments = new KernelArguments(executionSettings),
                Instructions =
                    """
                    You are a master of suspense. The scene and character are set. Now, introduce a single, unexpected event or an immediate conflict that complicates the character's situation.
                    Your goal is to create tension and leave a question in the reader's mind about what will happen next. Build directly upon the established setting and character actions.
                    ALWAYS Write your file in the 'Output' folder
                    ALWAYS Write details and thoughts into a file use the filesystem tool. Use Plot_Generator.txt to store your thoughts. Create the file if needed
                    """
            };


            ChatHistory history = [];
            ValueTask responseCallback(Microsoft.SemanticKernel.ChatMessageContent response)
            {
                history.Add(response);
                return ValueTask.CompletedTask;
            }
            // --- Orchestratie ---
            OrchestrationMonitor monitor = new(showOrchistrationCallback);

            var orchestration = new SequentialOrchestration(worldBuilder, characterCreator, plotGenerator)
            {
                ResponseCallback = responseCallback,
                //StreamingResponseCallback = monitor.StreamingResultCallback,
            };

            // The initial scenario to kick off the group chat.
            string userInput = "An airship of gleaming brass and wood lands softly in a hidden jungle valley.";

            Console.WriteLine($"\nSCENARIO: \"{userInput}\"\n");
            Console.WriteLine("--- Group Chat Results ---");

            // Create a runtime environment and pass it to the orchestration.
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();

            OrchestrationResult<string> results = await orchestration.InvokeAsync(userInput, runtime);

            // Display the final summary from the orchestration.
            string output = await results.GetValueAsync(TimeSpan.FromSeconds(600));
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