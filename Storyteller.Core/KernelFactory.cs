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
        public static async Task<Kernel> CreateKernelForModel(string modelId = "phi-4-mini")
        {
         
            var builder = Kernel.CreateBuilder();


            //////AzureFoundry Local demo
            Console.WriteLine("\n\nLOADING " + modelId);
            var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: modelId);
            Console.WriteLine("\n\nLOADED \n\n ");
            var model = await manager.GetModelInfoAsync(aliasOrModelId: modelId);

            builder.AddOpenAIChatCompletion(
                modelId: model?.ModelId,
                apiKey: manager.ApiKey,
                endpoint: manager.Endpoint
            );

            // Ollama Local demo //works with mcp
            //builder.AddOllamaChatCompletion(
            //    modelId: "gpt-oss:20b",
            //    endpoint: new Uri("http://localhost:11434")
            //); // Standaard Ollama endpoint

            //// Azure Ai Foundry demo
            //builder.AddAzureOpenAIChatCompletion(
            //    deploymentName: "gpt-5-mini",
            //    apiKey: ConfigurationHelper.config.GetSection("gpt").Value,
            //    endpoint: ConfigurationHelper.config.GetSection("azureAIFoundryUrl").Value,
            //    modelId: "gpt-5-mini"
            //);


            //builder.AddAzureOpenAIChatCompletion(
            //    deploymentName: "gpt-4o",
            //    apiKey: ConfigurationHelper.config.GetSection("apikey").Value,
            //    endpoint: ConfigurationHelper.config.GetSection("azureAIFoundryUrl").Value,
            //    modelId: "gpt-4o"
            //);

            //builder.AddAzureOpenAIChatCompletion(
            //    deploymentName: "gpt-oss-120b",
            //    apiKey: ConfigurationHelper.config.GetSection("apikey").Value,
            //    endpoint: ConfigurationHelper.config.GetSection("azureAIFoundryUrl").Value,
            //    modelId: "gpt-oss-120b"
            //);


            return builder.Build(); 
        }
    }
}