using OllamaSharp.RAG.SQLite.Helpers;
using OllamaSharp.RAG.SQLite.Services;
using SQLitePCL;
using System.Text;

namespace OllamaSharp.RAG.SQLite;

internal class Program
{
    private static async Task Main()
    {
        try
        {
            // Use ASCII/ANSI encoding for maximum compatibility
            Console.OutputEncoding = Encoding.ASCII;
            Console.InputEncoding = Encoding.ASCII;

            Batteries.Init();

            // Display banner
            ColorHelper.WriteBanner(new[]
            {
                "OllamaSharp RAG SQLite",
                "Retrieval-Augmented Generation with Local LLMs",
                "Version 1.0"
            });

            ColorHelper.WriteDivider('-', 60);

            var ragService = new RagService();

            ColorHelper.WriteMessage(MessageType.Processing, "Initializing system...");

            ColorHelper.WriteDivider('.', 60);

            await ragService.InitializeAsync();

            ColorHelper.WriteDivider('=', 60);
            ColorHelper.WriteMessage(MessageType.Success, "System ready! Type 'exit' to quit");
            ColorHelper.WriteDivider('=', 60);

            // Display help
            await DisplayHelp();

            while (true)
            {
                ColorHelper.WriteDivider('-', 60);
                ColorHelper.WriteMessage(MessageType.Question, "Your question: ", false);
                string? question = Console.ReadLine();

                if (string.Equals(question, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    ColorHelper.WriteDivider('=', 60);
                    ColorHelper.WriteMessage(MessageType.Complete, "Goodbye! Have a great day!");
                    ColorHelper.WriteDivider('=', 60);
                    break;
                }

                if (string.Equals(question, "help", StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayHelp();
                    continue;
                }

                if (string.Equals(question, "stats", StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayStats(ragService);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(question))
                {
                    ColorHelper.WriteWarning("Please enter a question or type 'help' for commands");
                    continue;
                }

                ColorHelper.WriteDivider('-', 60);
                ColorHelper.WriteMessage(MessageType.AI, "Assistant:", true);
                ColorHelper.WriteDivider('.', 60);

                // Stream the response with colors
                await foreach (var chunk in ragService.AskStreamingAsync(question))
                {
                    // Color-code different parts of the response (ASCII only)
                    if (chunk.Contains("[") && chunk.Contains("]"))
                    {
                        ColorHelper.Write(chunk, ConsoleColor.Yellow);
                    }
                    else if (chunk.Contains("Error") || chunk.Contains("ERR"))
                    {
                        ColorHelper.Write(chunk, ConsoleColor.Red);
                    }
                    else if (chunk.Contains("OK") || chunk.Contains("Found") || chunk.Contains("Complete"))
                    {
                        ColorHelper.Write(chunk, ConsoleColor.Green);
                    }
                    else if (chunk.Contains("...") || chunk.Contains(">>>"))
                    {
                        ColorHelper.Write(chunk, ConsoleColor.DarkGray);
                    }
                    else
                    {
                        ColorHelper.Write(chunk, ConsoleColor.White);
                    }
                }

                Console.WriteLine();
                ColorHelper.WriteDivider('=', 60);
            }
        }
        catch (Exception ex)
        {
            ColorHelper.WriteDivider('!', 60);
            ColorHelper.WriteError($"Fatal error: {ex.Message}");
            ColorHelper.WriteDivider('!', 60);

#if DEBUG
            ColorHelper.WriteColoredMessage(ex.ToString(), ConsoleColor.DarkRed, "", true);
#endif

            ColorHelper.WriteWarning("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static async Task DisplayHelp()
    {
        ColorHelper.WriteDivider('-', 60);
        ColorHelper.WriteColoredMessage("COMMANDS", ConsoleColor.Cyan, "", true);
        ColorHelper.WriteDivider('-', 60);
        ColorHelper.WriteColoredMessage("  exit    - Exit the application", ConsoleColor.White, "", true);
        ColorHelper.WriteColoredMessage("  help    - Show this help message", ConsoleColor.White, "", true);
        ColorHelper.WriteColoredMessage("  stats   - Show database statistics", ConsoleColor.White, "", true);
        ColorHelper.WriteColoredMessage("  [question] - Ask any question", ConsoleColor.White, "", true);
        ColorHelper.WriteDivider('-', 60);

        await Task.CompletedTask;
    }

    private static async Task DisplayStats(RagService ragService)
    {
        ColorHelper.WriteDivider('=', 60);
        ColorHelper.WriteColoredMessage("SYSTEM STATISTICS", ConsoleColor.Cyan, "", true);
        ColorHelper.WriteDivider('=', 60);

        var stats = await ragService.GetStatisticsAsync();

        var data = new Dictionary<string, string>
        {
            { "Chunks in database", stats.ChunkCount.ToString() },
            { "Documents indexed", stats.DocumentCount.ToString() },
            { "Last indexed", stats.LastIndexed?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never" },
            { "Model (LLM)", stats.LLMModel },
            { "Model (Embeddings)", stats.EmbeddingModel },
            { "Status", stats.IsOllamaRunning ? "[ONLINE]" : "[OFFLINE]" }
        };

        ColorHelper.WriteTable(data);
        ColorHelper.WriteDivider('=', 60);

        await Task.CompletedTask;
    }
}

// Statistics class for the service
public class SystemStatistics
{
    public int ChunkCount { get; set; }
    public int DocumentCount { get; set; }
    public DateTime? LastIndexed { get; set; }
    public string LLMModel { get; set; } = "llama3:8b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public bool IsOllamaRunning { get; set; }
}