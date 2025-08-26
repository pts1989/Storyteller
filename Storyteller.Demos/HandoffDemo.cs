using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Storyteller.Core;
using Storyteller.Demos.internalUtilities;


namespace Storyteller.Demos
{
    public class HandoffDemo
    {
        public async Task RunAsync(bool showOrchistrationCallback, bool showOrchistrationHistory)
		{
            Kernel kernel = await KernelFactory.CreateKernelForModel();

            var storyteller = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Storyteller",
                Description = "Acts as a triage agent, determining which specialist is needed for the next step.",
                Instructions =
                   """
                    You are a router. Your sole responsibility is to analyze the user's request and determine the appropriate specialist agent.
                    - For requests about story, background, or lore, output ONLY the name: Lore_Master
                    - For requests about the quest's objective or goal, output ONLY the name: Goal_Setter
                    - For requests about creating a creature or monster, output ONLY the name: Monster_Creator
                    - For requests about a puzzle or riddle, output ONLY the name: Riddle_Maker
                    Your output must be a single word, the name of the agent. Do not add any other text, explanation, or punctuation.
                    """,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            };

            var loreMaster = new ChatCompletionAgent()
            {
                Kernel = await KernelFactory.CreateKernelForModel(),
                Name = "Lore_Master",
                Description = "World-builder who crafts the background story and name for a quest.",
                Instructions = "You are a master world-builder and historian. Based on the user's request and the conversation history, craft a rich, compelling background story for a fantasy quest. Conclude with a fitting and evocative name for the quest. The tone should be mysterious and epic.",
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.None() }),
            };

            // Specialist Agent: Defines the quest's main goal.
            var goalSetter = new ChatCompletionAgent()
            {
                Kernel = await  KernelFactory.CreateKernelForModel(),
                Name = "Goal_Setter",
                Description = "Quest designer who defines the final objective of a quest.",
                Instructions = "You are a master quest designer. Using the established lore from the conversation history, define a clear, challenging, and rewarding final objective for the quest. The goal should be specific and actionable.",
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.None() }),
            };

            // Specialist Agent: Designs a monster for the quest.
            var monsterCreator = new ChatCompletionAgent()
            {
                Kernel = await KernelFactory.CreateKernelForModel(),
                Name = "Monster_Creator",
                Description = "Creature designer who creates a unique monster fitting for the quest.",
                Instructions = "You are a legendary creature designer. Based on the quest's theme and lore from the conversation history, design a unique and fearsome monster. Describe its appearance, abilities, and why it guards a crucial path or location within the quest.",
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.None() }),
            };

            // Specialist Agent: Creates a riddle for the quest.
            var riddleMaker = new ChatCompletionAgent()
            {
                Kernel = await KernelFactory.CreateKernelForModel(),
                Name = "Riddle_Maker",
                Description = "Puzzle master who writes a clever riddle fitting for the quest.",
                Instructions = "You are a master of puzzles and enigmas. Drawing inspiration from the established quest lore in the conversation history, write a clever and thematic riddle. The riddle will be used to unlock a magical barrier or a hidden secret. Provide the riddle and the answer separately.",
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.None() }),
            };

            OrchestrationMonitor monitor = new(showOrchistrationCallback);
            var handoffs = OrchestrationHandoffs
                .StartWith(storyteller)
                .Add(storyteller, loreMaster, goalSetter, monsterCreator, riddleMaker)
                // 2. Define the pathways FROM the Storyteller (triage) TO each specialist.
                // The triage agent's prompt logic will decide which path to take based on these descriptions.
                .Add(storyteller, loreMaster, "Handoff to develop the story, background, or world-building elements.")
                .Add(storyteller, goalSetter, "Handoff to define a clear and compelling quest objective.")
                .Add(storyteller, monsterCreator, "Handoff to design a unique creature for the quest.")
                .Add(storyteller, riddleMaker, "Handoff to create a thematic puzzle or enigma.")

                // 3. Define the pathways FROM each specialist BACK to the Storyteller.
                // After a specialist has finished, it always returns to the triage agent to process the result or await the next instruction.
                .Add(loreMaster, storyteller, "Return to the router after the lore has been established.")
                .Add(goalSetter, storyteller, "Return to the router after the quest goal has been defined.")
                .Add(monsterCreator, storyteller, "Return to the router after the monster has been created.")
                .Add(riddleMaker, storyteller, "Return to the router after the riddle has been written.");


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

            string userInput = "I want to create an atmospheric background for my quest about a cursed forest.";

            Console.WriteLine($"GEZAMENLIJKE INPUT: \"{userInput}\"\n");
            Console.WriteLine("--- Resultaten van de Brainstorm ---");


            // OPLOSSING 3: Maak een runtime aan en geef deze mee
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();
            OrchestrationResult<string> results = await orchestration.InvokeAsync(userInput, runtime);


            string output = await results.GetValueAsync();
			Console.ForegroundColor = ConsoleColor.DarkBlue;
			Console.WriteLine($"\n# FINAL RESULT:\n{output}");
			Console.ResetColor();

			if (showOrchistrationHistory)
			{
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.WriteLine("\n\nORCHESTRATION HISTORY\n");

				foreach (Microsoft.SemanticKernel.ChatMessageContent message in monitor.History)
				{
					AIHelpers.WriteAgentChatMessage(message);
				}
			}
			await runtime.RunUntilIdleAsync();


			Console.ResetColor();
		}
    }
}