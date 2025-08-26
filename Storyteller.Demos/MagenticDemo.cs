using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Magentic;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Storyteller.Core; 
using Storyteller.Demos.internalUtilities; 

namespace Storyteller.Demos
{
    public class MagenticDemo
    {
        public async Task RunAsync(bool showOrchistrationCallback, bool showOrchistrationHistory)

		{
            Kernel kernel = await KernelFactory.CreateKernelForModel();

            // Agent 1: The initial idea generator.
            var conceptAgent = new ChatCompletionAgent()
            {
                Kernel = await KernelFactory.CreateKernelForModel(),
                Name = "ConceptAgent",
                Description = "You are an expert at creating high-level concepts for roleplay characters. Other agents will handle the details.",
                Instructions =
                    """
                    You create a character concept based on the user's request.
                    Your job is ONLY to define the core archetype, a key skill, and a major flaw.
                    Provide interesting hooks for other agents to expand upon. Do NOT write a background story or a name.
                    Example Output:
                    - Concept: A grizzled, exiled dwarven blacksmith.
                    - Key Skill: Can identify any metal by taste.
                    - Major Flaw: Deeply mistrusts all forms of magic.
                    """
            };

            // Agent 2: The storyteller who fleshes out the concept.
            var backgroundExpert = new ChatCompletionAgent()
            {
                Kernel = await KernelFactory.CreateKernelForModel(),
                Name = "BackgroundExpert",
                Description = "Writes a short, atmospheric background story for a character concept.",
                Instructions =
                    """
                    You are a creative writer. Take the character concept provided by the ConceptAgent and write a brief, compelling background story (2-3 paragraphs).
                    The story should explain the character's key skill and major flaw. Leave the character's name open.
                    """
            };

            // Agent 3: The namer who uses the background to find a fitting name.
            var nameGenerator = new ChatCompletionAgent()
            {
                Kernel = await KernelFactory.CreateKernelForModel(),
                Name = "NameGenerator",
                Description = "Generates a fitting and unique name based on a character's background.",
                Instructions = "You are a master namer. Based on the concept and background story from the other agents, generate one fitting and memorable name for the character."
            };

            // Agent 4: The final assembler who creates the character sheet.
            var sheetAgent = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "SheetAgent",
                Description = "You create the final, formatted character sheet.",
                Instructions =
                    """
                    You are the final assembler. Your task is to gather all the information from the other agents (Concept, Background, and Name) and compile it into a single, clean character sheet.
                    The final output should follow this exact format:

                    # Character Sheet
                    **Name:** [Insert Name from NameGenerator]
                    
                    ## Concept
                    [Insert Concept from ConceptAgent]
                    
                    ## Background
                    [Insert Background from BackgroundExpert]
                    """
            };

            // --- Orchestration ---
            var monitor = new OrchestrationMonitor();
            Kernel managerKernel = await KernelFactory.CreateKernelForModel();

            var manager =
                new StandardMagenticManager(managerKernel.GetRequiredService<IChatCompletionService>(), new OpenAIPromptExecutionSettings())
                {
                    MaximumInvocationCount = 20,
                };

            var orchestration = new MagenticOrchestration(manager, conceptAgent, sheetAgent, backgroundExpert, nameGenerator)
            {
                ResponseCallback = monitor.ResponseCallback,
            };

            string userInput = "Generate a complete character sheet based on a simple concept: a disgraced knight seeking redemption.";

            Console.WriteLine($"\nUSER INPUT: \"{userInput}\"\n");
            Console.WriteLine("--- Brainstorming Results ---");

            // Create and start the agent runtime environment.
            var runtime = new InProcessRuntime();
            await runtime.StartAsync();

            OrchestrationResult<string> results = await orchestration.InvokeAsync(userInput, runtime);

            // Display the final result.
            string output = await results.GetValueAsync();
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($"\n# FINAL RESULT:\n{output}");
            Console.ResetColor();

            // Display the full conversation history.
            Console.WriteLine("\n\n--- ORCHESTRATION HISTORY ---\n");
            foreach (ChatMessageContent message in monitor.History)
            {
                AIHelpers.WriteAgentChatMessage(message);
            }

            await runtime.RunUntilIdleAsync();
            Console.ResetColor();
        }
    }
}