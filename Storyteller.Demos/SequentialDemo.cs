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
        public async Task RunAsync()
        {
            Kernel kernel = KernelFactory.CreateKernelForModel("qwen3:8b");

            var worldBuilder = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Omgevings_Specialist",
                Instructions = "Jij bent een meester-verhalenverteller. Beschrijf een rijke, sfeervolle omgeving op basis van de input. Focus enkel op de omgeving.",
                Description = "Jij bent een meester-verhalenverteller. Beschrijf een rijke, sfeervolle omgeving op basis van de input."
            };

            var characterCreator = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Personage_Ontwerper",
                Description = "Jij creëert memorabele personages. ",
                Instructions = "Jij creëert memorabele personages. Introduceer één uniek personage dat past in de gegeven setting."
            };

            var plotGenerator = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Conflict_Generator",
                Instructions = "Jij bent een meester van spanning. Introduceer een onverwachte gebeurtenis of conflict dat past bij de setting en het personage.",
                Description= "Jij bent een meester van spanning. Introduceer een onverwachte gebeurtenis of conflicten voor het verhaal."
            };


            ChatHistory history = [];
            ValueTask responseCallback(Microsoft.SemanticKernel.ChatMessageContent response)
            {
                history.Add(response);
                return ValueTask.CompletedTask;
            }
            // --- Orchestratie ---
            OrchestrationMonitor monitor = new();
            
            var orchestration = new SequentialOrchestration(worldBuilder, characterCreator, plotGenerator)
            {
                ResponseCallback = responseCallback,
                //StreamingResponseCallback = monitor.StreamingResultCallback,
            };
            string userInput = "Een luchtschip van glimmend koper en hout landt zachtjes in een verborgen junglevallei.";

            Console.WriteLine($"GEZAMENLIJKE INPUT: \"{userInput}\"\n");
            Console.WriteLine("--- Resultaten van de Brainstorm ---");


            // OPLOSSING 3: Maak een runtime aan en geef deze mee
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();
            OrchestrationResult<string> results = await orchestration.InvokeAsync(userInput, runtime);

            
            string output = await results.GetValueAsync(TimeSpan.FromSeconds(3000));
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($"\n# RESULT: {output}");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\n\nORCHESTRATION HISTORY");

            foreach (Microsoft.SemanticKernel.ChatMessageContent message in history)
            {
                AIHelpers.WriteAgentChatMessage(message);
            }
            await runtime.RunUntilIdleAsync();
          
          
            Console.ForegroundColor = ConsoleColor.White;
        }  
    }
}