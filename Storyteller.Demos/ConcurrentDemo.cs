using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using OllamaSharp.Models.Chat;
using OpenAI.Assistants;
using OpenAI.Chat;
using Storyteller.Core;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Azure.AI.Agents.Persistent;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Files;
using Storyteller.Demos.internalUtilities;

namespace Storyteller.Demos
{
    public class ConcurrentDemo
    {
        public async Task RunAsync()
        {
            Kernel kernel = KernelFactory.CreateKernelForModel("qwen3:8b");
            // OPLOSSING: Voeg een 'Description' toe aan elke agent.
            var fantasyExpert = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Fantasy_Expert",
                Description = "Gespecialiseerd in het schrijven van epische en magische fantasy verhalen.",
                Instructions = "Jij bent een expert in epische fantasy. Schrijf een sfeervolle, magische openingsscène."
            };

            var scifiExpert = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "SciFi_Expert",
                Description = "Gespecialiseerd in het schrijven van futuristische en technologische sciencefiction verhalen.",
                Instructions = "Jij bent een expert in sciencefiction. Schrijf een futuristische, technologische openingsscène."
            };

            var horrorExpert = new ChatCompletionAgent()
            {
                Kernel= kernel,
                Name = "Horror_Expert",
                Description = "Gespecialiseerd in het schrijven van enge en onheilspellende horror verhalen.",
                Instructions = "Jij bent een expert in horror. Schrijf een enge, onheilspellende openingsscène."
            };

            // --- Orchestratie ---
            OrchestrationMonitor monitor = new();
            
            var orchestration = new ConcurrentOrchestration(fantasyExpert, scifiExpert, horrorExpert)
            {
                ResponseCallback = monitor.ResponseCallback,
                //StreamingResponseCallback = monitor.StreamingResultCallback,
            };
            string userInput = "Een figuur staat op een heuvel en kijkt uit over een stad.";

            Console.WriteLine($"GEZAMENLIJKE INPUT: \"{userInput}\"\n");
            Console.WriteLine("--- Resultaten van de Brainstorm ---");


            // OPLOSSING 3: Maak een runtime aan en geef deze mee
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();
            OrchestrationResult<string[]> results = await orchestration.InvokeAsync(userInput, runtime);

            
            string[] output = await results.GetValueAsync(TimeSpan.FromSeconds(3000));
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\n# RESULT:\n{string.Join("\n\n", output.Select(text => $"{text}"))}");

            await runtime.RunUntilIdleAsync();
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\n\nORCHESTRATION HISTORY");
            foreach (Microsoft.SemanticKernel.ChatMessageContent message in monitor.History)
            {
                AIHelpers.WriteAgentChatMessage(message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }  
    }
}