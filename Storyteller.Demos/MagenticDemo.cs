using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Magentic;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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

namespace Storyteller.Demos
{
    public class MagenticDemo
    {
        public async Task RunAsync()
        {
            Kernel kernel = KernelFactory.CreateKernelForModel("qwen3:8b");
            var SheetAgent = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "SheetAgent",
                Description = "Je maakt charactersheets voor personages",
                Instructions = "Voor roleplay personages bedenk jij het character sheet. Dit is gebaseerd op allerlei informatie uit eerdere opdrachten."
            };

            var ConceptAgent = new ChatCompletionAgent()
            {
                Kernel = KernelFactory.CreateKernelForModel("qwen3:8b"),
                Name = "ConceptAgent",
                Description = "Je bent goed in het bedenken van concepten voor roleplay characters. De uitwerking wordt door andere agents gedaan",
                Instructions = "Bedenk een character concept, de achtergrond en naam worden door andere gedaan, het concept moet interessante haakjes hebben waar andere op verder kunnen"
            };

            var backgroundExpert = new ChatCompletionAgent()
            {
                Kernel = KernelFactory.CreateKernelForModel("qwen3:8b"),
                Name = "backgroundExpert",
                Description = "Schrijft een korte, sfeervolle achtergrond voor een personage.",
                Instructions = "Je bent een creatieve schrijver, je schrijft background verhalen voor roleplay personages. Hierbij laat je de naam nog open."
            };

            var nameGenerator = new ChatCompletionAgent()
            {
                Kernel= KernelFactory.CreateKernelForModel("qwen3:8b"),
                Name = "nameGenerator",
                Description = "Genereert een passende en unieke naam voor een personage.",
                Instructions = "Genereer eenpassende naam op basis van de eerder verkregen informatie van het personage"
            };

            // --- Orchestratie ---
            OrchestrationMonitor monitor = new();
            Kernel managerKernel = KernelFactory.CreateKernelForModel("qwen3:8b");
            StandardMagenticManager manager =
                new(managerKernel.GetRequiredService<IChatCompletionService>(), new OpenAIPromptExecutionSettings())
                {
                    MaximumInvocationCount = 20,
                };
            MagenticOrchestration orchestration = new (manager, ConceptAgent, SheetAgent, backgroundExpert, nameGenerator)
            {
                ResponseCallback = monitor.ResponseCallback,
                //StreamingResponseCallback = monitor.StreamingResultCallback,
            };
            string userInput = "Genereert een compleet character sheet op basis van een simpel concept.";

            Console.WriteLine($"GEZAMENLIJKE INPUT: \"{userInput}\"\n");
            Console.WriteLine("--- Resultaten van de Brainstorm ---");


            // OPLOSSING 3: Maak een runtime aan en geef deze mee
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();
            OrchestrationResult<string> results = await orchestration.InvokeAsync(userInput, runtime);


            string output = await results.GetValueAsync();
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\n# RESULT: {output}");
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\n\nORCHESTRATION HISTORY");

            foreach (Microsoft.SemanticKernel.ChatMessageContent message in monitor.History)
            {
                AIHelpers.WriteAgentChatMessage(message);
            }
            await runtime.RunUntilIdleAsync();


            Console.ForegroundColor = ConsoleColor.White;
        }  
    }
}