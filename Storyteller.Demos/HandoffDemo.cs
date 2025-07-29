using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
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
    public class HandoffDemo
    {
        public async Task RunAsync()
        {
            Kernel kernel = KernelFactory.CreateKernelForModel("qwen3:8b");
            var storyteller = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Storyteller",
                Description = "Je bent de hoofdstoryteller die als triage-agent fungeert. Je bepaalt welke specialist nodig is voor de volgende stap.",
                Instructions =
                    """
                    Analyseer de input van de gebruiker. Jouw enige taak is om te bepalen welke expert hierna nodig is.
                    - Als de input gaat over de achtergrond of het verhaal, antwoord dan met ALLEEN de tekst: Lore_Master
                    - Als de input gaat over het doel van de quest, antwoord dan met ALLEEN de tekst: Goal_Setter
                    - Als de input gaat over een monster, antwoord dan met ALLEEN de tekst: Monster_Creator
                    - Als de input gaat over een raadsel, antwoord dan met ALLEEN de tekst: Riddle_Maker
                    Geef geen andere tekst of uitleg.
                    """,
                    Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }),
            };
            var loreMaster = new ChatCompletionAgent()
            {
                Kernel = KernelFactory.CreateKernelForModel("qwen3:8b"),
                Name = "Lore_Master",
                Description = "Bedenkt de achtergrond en naam voor een quest.",
                Instructions = "Bedenk een interessante achtergrond en een pakkende naam voor een quest.",
                Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.None()
                }),
            };
            var goalSetter = new ChatCompletionAgent()
            {
                Kernel = KernelFactory.CreateKernelForModel("qwen3:8b"),
                Name = "Goal_Setter",
                Description = "Definieert het einddoel van een quest.",
                Instructions = "Definieer een duidelijk en uitdagend einddoel voor een quest.",

                Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.None()
                }),
            };
            var monsterCreator = new ChatCompletionAgent()
            {
                Kernel = KernelFactory.CreateKernelForModel("qwen3:8b"),
                Name = "Monster_Creator",
                Description = "Ontwerpt een uniek monster passend bij de quest.",

                Instructions = "Bedenk een uniek monster dat een pad bewaakt, passend bij de quest.",
                Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.None()
                }),
            };
            var riddleMaker = new ChatCompletionAgent()
            {
                Kernel = KernelFactory.CreateKernelForModel("qwen3:8b"),
                Name = "Riddle_Maker",
                Description = "Schrijft een raadsel passend bij de quest.",
                Instructions = "Schrijf een slim raadsel voor een magische barrière, passend bij de quest.",
                Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.None()
                })

            };


            // --- Orchestratie ---
            //ChatHistory history = [];

            //ValueTask responseCallback(Microsoft.SemanticKernel.ChatMessageContent response)
            //{
            //    history.Add(response);
            //    return ValueTask.CompletedTask;
            //}
            OrchestrationMonitor monitor = new();
            var handoffs = OrchestrationHandoffs
                .StartWith(storyteller)
                .Add(storyteller, loreMaster, goalSetter, monsterCreator, riddleMaker)
                .Add(loreMaster, storyteller, "Transfer to this agent if the issue is lore related")
                .Add(goalSetter, storyteller, "Transfer to this agent if the issue is goal related")
                .Add(monsterCreator, storyteller, "Transfer to this agent if the issue is monster related")
                .Add(riddleMaker, storyteller, "Transfer to this agent if the issue is riddle related");



            ValueTask<Microsoft.SemanticKernel.ChatMessageContent> interactiveCallback()
            {
                Console.WriteLine("Geef input:");
                string input = Console.ReadLine();
                Console.WriteLine($"\n# INPUT: {input}\n");
                return ValueTask.FromResult(new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.User, input));
            }

            HandoffOrchestration orchestration = new HandoffOrchestration(
                handoffs,
                storyteller,
                loreMaster,
                goalSetter,
                monsterCreator,
                riddleMaker)
            {
                InteractiveCallback = interactiveCallback,
                ResponseCallback = monitor.ResponseCallback,
                
            };

            string userInput = "Ik wil een sfeervolle achtergrond bedenken voor mijn quest over een vervloekt bos.";

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