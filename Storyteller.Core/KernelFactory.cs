using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Storyteller.Core
{
    // Een static factory klasse om het aanmaken van Kernels te centraliseren.
    public static class KernelFactory
    {
        // Deze methode maakt een nieuwe Kernel aan die verbonden is
        // met een specifiek model op je lokale Ollama instance.
        public static async Task<Kernel> CreateKernelForModel(string modelId)
        {
            var builder = Kernel.CreateBuilder();
            var alias = "mistral-7b-v0.2";
            Console.WriteLine("\n\nLOADING MODAL "+ alias);
            var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: alias);
            Console.WriteLine("\n\nMODAL LOADED \n\n ");
            var model = await manager.GetModelInfoAsync(aliasOrModelId: alias);
            ApiKeyCredential key = new ApiKeyCredential(manager.ApiKey);
            OpenAIClient client = new OpenAIClient(key, new OpenAIClientOptions
            {
                Endpoint = manager.Endpoint
            });

            var chatClient = client.GetChatClient(model?.ModelId);
            var chatMessages = new List<ChatMessage>();

            chatMessages.Add(new SystemChatMessage("You are a helpful assistant helping people to get the most out of local AI"));
            chatMessages.Add(new UserChatMessage("Why is the sky blue?"));

            Console.Write("Assistant: ");

            // Stream the response
            string? response = null;
            await foreach (var streamingResult in chatClient.CompleteChatStreamingAsync(chatMessages))
            {
                Console.Write(streamingResult.ContentUpdate[0].Text);
                response += streamingResult.ContentUpdate[0].Text;

            }
            chatMessages.Add(new AssistantChatMessage(response));
            //builder.AddOpenAIChatCompletion(alias, openAIClient: client);
            Console.WriteLine();
            builder.AddOpenAIChatCompletion(
                modelId: model?.ModelId,
                apiKey: manager.ApiKey,
                endpoint: manager.Endpoint,
                httpClient: new HttpClient() // Optional; for customizing HTTP client
                    );
            // Voeg de chat completion service toe, wijzend naar je lokale Ollama.
            //builder.AddOllamaChatCompletion(
            //    modelId: modelId,
            //    endpoint: new Uri("http://localhost:11434")

            //); // Standaard Ollama endpoint
            var kernel = builder.Build();

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
            await foreach (Microsoft.SemanticKernel.ChatMessageContent agentresponse in worldBuilder.InvokeAsync(new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.User, "An airship of gleaming brass and wood lands softly in a hidden jungle valley.")))
            {
                // Process agent response(s)...
                Console.WriteLine("\n\n " + agentresponse.Content + " \n\n ");
                
            }
            
            Console.Write("done: ");
            return kernel;
        }
    }
}