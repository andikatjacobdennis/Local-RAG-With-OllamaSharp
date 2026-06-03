# OllamaSharp RAG SQLite

A Retrieval-Augmented Generation (RAG) application built with Ollama and SQLite that enables local document search and AI-powered question answering without relying on external cloud services.

## What It Does

This application indexes documents from a local folder, generates vector embeddings using Ollama, and stores them in SQLite. When a user asks a question, the system retrieves the most relevant document content using semantic search and provides an answer based only on the retrieved context.

## Use Cases

* Internal company knowledge base
* Documentation search
* FAQ assistants
* Offline AI chat with private documents
* Product and support information retrieval
* Local RAG experimentation and learning

## Features

* Local document indexing
* Ollama embeddings (`nomic-embed-text`)
* SQLite vector persistence
* Semantic similarity search
* Streaming AI responses
* SHA256-based file deduplication
* Fully offline and self-hosted

## Required NuGet Packages

```bash
dotnet add package OllamaSharp
dotnet add package Microsoft.Data.Sqlite
dotnet add package SQLitePCLRaw.bundle_e_sqlite3
```

## Prerequisites

Install Ollama and download the required models:

```bash
ollama pull llama3:8b
ollama pull nomic-embed-text
ollama serve
```

## How It Works

1. Documents are scanned and indexed.
2. Text is converted into embeddings using Ollama.
3. Embeddings are stored in SQLite.
4. User questions are converted into embeddings.
5. Cosine similarity identifies the most relevant document chunks.
6. Retrieved content is provided as context to the LLM.
7. The generated answer is streamed back to the user.

## Usage

1. Place your documents inside the configured document folder.
2. Run the application.
3. Documents are automatically indexed during startup.
4. Enter questions through the console.
5. The system retrieves relevant information and generates an answer based on the indexed content.

Example:

```text
Question:
What products does the company sell?

Answer:
Based on the indexed documents, the company sells Product A, Product B, and Product C.
```

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
