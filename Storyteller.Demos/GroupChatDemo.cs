using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
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

namespace Storyteller.Demos
{
    public class GroupChatDemo
    {
        public async Task RunAsync()
        {
            Kernel kernel = KernelFactory.CreateKernelForModel("qwen3:8b");
            var hero = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Aragorn_De_Held",
                Description = "Een dappere en nobele krijger die de leiding neemt.",
                Instructions = "Jij bent Aragorn, een dappere en nobele krijger. Je bent recht door zee, beschermend en neemt graag de leiding. Spreek plechtig."
            };

            var thief = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Lila_De_Dief",
                Description = "Een sarcastische en praktische dief die scherpe opmerkingen maakt.",
                Instructions = "Jij bent Lila, een snelle en sarcastische dief. Je bent cynisch, praktisch en maakt graag scherpe opmerkingen. Je vertrouwt niemand volledig."
            };

            var mage = new ChatCompletionAgent()
            {
                Kernel= kernel,
                Name = "Zoltan_De_Tovenaar",
                Description = "Een wijze maar cryptische magiër die in raadsels spreekt.",
                Instructions = "Jij bent Zoltan, een oude, wijze maar cryptische magiër. Je spreekt in raadsels en denkt na over de diepere betekenis van dingen."
            };

            // --- Orchestratie ---
            ChatHistory history = [];

            ValueTask responseCallback(Microsoft.SemanticKernel.ChatMessageContent response)
            {
                history.Add(response);
                return ValueTask.CompletedTask;
            }

            var orchestration = new GroupChatOrchestration(new RoundRobinGroupChatManager { MaximumInvocationCount = 5 }, hero, thief, mage)
            {
                ResponseCallback = responseCallback
            };
            string userInput = "Jullie staan voor een gigantische, verzegelde stenen deur. Een vreemd paars licht pulseert in de kieren. Wat doen jullie?";

            Console.WriteLine($"GEZAMENLIJKE INPUT: \"{userInput}\"\n");
            Console.WriteLine("--- Resultaten van de Brainstorm ---");


            // OPLOSSING 3: Maak een runtime aan en geef deze mee
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();
            OrchestrationResult<string> results = await orchestration.InvokeAsync(userInput, runtime);


            string output = await results.GetValueAsync(TimeSpan.FromSeconds(3000));
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\n# RESULT: {output}");
            Console.BackgroundColor = ConsoleColor.White;
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