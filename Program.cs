using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

internal class Program
{
    /// <summary>
    /// Program entry point.
    /// </summary>
    private static async Task Main()
    {
        //
        // Pick up our OpenAI endpoint and API key from the environment
        //
        string modelId = Environment.GetEnvironmentVariable("MY_AZURE_OPENAI_MODEL") ?? "gpt-35-turbo";
        string? myEndPoint = Environment.GetEnvironmentVariable("MY_AZURE_OPENAI_ENDPOINT");
        string? myApiKey = Environment.GetEnvironmentVariable("MY_AZURE_OPENAI_API_KEY");

        //
        // Make sure we picked up values from the environment
        //
        if (myEndPoint == null || myApiKey == null)
        {
            Console.WriteLine("The 'MY_AZURE_OPENAI_ENDPOINT' and 'MY_AZURE_OPENAI_API_KEY' environment variables must be set");
            Console.WriteLine("and the 'MY_AZURE_OPENAI_MODEL' environment variable can be set to the model you want to use.");
            Console.WriteLine("\nPlease set them and try again.");
            return;
        }

        //
        // Instantiate a kernel with OpenAI chat completion
        //
        IKernelBuilder builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, myEndPoint, myApiKey);

        //
        // Set the enterprise components
        //
        builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

        //
        // Build the kernel so we can use it
        //
        Kernel kernel = builder.Build();
        IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        //
        // Enable planning
        //
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        //
        // Create a history store the conversation
        //
        ChatHistory history = [];

        //
        // Time to have a conversation
        //
        string? userInput;
        while (true)
        {
            //
            // Get the user input
            //
            Console.Write("User > ");
            userInput = Console.ReadLine();

            //
            // Nothing entered, bail out
            //
            if (string.IsNullOrWhiteSpace(userInput))
                break;

            //
            // Whatever the user said, add it to the history
            //
            history.AddUserMessage(userInput);

            //
            // Ask and you shall receive
            //
            ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(
                               history,
                               executionSettings: openAIPromptExecutionSettings,
                               kernel: kernel);

            //
            // Print the results
            //
            Console.WriteLine("Imaginary Friend Response: " + result);

            //
            // Add the message from the agent to the chat history
            //
            history.AddMessage(result.Role, result.Content ?? string.Empty);
        }

        Console.WriteLine("Our Imaginary Friend says 'Go Pack Sand'.");

        //
        // Just for giggles, dump out our conversation history to a file
        //
        string historyFile = @"C:\Data\MyChatHistory.txt";
        File.WriteAllLines(historyFile, history.Select(h => h.ToString()));
        Console.WriteLine($"Chat history written to '{historyFile}'.");
    }
}