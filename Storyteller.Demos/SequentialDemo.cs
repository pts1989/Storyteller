using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
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
    public class SequentialDemo
    {
        public async Task RunAsync(bool showOrchistrationCallback, bool showOrchistrationHistory)
		{
            Kernel kernel = await KernelFactory.CreateKernelForModel();
            var worldBuilder = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "World_Builder",
                Description = "Builds a rich, atmospheric scene based on the initial prompt.",
                Instructions =
                    """
                    You are a master world-builder. Based on the user's input, describe a rich, atmospheric environment using vivid sensory details (sights, sounds, smells).
                    Do NOT include any characters or plot events. Your focus is solely on setting the scene.
                    """
            };

            var characterCreator = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Character_Creator",
                Description = "Creates a memorable character that fits within the previously established environment.",
                Instructions =
                    """
                    You are a master character designer. Based on the environment described in the preceding text, introduce a single, memorable character.
                    Describe their appearance, their immediate goal or motivation, and a simple action they are performing within that scene. Ensure the character feels like a natural part of the world.
                    """
            };

            var plotGenerator = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Plot_Generator",
                Description = "Introduces a conflict or inciting incident into the scene.",
                Instructions =
                    """
                    You are a master of suspense. The scene and character are set. Now, introduce a single, unexpected event or an immediate conflict that complicates the character's situation.
                    Your goal is to create tension and leave a question in the reader's mind about what will happen next. Build directly upon the established setting and character actions.
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
            string userInput = "An airship of gleaming brass and wood lands softly in a hidden jungle valley.";

            Console.WriteLine($"GEZAMENLIJKE INPUT: \"{userInput}\"\n");
            Console.WriteLine("--- Resultaten van de Brainstorm ---");


            // OPLOSSING 3: Maak een runtime aan en geef deze mee
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();
            OrchestrationResult<string> results = await orchestration.InvokeAsync(userInput, runtime);

            
            string output = await results.GetValueAsync(TimeSpan.FromSeconds(3000));
			Console.ForegroundColor = ConsoleColor.DarkBlue;
			Console.WriteLine($"\n# FINAL RESULT:\n{output}");
			Console.ResetColor();

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