# OllamaSharp-RAG-Console

A simple Retrieval-Augmented Generation (RAG) implementation in C# using OllamaSharp, Llama 3, and Nomic embeddings.

This project demonstrates how to:

* Load local documents
* Chunk document content
* Generate embeddings with `nomic-embed-text`
* Perform cosine similarity search
* Retrieve relevant context
* Generate answers with `llama3:8b`

No external vector database is required. Embeddings are stored in memory for simplicity.

---

## Features

* Local-first RAG pipeline
* Uses Ollama running locally
* Supports `.txt` and `.json` files
* Automatic document chunking
* Cosine similarity retrieval
* Streaming LLM responses
* Lightweight and easy to understand

---

## Architecture

```text
Documents
    ↓
Chunking
    ↓
Embeddings (nomic-embed-text)
    ↓
In-Memory Vector Store
    ↓
Similarity Search
    ↓
Context Retrieval
    ↓
Llama3 Prompt
    ↓
Answer
```

---

## Requirements

### .NET

* .NET 8 SDK

### Ollama

Install Ollama:

https://ollama.com

Pull required models:

```bash
ollama pull llama3:8b
ollama pull nomic-embed-text
```

Start Ollama:

```bash
ollama serve
```

Default endpoint:

```text
http://localhost:11434
```

---

## Installation

Clone the repository:

```bash
git clone https://github.com/YOUR_USERNAME/OllamaSharp-RAG-Console.git
```

Enter the project:

```bash
cd OllamaSharp-RAG-Console
```

Install OllamaSharp:

```bash
dotnet add package OllamaSharp
```

Build:

```bash
dotnet build
```

Run:

```bash
dotnet run
```

---

## Document Folder

Place your documents in:

```text
C:\RagDocuments
```

Supported formats:

```text
.txt
.json
```

Example:

```text
C:\RagDocuments
│
├── products.txt
├── faq.txt
└── company.json
```

---

## How It Works

### 1. Load Documents

The application scans the document folder and reads all supported files.

### 2. Chunk Documents

Documents are split into chunks of 1000 characters.

```csharp
ChunkText(text, 1000);
```

### 3. Create Embeddings

Each chunk is converted into a vector using:

```text
nomic-embed-text
```

### 4. Embed User Question

The user's question is embedded using the same embedding model.

### 5. Similarity Search

Cosine similarity is calculated between the question and document chunks.

```csharp
CosineSimilarity(queryVector, chunk.Embedding);
```

### 6. Retrieve Top Matches

The top 3 most relevant chunks are selected.

```csharp
.Take(3)
```

### 7. Generate Response

Retrieved context is injected into the prompt and sent to:

```text
llama3:8b
```

---

## Example

Question:

```text
What products does the company sell?
```

Retrieved Context:

```text
Source: products.txt

Product A ...
Product B ...
```

Generated Answer:

```text
The company sells Product A and Product B.
```

---

## Configuration

### Change Chunk Size

```csharp
chunkSize: 1000
```

### Change Number of Retrieved Chunks

```csharp
.Take(3)
```

### Change LLM

```csharp
"llama3:8b"
```

Examples:

```csharp
"llama3.1:8b"
"qwen3:8b"
"mistral"
```

### Change Embedding Model

```csharp
"nomic-embed-text"
```

---

## Limitations

This example intentionally keeps things simple:

* Embeddings are generated every startup
* No persistent vector database
* No metadata filtering
* No reranking
* Character-based chunking only

For production systems consider:

* PostgreSQL + pgvector
* ChromaDB
* Qdrant
* Milvus
* Weaviate

---

## Future Improvements

* PDF support
* DOCX support
* Persistent embeddings
* Hybrid search
* Reranking
* Semantic chunking
* Metadata filtering
* ASP.NET Core API
* Blazor UI

---

## Tech Stack

* C#
* .NET 9
* OllamaSharp
* Ollama
* Llama 3
* Nomic Embed Text

---

## License

MIT License

Feel free to use, modify, and improve this project.
