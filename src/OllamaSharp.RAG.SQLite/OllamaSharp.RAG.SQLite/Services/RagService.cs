using OllamaSharp.Models;
using OllamaSharp.RAG.SQLite.Models;
using System.Text;

namespace OllamaSharp.RAG.SQLite.Services;

public class RagService
{
    private readonly DocumentIndexer _indexer;
    private readonly DatabaseService _database;
    private readonly OllamaApiClient _llm;
    private readonly OllamaApiClient _embedder;

    public RagService()
    {
        _database = new DatabaseService();
        _indexer = new DocumentIndexer(_database);
        _llm = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3:8b");
        _embedder = new OllamaApiClient(new Uri("http://localhost:11434"), "nomic-embed-text");
    }

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        await _indexer.IndexAsync();

        Console.WriteLine($"Chunks in database: " + $"{await _database.GetChunkCountAsync()}");
    }

    // New streaming version that yields results as they're ready
    public IAsyncEnumerable<string> AskStreamingAsync(string question)
    {
        return ExecuteStreamingQuery(question);
    }

    private async IAsyncEnumerable<string> ExecuteStreamingQuery(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            yield return "Question cannot be empty.";
            yield break;
        }

        // Indicate that we're processing the question
        yield return "Processing your question...\n\n";

        var queryEmbedding = await _embedder.EmbedAsync(
            new EmbedRequest
            {
                Model = "nomic-embed-text",
                Input = new List<string> { question }
            });

        float[] queryVector = [.. queryEmbedding.Embeddings[0]];

        List<ChunkRecord> chunks = await _database.LoadChunksAsync();

        if (chunks.Count == 0)
        {
            yield return """
                   No indexed documents found.

                   Add documents and run indexing first.
                   """;
            yield break;
        }

        // Show that we're searching
        yield return "Searching relevant documents...\n\n";

        var matches =
            chunks
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Score = CosineSimilarity(queryVector, chunk.Embedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(10)
                .ToList();

        // Show what sources we found
        if (matches.Any())
        {
            yield return "Found relevant sources:\n";
            foreach (var match in matches.Take(3))
            {
                yield return $"   {match.Chunk.FileName} (similarity: {match.Score:F2})\n";
            }
            yield return "\nGenerating answer...\n\n";
        }

        var contextBuilder = new StringBuilder();

        foreach (var match in matches)
        {
            if (contextBuilder.Length > 7000)
            {
                break;
            }

            contextBuilder.AppendLine($"Source: {match.Chunk.FileName}");
            contextBuilder.AppendLine(match.Chunk.Content);
            contextBuilder.AppendLine();
        }

        string prompt = $"""
You are a helpful assistant.

Answer ONLY using the supplied context.

If the answer cannot be found in the context,
say "I could not find that information."

Context:
{contextBuilder}

Question:
{question}

Answer:
""";

        // Stream the response character by character as it comes from Ollama
        var responseBuffer = new StringBuilder();
        bool hasStarted = false;

        await foreach (var responseChunk in _llm.GenerateAsync(new GenerateRequest
        {
            Model = "llama3:8b",
            Prompt = prompt,
            Stream = true  // Ensure streaming is enabled
        }))
        {
            if (!string.IsNullOrWhiteSpace(responseChunk?.Response))
            {
                if (!hasStarted)
                {
                    yield return "\n";
                    hasStarted = true;
                }

                yield return responseChunk.Response;
                responseBuffer.Append(responseChunk.Response);
            }
        }

        if (responseBuffer.Length == 0)
        {
            yield return "\n\nNo response generated.";
        }
        else
        {
            yield return "\n";
        }
    }

    // Keep the original method for backward compatibility
    public async Task<string> AskAsync(string question)
    {
        var result = new StringBuilder();

        await foreach (var chunk in AskStreamingAsync(question))
        {
            result.Append(chunk);
        }

        return result.ToString();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0;
        double magA = 0;
        double magB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    public async Task<SystemStatistics> GetStatisticsAsync()
    {
        var stats = new SystemStatistics
        {
            ChunkCount = await _database.GetChunkCountAsync(),
            DocumentCount = await _database.GetDocumentCountAsync(),
            IsOllamaRunning = await CheckOllamaHealthAsync()
        };

        // Try to get last indexed date
        var lastIndexed = await _database.GetLastIndexedDateAsync();
        if (lastIndexed.HasValue)
            stats.LastIndexed = lastIndexed.Value;

        return stats;
    }

    private async Task<bool> CheckOllamaHealthAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            var response = await client.GetAsync("http://localhost:11434/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}