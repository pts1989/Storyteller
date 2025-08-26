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
    
    public static class KernelFactory
    {

        // Deze methode maakt een nieuwe Kernel aan die verbonden is
        // met een specifiek model op je lokale Ollama instance.
        public static async Task<Kernel> CreateKernelForModel(string modelId = "mistral-7b-v0.2")
        {
         
            var builder = Kernel.CreateBuilder();


            // AzureFoundry Local demo
            //Console.WriteLine("\n\nLOADING MODAL "+ modelId);
            //var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: modelId);
            //Console.WriteLine("\n\nMODAL LOADED \n\n ");
            //var model = await manager.GetModelInfoAsync(aliasOrModelId: modelId);
            
            //builder.AddOpenAIChatCompletion(
            //    modelId: model?.ModelId,
            //    apiKey: manager.ApiKey,
            //    endpoint: manager.Endpoint
            //);

            // Ollama Local demo
            //builder.AddOllamaChatCompletion(
            //    modelId: modelId,
            //    endpoint: new Uri("http://localhost:11434")
            //); // Standaard Ollama endpoint

            // Azure Ai Foundry demo
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: "gpt-4o-mini",
                apiKey: ConfigurationHelper.config.GetSection("apiKey").Value,
                endpoint: "https://ai-paulstolk7788ai591892606067.openai.azure.com/",
                modelId: "gpt-4o-mini" 
            );

            return builder.Build(); 
        }
    }
}