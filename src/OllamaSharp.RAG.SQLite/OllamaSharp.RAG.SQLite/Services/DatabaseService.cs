using Microsoft.Data.Sqlite;
using OllamaSharp.RAG.SQLite.Models;
using System.Text.Json;

namespace OllamaSharp.RAG.SQLite.Services;

public class DatabaseService
{
    private const string DatabaseFile = "rag.db";

    private readonly string _connectionString = $"Data Source={DatabaseFile}";

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
        """
        CREATE TABLE IF NOT EXISTS Files
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FileName TEXT NOT NULL,
            FileHash TEXT NOT NULL UNIQUE,
            IndexedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Chunks
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FileHash TEXT NOT NULL,
            FileName TEXT NOT NULL,
            ChunkIndex INTEGER NOT NULL,
            Content TEXT NOT NULL,
            Embedding TEXT NOT NULL
        );
        """;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> FileExistsAsync(
        string fileHash)
    {
        using var connection = new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
        """
        SELECT COUNT(*)
        FROM Files
        WHERE FileHash = $hash
        """;

        command.Parameters.AddWithValue("$hash", fileHash);
        var result = (long)(await command.ExecuteScalarAsync() ?? 0);
        return result > 0;
    }

    public async Task SaveFileAsync(
        string fileName,
        string fileHash)
    {
        using var connection = new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
        """
        INSERT INTO Files
        (
            FileName,
            FileHash,
            IndexedAt
        )
        VALUES
        (
            $fileName,
            $fileHash,
            $indexedAt
        )
        """;

        command.Parameters.AddWithValue("$fileName", fileName);
        command.Parameters.AddWithValue("$fileHash", fileHash);

        command.Parameters.AddWithValue("$indexedAt", DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveChunkAsync(
        ChunkRecord chunk)
    {
        using var connection = new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
        """
        INSERT INTO Chunks
        (
            FileHash,
            FileName,
            ChunkIndex,
            Content,
            Embedding
        )
        VALUES
        (
            $fileHash,
            $fileName,
            $chunkIndex,
            $content,
            $embedding
        )
        """;

        command.Parameters.AddWithValue("$fileHash", chunk.FileHash);
        command.Parameters.AddWithValue("$fileName", chunk.FileName);
        command.Parameters.AddWithValue("$chunkIndex", chunk.ChunkIndex);
        command.Parameters.AddWithValue("$content", chunk.Content);
        command.Parameters.AddWithValue("$embedding", JsonSerializer.Serialize(chunk.Embedding));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<ChunkRecord>>
        LoadChunksAsync()
    {
        var chunks = new List<ChunkRecord>();

        using var connection = new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
        """
        SELECT
            Id,
            FileHash,
            FileName,
            ChunkIndex,
            Content,
            Embedding
        FROM Chunks
        """;

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            chunks.Add(
                new ChunkRecord
                {
                    Id = reader.GetInt32(0),
                    FileHash = reader.GetString(1),
                    FileName = reader.GetString(2),
                    ChunkIndex = reader.GetInt32(3),
                    Content = reader.GetString(4),
                    Embedding =
                        JsonSerializer.Deserialize<float[]>(
                            reader.GetString(5))
                        ?? Array.Empty<float>()
                });
        }

        return chunks;
    }

    public async Task<int> GetChunkCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText =
        """
        SELECT COUNT(*)
        FROM Chunks
        """;

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<int> GetDocumentCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Files";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<DateTime?> GetLastIndexedDateAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX(IndexedAt) FROM Files";
        var result = await command.ExecuteScalarAsync();

        if (result != null && result != DBNull.Value)
        {
            if (DateTime.TryParse(result.ToString(), out var date))
                return date;
        }

        return null;
    }
}