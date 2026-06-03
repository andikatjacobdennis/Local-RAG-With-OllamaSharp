# OllamaSharp RAG SQLite - Technical Documentation

## Technical Architecture Document

**Version**: 1.0  
**Target Framework**: .NET 9.0  
**Language**: C# 13  
**Last Updated**: 2026-06-03

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture](#architecture)
3. [Component Details](#component-details)
4. [Data Flow](#data-flow)
5. [Database Schema](#database-schema)
6. [Algorithms](#algorithms)
7. [API Reference](#api-reference)
8. [Configuration](#configuration)
9. [Performance Characteristics](#performance-characteristics)
10. [Error Handling](#error-handling)
11. [Security Considerations](#security-considerations)
12. [Extensibility Points](#extensibility-points)

---

## System Overview

OllamaSharp RAG SQLite is a local-first Retrieval-Augmented Generation system that combines vector similarity search with large language models. The system eliminates external dependencies by using SQLite for vector storage and Ollama for both embeddings and text generation.

### Key Characteristics

| Property | Value |
|----------|-------|
| Deployment Model | Local-only |
| Vector Storage | SQLite with JSON serialization |
| Search Algorithm | Brute-force Cosine Similarity |
| Embedding Dimension | 768 from nomic-embed-text |
| Context Window | ≈7000 characters |
| Chunking Strategy | Whole-file with pluggable interface |

---

## Architecture

### High-Level Architecture

```mermaid
flowchart TB
    subgraph Input["Input Layer"]
        DOC[Documents in C:\RagDocuments folder]
        Q[User Question]
    end
    
    subgraph Processing["Processing Layer"]
        H[HashHelper for SHA256 hashing]
        I[DocumentIndexer for chunking and embedding]
        R[RagService for query orchestration]
    end
    
    subgraph Storage["Storage Layer"]
        DB[SQLite Database file rag.db]
        F[Files Table]
        C[Chunks Table]
    end
    
    subgraph External["External Services"]
        O[Ollama API on localhost port 11434]
        EMB[nomic-embed-text model]
        LLM[llama3:8b model]
    end
    
    subgraph Output["Output Layer"]
        UI[Console UI with ASCII colors]
        RESP[Streaming Response]
    end
    
    DOC --> H
    H --> I
    I --> EMB
    EMB --> C
    C --> F
    
    Q --> R
    R --> EMB
    R --> C
    R --> LLM
    LLM --> RESP
    RESP --> UI
    
    O --> EMB
    O --> LLM
```

### Component Interaction Diagram

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant RagService
    participant DatabaseService
    participant OllamaAPI
    
    User->>Program: Enter question
    Program->>RagService: Call AskStreamingAsync with question
    
    activate RagService
    RagService->>OllamaAPI: Call EmbedAsync with question
    OllamaAPI-->>RagService: Return queryVector
    
    RagService->>DatabaseService: Call LoadChunksAsync
    DatabaseService-->>RagService: Return List of ChunkRecord objects
    
    RagService->>RagService: Calculate CosineSimilarity for each chunk
    RagService->>RagService: Select top 10 matches
    
    RagService->>RagService: Build prompt with context
    
    RagService->>OllamaAPI: Call GenerateAsync with stream = true
    
    loop Streaming Response
        OllamaAPI-->>RagService: Return responseChunk
        RagService-->>Program: Yield return chunk
        Program-->>User: Display chunk
    end
    
    deactivate RagService
    Program-->>User: Display complete response
```

---

## Component Details

### 1. Program.cs - Entry Point

**Responsibilities**:
- Console lifecycle management
- Command parsing (exit, help, stats)
- ASCII UI rendering
- Streaming output handling

**Key Methods**:

| Method | Description |
|--------|-------------|
| Main | Application entry point initializes services and runs main loop |
| DisplayHelp | Shows available commands to user |
| DisplayStats | Retrieves and displays system statistics |

**Console Encoding**:
```csharp
Console.OutputEncoding = Encoding.ASCII;
Console.InputEncoding = Encoding.ASCII;
```

### 2. ColorHelper.cs - UI Utilities

**Responsibilities**:
- ANSI color management
- ASCII divider generation
- Table formatting
- Animated spinners (thinking and loading states)

**Key Methods**:

| Method | Output Example | Purpose |
|--------|----------------|---------|
| WriteDivider(char, int) | "----" or "====" | Section separation |
| WriteMessage(MessageType, string) | "[OK] Message" | Typed message with color |
| WriteTable(Dictionary) | Formatted table | Statistics display |
| WriteBanner(string[]) | "* Banner *" | Application header display |

**Message Types and Colors**:

```mermaid
flowchart LR
    subgraph Status["Status Messages"]
        S[Success] --> G[Green color]
        E[Error] --> R[Red color]
        W[Warning] --> Y[Yellow color]
        I[Info] --> C[Cyan color]
    end
    
    subgraph Process["Process Messages"]
        P[Processing] --> Y[Yellow color]
        T[Thinking] --> M[Magenta color]
        SR[Search] --> DY[Dark Yellow color]
        DB[Database] --> DG[Dark Gray color]
    end
    
    subgraph Output["Output Messages"]
        AI[AI Assistant] --> M[Magenta color]
        Q[Question] --> C[Cyan color]
        D[Document] --> G[Gray color]
    end
```

### 3. HashHelper.cs - File Hashing

**Responsibilities**:
- SHA256 hash computation
- File integrity verification

**Implementation**:
```csharp
public static string GetFileHash(string filePath)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException();
    
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(filePath);
    byte[] hash = sha256.ComputeHash(stream);
    return Convert.ToHexString(hash);
}
```

### 4. DatabaseService.cs - SQLite Operations

**Responsibilities**:
- Database initialization
- CRUD operations for Files and Chunks tables
- Statistics queries

**Connection String**: `Data Source = rag.db`

**Core Methods**:

| Method | SQL Operation | Purpose |
|--------|---------------|---------|
| InitializeAsync | CREATE TABLE IF NOT EXISTS | Schema setup on first run |
| FileExistsAsync(string hash) | SELECT COUNT(*) | Duplicate detection using hash |
| SaveFileAsync(string, string) | INSERT INTO Files | Track indexed files |
| SaveChunkAsync(ChunkRecord) | INSERT INTO Chunks | Store chunk with vector |
| LoadChunksAsync() | SELECT * FROM Chunks | Load all vectors for search |
| GetChunkCountAsync() | SELECT COUNT(*) | Statistics for display |
| GetDocumentCountAsync() | SELECT COUNT(*) | Statistics for display |

**Vector Storage Strategy**:
```csharp
// Embedding stored as JSON string
command.Parameters.AddWithValue("@embedding", 
    JsonSerializer.Serialize(chunk.Embedding));

// Retrieval deserialization
Embedding = JsonSerializer.Deserialize<float[]>(reader.GetString(5)) 
    ?? Array.Empty<float>();
```

### 5. DocumentIndexer.cs - Indexing Pipeline

**Responsibilities**:
- Document discovery from folder
- Change detection via hash comparison
- Embedding generation via Ollama

**Configuration**:
```csharp
private const string DocumentsFolder = @"C:\RagDocuments";
private readonly OllamaApiClient _embedder with model "nomic-embed-text"
```

**Indexing Flow**:

```mermaid
flowchart TD
    START[Start IndexAsync] --> SCAN[Scan Documents Folder]
    SCAN --> FOREACH[Loop for each file]
    
    FOREACH --> HASH[Compute SHA256 Hash]
    HASH --> EXISTS{Check exists in Database}
    
    EXISTS --> Yes[File exists] --> SKIP[Skip already indexed]
    EXISTS --> No[File not exists] --> READ[Read file content]
    
    READ --> EMBED[Generate embedding via Ollama]
    EMBED --> SAVE_CHUNK[Save to Chunks table]
    SAVE_CHUNK --> SAVE_FILE[Save to Files table]
    
    SKIP --> FOREACH
    SAVE_FILE --> FOREACH
    
    FOREACH --> Complete[No more files] --> END[Indexing complete]
```

### 6. RagService.cs - RAG Orchestration

**Responsibilities**:
- Query embedding generation
- Similarity search execution
- Prompt engineering
- Response streaming

**Core Dependencies**:
```csharp
private readonly DatabaseService _database;      // data access
private readonly DocumentIndexer _indexer;       // initialization
private readonly OllamaApiClient _llm;           // llama3:8b
private readonly OllamaApiClient _embedder;      // nomic-embed-text
```

**Query Processing Pipeline**:

```mermaid
flowchart TD
    Q[User Question] --> VALIDATE{Is question valid}
    VALIDATE --> No[Not valid] --> ERROR[Return error message]
    VALIDATE --> Yes[Valid] --> EMBED_Q[Embed question]
    
    EMBED_Q --> LOAD[Load all chunks from database]
    LOAD --> CHECK{Do chunks exist}
    CHECK --> No[No chunks] --> NO_DOCS[Return no documents error]
    
    CHECK --> Yes[Chunks exist] --> SIM[Calculate cosine similarity]
    SIM --> SORT[Sort by score descending]
    SORT --> TOP[Take top 10 results]
    
    TOP --> BUILD[Build context string from matches]
    BUILD --> PROMPT[Create prompt with context]
    PROMPT --> STREAM[Stream from llama3:8b]
    
    STREAM --> YIELD[Yield return chunk]
    YIELD --> USER[Display to user]
```

**Prompt Template**:
```
You are a helpful assistant.

Answer ONLY using the supplied context.

If the answer cannot be found in the context,
say "I could not find that information."

Context:
{context text here}

Question:
{question text here}

Answer:
```

---

## Data Flow

### Indexing Data Flow

```mermaid
flowchart LR
    subgraph Source
        F1[products.txt]
        F2[faq.txt]
        F3[manual.txt]
    end
    
    subgraph Processing
        H1[SHA256 hash: A3F2...]
        H2[SHA256 hash: B4E1...]
        H3[SHA256 hash: C5D9...]
        
        V1[Vector: 0.123, -0.456, ...]
        V2[Vector: 0.789, -0.012, ...]
        V3[Vector: -0.345, 0.678, ...]
    end
    
    subgraph Storage
        DB[SQLite Database]
        FT[Files Table: Hash, Name, Date]
        CT[Chunks Table: Hash, Content, Vector JSON]
    end
    
    F1 --> H1 --> V1 --> CT
    F2 --> H2 --> V2 --> CT
    F3 --> H3 --> V3 --> CT
    
    H1 --> FT
    H2 --> FT
    H3 --> FT
```

### Query Data Flow

```mermaid
flowchart LR
    Q[Question: What products?]
    
    subgraph Vectorization
        QV[Query Vector: 0.234, -0.567, ...]
    end
    
    subgraph Similarity
        S1[Score: 0.89 from products.txt]
        S2[Score: 0.76 from faq.txt]
        S3[Score: 0.45 from manual.txt]
    end
    
    subgraph Context
        CTX["Products A, B, C + FAQ Pricing info"]
    end
    
    subgraph Generation
        PR[Prompt + Context]
        RES[Answer: We sell A, B, C]
    end
    
    Q --> QV
    QV --> S1
    QV --> S2
    QV --> S3
    
    S1 --> CTX
    S2 --> CTX
    
    CTX --> PR --> RES
```

---

## Database Schema

### Entity Relationship Diagram

```mermaid
erDiagram
    FILES {
        INTEGER Id PK
        TEXT FileName
        TEXT FileHash UK
        TEXT IndexedAt
    }

    CHUNKS {
        INTEGER Id PK
        TEXT FileHash FK
        TEXT FileName
        INTEGER ChunkIndex
        TEXT Content
        TEXT Embedding
    }

    FILES ||--o{ CHUNKS : contains
```

### Detailed Schema

**Files Table**:
```sql
CREATE TABLE Files
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FileName TEXT NOT NULL,
    FileHash TEXT NOT NULL UNIQUE,
    IndexedAt TEXT NOT NULL
);

-- Create indexes
CREATE INDEX idx_files_hash ON Files(FileHash);
CREATE INDEX idx_files_indexed ON Files(IndexedAt);
```

**Chunks Table**:
```sql
CREATE TABLE Chunks
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FileHash TEXT NOT NULL,
    FileName TEXT NOT NULL,
    ChunkIndex INTEGER NOT NULL,
    Content TEXT NOT NULL,
    Embedding TEXT NOT NULL
);

-- Create indexes
CREATE INDEX idx_chunks_hash ON Chunks(FileHash);
CREATE INDEX idx_chunks_filename ON Chunks(FileName);
```

**Sample Data**:

Files row example:
```json
{
  "Id": 1,
  "FileName": "products.txt",
  "FileHash": "A3F2C1B8E9D4F6A2B1C3D4E5F6A7B8C9D0E1F2A3B4C5D6E7F8A9B0C1D2E3F4",
  "IndexedAt": "2026-06-03T14:30:22.123456Z"
}
```

Chunks row example:
```json
{
  "Id": 1,
  "FileHash": "A3F2C1B8E9D4F6A2B1C3D4E5F6A7B8C9D0E1F2A3B4C5D6E7F8A9B0C1D2E3F4",
  "FileName": "products.txt",
  "ChunkIndex": 0,
  "Content": "Our company sells three main products...",
  "Embedding": "[0.123, -0.456, 0.789, -0.012, ...]"  // 768 floats total
}
```

---

## Algorithms

### Cosine Similarity

**Formula**:
```
similarity(A, B) = (A·B) / (‖A‖ × ‖B‖)

Where:
A·B = Σᵢ(aᵢ × bᵢ)
‖A‖ = √Σᵢ(aᵢ²)
‖B‖ = √Σᵢ(bᵢ²)
```

**Implementation**:
```csharp
private static double CosineSimilarity(float[] a, float[] b)
{
    double dot = 0, magA = 0, magB = 0;
    
    for (int i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        magA += a[i] * a[i];
        magB += b[i] * b[i];
    }
    
    if (magA == 0 || magB == 0)
        return 0;
    
    return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
}
```

**Complexity**: O(n) where n = vector dimensions (768)

**Output Range**: [-1.0, 1.0]

| Value | Meaning |
|-------|---------|
| 1.0 | Identical vectors |
| 0.0 | Orthogonal (no relation) |
| -1.0 | Opposite directions |

### SHA256 Hashing

**Purpose**: File identity and change detection

**Characteristics**:
- 256-bit output (64 hex characters)
- Deterministic (same file → same hash)
- Collision-resistant

**Implementation**:
```csharp
using var sha256 = SHA256.Create();
using var stream = File.OpenRead(filePath);
byte[] hash = sha256.ComputeHash(stream);
return Convert.ToHexString(hash);
```

---

## API Reference

### Ollama API Integration

**Embeddings Endpoint**:
```
POST http://localhost:11434/api/embed
Content-Type: application/json

{
  "model": "nomic-embed-text",
  "input": "Your text here"
}
```

**Response**:
```json
{
  "embeddings": [
    [0.123, -0.456, 0.789, ...]
  ]
}
```

**Generate Endpoint (Streaming)**:
```
POST http://localhost:11434/api/generate
Content-Type: application/json

{
  "model": "llama3:8b",
  "prompt": "Your prompt here",
  "stream": true
}
```

**Streaming Response Format**:
```
data: {"response": "Hello", "done": false}
data: {"response": " world", "done": false}
data: {"response": "", "done": true}
```

### OllamaSharp API Usage

```csharp
// Embedding generation
var embedResponse = await _embedder.EmbedAsync(new EmbedRequest
{
    Model = "nomic-embed-text",
    Input = new List<string> { text }
});
float[] vector = embedResponse.Embeddings[0].ToArray();

// Streaming generation
await foreach (var chunk in _llm.GenerateAsync(new GenerateRequest
{
    Model = "llama3:8b",
    Prompt = prompt,
    Stream = true
}))
{
    Console.Write(chunk.Response);
}
```

---

## Configuration

### Application Configuration

| Parameter | Location | Default | Description |
|-----------|----------|---------|-------------|
| DocumentsFolder | DocumentIndexer.cs | `C:\RagDocuments` | Source document directory |
| DatabaseFile | DatabaseService.cs | `rag.db` | SQLite database file name |
| OllamaEndpoint | RagService.cs | `http://localhost:11434` | Ollama API endpoint URL |
| LLMModel | RagService.cs | `llama3:8b` | Model used for generation |
| EmbeddingModel | RagService.cs | `nomic-embed-text` | Model used for embeddings |
| MaxContextLength | RagService.cs | 7000 | Maximum characters for context |
| TopK | RagService.cs | 10 | Number of chunks retrieved |

### Modifiable Parameters

**Change Document Path**:
```csharp
// In DocumentIndexer.cs line 10
private const string DocumentsFolder = @"D:\MyDocuments";
```

**Adjust Context Window**:
```csharp
// In RagService.cs ExecuteStreamingQuery method
if (contextBuilder.Length > 5000) // change from 7000
```

**Modify Retrieval Count**:
```csharp
// In RagService.cs ExecuteStreamingQuery method
.Take(5) // instead of 10
```

**Switch Models**:
```csharp
// In RagService.cs constructor
_llm = new OllamaApiClient(uri, "mistral"); // different LLM
_embedder = new OllamaApiClient(uri, "all-minilm"); // different embeddings
```

---

## Performance Characteristics

### Time Complexities

| Operation | Complexity | Typical Time (First Run) | Typical Time (Cached) |
|-----------|------------|--------------------------|----------------------|
| File hashing | O(file size) | 10ms/MB | N/A |
| Embedding generation | O(text length × 768) | 500ms/document | 50ms/document |
| Similarity search | O(chunks × 768) | 100ms for 1000 chunks | 100ms |
| LLM generation | O(output tokens) | 2–5 seconds/response | Same as first run |

### Space Complexity

| Storage | Size Estimate | Calculation |
|---------|---------------|-------------|
| Vector storage | ~3KB per chunk | 768 floats × 4 bytes + JSON overhead |
| Text storage | ~1KB per chunk | Original text content |
| File metadata | ~200 bytes per file | Hash + name + timestamp |

**Example Calculation**: 1000 chunks ≈ 4MB (vectors) + 1MB (text) = 5MB total

### Bottlenecks

```mermaid
flowchart LR
    subgraph Bottleneck["Primary Bottleneck Location"]
        O[Ollama API performing local inference]
    end
    
    subgraph Secondary["Secondary Bottleneck Locations"]
        D[Disk I/O for SQLite reads]
        S[Similarity Search with O(n) scan]
        M[Memory usage loading all chunks]
    end
    
    O --> B[Overall Response Time]
    D --> B
    S --> B
    M --> B
```

### Optimization Opportunities

| Area | Current | Optimized | Implementation Method |
|------|---------|-----------|----------------------|
| Search | O(n) scan | O(log n) | Add SQLite vector extension |
| Memory | All chunks loaded | Streaming loads | Implement paginated loading |
| Caching | None implemented | Embedding cache | Add dictionary cache |
| Parallelism | Sequential | Parallel execution | Use Parallel.ForEach for indexing |

---

## Error Handling

### Error Types and Recovery

```mermaid
flowchart TD
    E[Error Occurred] --> TYPE{Determine Error Type}
    
    TYPE --> Ollama[Ollama Connection Error] --> O1[Log error message]
    O1 --> O2[Display: ERR Connection failed]
    O2 --> O3[Statistics show OFFLINE status]
    O3 --> O4[Continue with cached data only]
    
    TYPE --> File[File Not Found Error] --> F1[Log warning message]
    F1 --> F2[Display: WARN Skipping file]
    F2 --> F3[Continue with next file]
    
    TYPE --> Database[Database Locked Error] --> D1[Log error message]
    D1 --> D2[Display: ERR Database locked]
    D2 --> D3[Exit or retry based on user choice]
    
    TYPE --> Invalid[Invalid Question Error] --> I1[Log info message]
    I1 --> I2[Display: Please enter valid question]
    I2 --> I3[Return to prompt]
    
    TYPE --> Model[Model Not Found Error] --> M1[Log error message]
    M1 --> M2[Display: Pull model first]
    M2 --> M3[Exit application]
```

### Exception Handling Pattern

```csharp
try
{
    // Perform operation
}
catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
{
    // Handle database locked
    ColorHelper.WriteError("Database is locked. Close other connections.");
}
catch (HttpRequestException ex)
{
    // Handle Ollama connection failed
    ColorHelper.WriteError($"Cannot connect to Ollama: {ex.Message}");
}
catch (FileNotFoundException ex)
{
    // Handle document missing
    ColorHelper.WriteWarning($"File not found: {ex.FileName}");
}
catch (Exception ex)
{
    // Handle general error
    ColorHelper.WriteError($"Unexpected error: {ex.Message}");
    // In debug mode
    ColorHelper.WriteColoredMessage(ex.ToString(), ConsoleColor.DarkRed);
}
```

---

## Security Considerations

### Local-First Architecture

| Aspect | Implementation | Risk Level |
|--------|----------------|------------|
| Data transmission | Localhost only | None |
| API authentication | None for local use | Low |
| File access | Current user context | Medium |
| Database encryption | None in default setup | Low for local use |

### Recommended Hardening for Production

```csharp
// Add SQLite encryption
using var connection = new SqliteConnection(
    "Data Source=rag.db;Password=secret");
{
    connection.Open();
    connection.Execute("PRAGMA key = 'your key'");
}

// Validate file paths to prevent traversal
if (!Path.GetFullPath(file).StartsWith(DocumentsFolder))
{
    throw new SecurityException("Path traversal detected");
}

// Sanitize user input
var sanitized = question.Replace("--", "").Replace(";", "");
```

---

## Extensibility Points

### Pluggable Components

```mermaid
flowchart TB
    subgraph Current["Current Implementation"]
        C1[Whole file chunking]
        C2[Cosine similarity]
        C3[SQLite storage]
        C4[Console UI]
    end
    
    subgraph Extension["Extension Points"]
        E1[IChunkingStrategy interface]
        E2[ISimilarityStrategy interface]
        E3[IVectorStore interface]
        E4[IUIProvider interface]
    end
    
    C1 -.-> E1
    C2 -.-> E2
    C3 -.-> E3
    C4 -.-> E4
```

### Interface Definitions

**Chunking Strategy Interface**:
```csharp
public interface IChunkingStrategy
{
    IEnumerable<string> Chunk(string text, int maxChunkSize);
}
```

Example implementations:
- `SemanticChunking` - splits by paragraphs
- `FixedSizeChunking` - matches current implementation
- `SentenceChunking` - splits by sentences

**Similarity Strategy Interface**:
```csharp
public interface ISimilarityStrategy
{
    double Calculate(float[] a, float[] b);
}
```

Example implementations:
- `CosineSimilarity` - matches current implementation
- `DotProductSimilarity` - provides alternative
- `EuclideanDistance` - works for certain embeddings

**Vector Store Interface**:
```csharp
public interface IVectorStore
{
    Task SaveAsync(string id, float[] vector, string metadata);
    Task<List<(float[] Vector, string Metadata)>> SearchAsync(float[] query, int k);
}
```

Example implementations:
- `SQLiteVectorStore` - matches current implementation
- `PostgreSQLVectorStore` - for production use
- `InMemoryVectorStore` - for testing

### Adding New Document Types

```csharp
// Extend DocumentIndexer class
public async Task IndexPdfAsync(string pdfPath)
{
    var text = ExtractPdfText(pdfPath); // using PDF library
    // Rest of indexing pipeline remains same
}

// Register supported extensions
private readonly HashSet<string> _supportedExtensions = new HashSet<string>
{
    ".txt", ".json", ".pdf", ".docx"
};
```

---

## Deployment Checklist

- [ ] Confirm .NET 9 SDK installed
- [ ] Verify Ollama installed and running
- [ ] Confirm `llama3:8b` model pulled
- [ ] Verify `nomic-embed-text` model pulled
- [ ] Create `C:\RagDocuments` folder
- [ ] Add sample documents to folder
- [ ] Ensure port 11434 is available for Ollama
- [ ] Verify write permissions in application directory for SQLite

---

## Troubleshooting Matrix

| Symptom | Likely Cause | Diagnostic Command | Solution |
|---------|--------------|-------------------|----------|
| "No connection" message | Ollama not running | `curl localhost:11434` | Run `ollama serve` |
| "Model not found" error | Model not pulled | `ollama list` | Run `ollama pull <model>` |
| Slow first query | Generating embeddings | Check CPU usage | Normal behavior, wait for completion |
| "Database locked" message | Another process using SQLite | Check for open connections | Close SQLite browsers |
| "No results found" message | Empty document folder | Check `C:\RagDocuments` | Add documents to folder |
| Encoding errors displayed | Unicode in console output | Check `Console.OutputEncoding` | Set to ASCII or Unicode as needed |

---

## Version History

| Version | Date | Changes Description |
|---------|------|---------------------|
| 1.0 | 2026-06-03 | Initial release with SQLite storage, ASCII UI, and streaming responses |

---

## Appendix A: Ollama Model Specifications

**llama3:8b Model**
- Parameters: 8 billion
- Context length: 8192 tokens
- Training: Meta Llama 3
- Use case: Text generation

**nomic-embed-text Model**
- Parameters: 137 million
- Output dimension: 768
- Training: Nomic AI
- Use case: Text embeddings

---

## Appendix B: Glossary

| Term | Definition |
|------|------------|
| RAG | Retrieval-Augmented Generation (combining search with LLMs) |
| Embedding | Numerical vector representation of text |
| Cosine Similarity | Measure of angle between two vectors |
| Chunk | Segment of a document prepared for embedding |
| Context Window | Maximum tokens an LLM can process |
| Vector Store | Database optimized for vector similarity search |
| SHA256 | Cryptographic hash function producing 256-bit output |

---

## Appendix C: Useful SQL Queries

```sql
-- View indexed files
SELECT FileName, IndexedAt FROM Files ORDER BY IndexedAt DESC;

-- Count chunks per file
SELECT FileName, COUNT(*) as ChunkCount 
FROM Chunks 
GROUP BY FileName 
ORDER BY ChunkCount DESC;

-- Find recently added documents (last 7 days)
SELECT FileName, IndexedAt 
FROM Files 
WHERE IndexedAt > datetime('now', '-7 days');

-- Check database size
SELECT page_count * page_size as Size 
FROM pragma_page_count(), pragma_page_size();
```

---

## Appendix D: Mathematical Notation Reference

| Symbol | Meaning | Example |
|--------|---------|---------|
| **q** | Query vector | **q** ∈ ℝ⁷⁶⁸ |
| **d** | Document vector | **d** ∈ ℝ⁷⁶⁸ |
| · | Dot product | **q**·**d** = Σᵢ qᵢdᵢ |
| ‖**v**‖ | Euclidean norm | ‖**v**‖ = √Σᵢ vᵢ² |
| cos φ | Cosine similarity | cos φ = (**q**·**d**)/(‖**q**‖·‖**d**‖) |
| φ | Angle between vectors | φ ∈ [0, π/2] for similarity |
| ≈ | Approximately equal | Context window ≈ 7000 chars |
| O(n) | Big O notation | Similarity search = O(n·d) |
| ⊥ | Orthogonal (no relation) | cos φ = 0 ⇒ vectors ⊥ |
