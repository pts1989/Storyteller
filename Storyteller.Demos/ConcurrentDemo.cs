using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Storyteller.Core;
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
                Description = "A master storyteller who weaves tales of ancient magic and epic destinies.",
                Instructions =
                    """
                    You are a master of epic fantasy. Your task is to write a captivating opening scene.
                    Focus on a sense of ancient wonder, mythical landscapes, and the subtle hum of forgotten magic.
                    Hint at a larger destiny or a looming prophecy.
                    """
            };

            var scifiExpert = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "SciFi_Expert",
                Description = "A futurist who envisions worlds of advanced technology and cybernetic wonders.",
                Instructions =
                    """
                    You are an expert in hard science fiction. Your task is to write a compelling opening scene.
                    Describe a futuristic, high-tech environment. Focus on specific details like shimmering data streams, the hum of anti-gravity engines, or cybernetic enhancements.
                    Create a sense of technological awe or alienation.
                    """
            };

            var horrorExpert = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "Horror_Expert",
                Description = "A purveyor of nightmares who crafts tales of suspense and dread.",
                Instructions =
                    """
                    You are a master of psychological horror. Your task is to write a deeply unsettling opening scene.
                    Build suspense not with jump scares, but with a palpable sense of dread.
                    Focus on unsettling details: a silence that is too deep, a shadow that moves wrong, a sound that doesn't belong.
                    """
            };
            //var fantasyExpert = new ChatCompletionAgent()
            //{
            //    Kernel = kernel,
            //    Name = "Fantasy_Expert",
            //    Description = "Gespecialiseerd in het schrijven van epische en magische fantasy verhalen.",
            //    Instructions = "Jij bent een expert in epische fantasy. Schrijf een sfeervolle, magische openingsscène."
            //};

            //var scifiExpert = new ChatCompletionAgent()
            //{
            //    Kernel = kernel,
            //    Name = "SciFi_Expert",
            //    Description = "Gespecialiseerd in het schrijven van futuristische en technologische sciencefiction verhalen.",
            //    Instructions = "Jij bent een expert in sciencefiction. Schrijf een futuristische, technologische openingsscène."
            //};

            //var horrorExpert = new ChatCompletionAgent()
            //{
            //    Kernel= kernel,
            //    Name = "Horror_Expert",
            //    Description = "Gespecialiseerd in het schrijven van enge en onheilspellende horror verhalen.",
            //    Instructions = "Jij bent een expert in horror. Schrijf een enge, onheilspellende openingsscène."
            //};

            // --- Orchestratie ---
            OrchestrationMonitor monitor = new();
            
            var orchestration = new ConcurrentOrchestration(fantasyExpert, scifiExpert, horrorExpert)
            {
                ResponseCallback = monitor.ResponseCallback,
            };
            string userInput = "A figure stands on a hill overlooking a city.";
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