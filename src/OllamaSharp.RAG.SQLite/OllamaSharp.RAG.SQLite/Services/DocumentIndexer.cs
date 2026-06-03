using OllamaSharp.Models;
using OllamaSharp.RAG.SQLite.Helpers;
using OllamaSharp.RAG.SQLite.Models;

namespace OllamaSharp.RAG.SQLite.Services;

public class DocumentIndexer
{
    private const string DocumentsFolder =
        @"C:\RagDocuments";

    private readonly DatabaseService _database;

    private readonly OllamaApiClient _embedder;

    public DocumentIndexer(
        DatabaseService database)
    {
        _database = database;

        _embedder = new OllamaApiClient(
            new Uri("http://localhost:11434"),
            "nomic-embed-text");
    }

    public async Task IndexAsync()
    {
        if (!Directory.Exists(
            DocumentsFolder))
        {
            Console.WriteLine(
                $"Folder not found: {DocumentsFolder}");

            return;
        }

        var files = Directory.GetFiles(
            DocumentsFolder,
            "*.*",
            SearchOption.AllDirectories);

        foreach (var file in files)
        {
            string hash =
                HashHelper.GetFileHash(file);

            bool exists =
                await _database
                    .FileExistsAsync(hash);

            if (exists)
            {
                Console.WriteLine(
                    $"Skipped: {Path.GetFileName(file)}");

                continue;
            }

            Console.WriteLine(
                $"Indexing: {Path.GetFileName(file)}");

            string text =
                await File.ReadAllTextAsync(file);

            var embedding =
                await _embedder.EmbedAsync(
                    new EmbedRequest
                    {
                        Model =
                            "nomic-embed-text",
                        Input =
                            new List<string>
                            {
                                text
                            }
                    });

            await _database.SaveChunkAsync(
                new ChunkRecord
                {
                    FileHash = hash,
                    FileName =
                        Path.GetFileName(file),
                    ChunkIndex = 0,
                    Content = text,
                    Embedding =
                        embedding
                            .Embeddings[0]
                            .ToArray()
                });

            await _database.SaveFileAsync(
                Path.GetFileName(file),
                hash);
        }
    }
}