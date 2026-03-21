# Lab 03: Travel Assistant with RAG

In this lab, you will enhance your travel assistant with **Retrieval Augmented Generation (RAG)** capabilities using the `TextSearchProvider`.

You will implement semantic search over visa policy documents, allowing your agent to answer questions based on up-to-date policy information.

By the end of this lab, you will:

- ✅ Understand how to implement RAG with the Microsoft Agent Framework
- ✅ Use `TextSearchProvider` to search documents before generating responses
- ✅ Implement document chunking and embedding generation
- ✅ Build a travel assistant that answers visa questions with accurate citations

## Key Implementation Details

### What is RAG?

**Retrieval Augmented Generation (RAG)** is a pattern that improves AI answers by first *retrieving* relevant information from your own documents, then *augmenting* the model's prompt with that information before it generates a response.

Without RAG, the model can only answer from what it learned during training — which may be outdated or simply not include your private data. With RAG, your agent can answer accurately from documents you control, and cite exactly where the answer came from.

The flow in this lab looks like this:

1. At startup, visa policy documents are split into overlapping chunks and **converted to embeddings** (numeric vectors that capture meaning).
2. When the user asks a question, the question is also converted to an embedding.
3. The most **semantically similar** chunks are retrieved using cosine similarity.
4. Those chunks are injected into the agent's context via a `TextSearchProvider` **before the model is called**.
5. The model generates an answer grounded in the retrieved policy text.

### Two Clients: Chat + Embeddings

Unlike the previous labs, this lab creates **two clients** from the same provider:

- **`IChatClient`** — used by the agent to generate conversational responses (same as before)
- **`IEmbeddingGenerator<string, Embedding<float>>`** — used to convert text into vectors for semantic search

```csharp
var azureClient = new AzureOpenAIClient(...);
var chatClient = azureClient.GetChatClient(modelName).AsIChatClient()...;
var embeddingGenerator = azureClient.GetEmbeddingClient(embeddingModelName).AsIEmbeddingGenerator();
```

The embedding model (e.g. `text-embedding-ada-002`) is separate from the chat model (e.g. `gpt-4o`). Its only job is to turn text into a list of numbers that captures the meaning of that text.

### Document Chunking

Large documents cannot be passed to a model in one go — there are token limits, and burying the model in irrelevant text impacts quality. The `UploadDocumentationFromFileAsync` method splits each file into **overlapping chunks**:

- **Chunk size** — 2000 characters per chunk
- **Overlap** — 200 characters shared between adjacent chunks

The overlap prevents important information from being cut off at a chunk boundary. Each chunk becomes a `TextSearchDocument` with a source name and link for citation.

### The In-Memory Vector Store (`TextSearchStore`)

For this lab, all document chunks and their embeddings are stored in a plain `List<TextSearchDocument>` in memory. When a search query arrives:

1. The query text is embedded using the same model used for the documents.
2. **Cosine similarity** is calculated between the query embedding and every stored document embedding.
3. The top 5 most similar chunks are returned.

Cosine similarity measures the *angle* between two vectors — a score of 1.0 means identical meaning, 0.0 means completely unrelated.

> In production you would replace this with a dedicated vector database such as Azure AI Search or Azure Cosmos DB, which can scale to millions of documents and search in milliseconds.

### Citations

Each `TextSearchResult` carries a `SourceName` and `SourceLink`. The agent's instructions tell it to cite sources when available, so answers reference the original policy document — making responses trustworthy and auditable.

---

## Instructions

### Step 1: Navigate to the Lab Folder

```bash
cd labs/00-foundations/lab03-rag
```

### Step 2: Run the Program

With .NET 10's file-based apps, you can run the single .cs file directly:

```bash
dotnet run Program.cs
```

Or in Visual Studio Code, open Program.cs and click the **"Run"** button that appears above the code.

### Step 3: Observe the Output

You should see the agent answer visa-related questions by searching through the policy documents and providing accurate responses with citations. Notice how:

- The agent retrieves relevant information from visa policy documents
- Responses include specific policy details and source references

---

## Challenges and Next Steps

**Add a new policy document**
- Create a new file in the `data/` folder (e.g. `visa-policy-uk.md`) with some made-up policy content.
- Add a call to `UploadDocumentationFromFileAsync` for the new file and test questions about it.

**Ask multi-turn follow-up questions**
- After getting an answer about Japan, ask: `"What documents do I need to bring?"` or `"How long can I stay?"`.
- Notice how the `RecentMessageMemoryLimit` keeps the search contextually relevant even for short follow-up messages.

**Try a question with no matching document**
- Ask: `"Do I need a visa to visit Brazil?"` — a country not in the policy files.
- Observe how the agent responds when retrieval finds nothing relevant. Does it admit uncertainty or guess? How could you improve this to avoid providing incorrect information?

**Change the chunk size**
- In `UploadDocumentationFromFileAsync`, try a smaller chunk size (e.g. `500`) and a larger one (e.g. `5000`).
- Smaller chunks are more precise but may miss broader context. Larger chunks give more context but may include irrelevant text. Test which gives better answers.

**Swap the vector store for Azure AI Search**
- The `SearchAdapter` `Func` is the only thing that needs to change to use a real vector database.
- Look into `Azure.Search.Documents` and consider how you would replace `textSearchStore.SearchAsync(...)` with a call to an Azure AI Search index.
