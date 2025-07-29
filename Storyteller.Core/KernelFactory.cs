using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Storyteller.Core
{
    // Een static factory klasse om het aanmaken van Kernels te centraliseren.
    public static class KernelFactory
    {
        // Deze methode maakt een nieuwe Kernel aan die verbonden is
        // met een specifiek model op je lokale Ollama instance.
        public static Kernel CreateKernelForModel(string modelId)
        {
            var builder = Kernel.CreateBuilder();

            // Voeg de chat completion service toe, wijzend naar je lokale Ollama.
            builder.AddOllamaChatCompletion(
                modelId: modelId,
                endpoint: new Uri("http://localhost:11434")

            ); // Standaard Ollama endpoint
            return builder.Build();
        }
    }
}