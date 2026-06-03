# OllamaSharp RAG SQLite

A Retrieval-Augmented Generation (RAG) system using Ollama for embeddings and chat generation, with SQLite-based vector storage.

## Features

* Local document indexing
* Ollama embeddings (`nomic-embed-text`)
* SQLite vector persistence
* Semantic search using cosine similarity
* Streaming LLM responses
* SHA256-based file deduplication

## Required NuGet Packages

```bash
dotnet add package OllamaSharp
dotnet add package Microsoft.Data.Sqlite
dotnet add package SQLitePCLRaw.bundle_e_sqlite3
```

## Prerequisites

```bash
ollama pull llama3:8b
ollama pull nomic-embed-text
ollama serve
```

## How It Works

1. Documents are indexed and converted into embeddings.
2. Embeddings are stored in SQLite.
3. User questions are converted into embeddings.
4. Cosine similarity finds the most relevant document chunks.
5. Retrieved context is sent to the LLM.
6. The answer is streamed back to the user.

## Run

```bash
dotnet build
dotnet run
```

## Project Structure

```text
Program.cs
ColorHelper.cs
HashHelper.cs
Services/
 ├─ RagService.cs
 ├─ DatabaseService.cs
 └─ DocumentIndexer.cs
Models/
 └─ ChunkRecord.cs
```

## License

MIT
