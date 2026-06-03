namespace OllamaSharp.RAG.SQLite.Models;

public sealed class ChunkRecord
{
    public int Id { get; set; }

    public string FileHash { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;

    public float[] Embedding { get; set; } = Array.Empty<float>();
}