# Getting Started

Welcome to the AI Agent Builder Workshop!

In this hands-on workshop, you'll build **Travel Assistant**, an AI-powered agent that assists users plan trips, search flights, and provide personalized recommendations.

![Cover](../media/cover.png)

## Foundation Labs

If you're new to the Microsoft Agent Framework, we recommend starting with the foundation labs in `labs/00-foundations/`.

These standalone labs introduce core concepts through hands-on examples. The code in these labs is separate from the main travel assistant codebase, so you can experiment freely.

1. [**Basic Agent**](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/blob/main/labs/00-foundations/lab01-basic-agent/README.md) - Create your first agent with multi-turn conversations
2. [**Context Provider**](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/blob/main/labs/00-foundations/lab02-context/README.md) - Add dynamic context to agent responses
3. [**RAG (Retrieval Augmented Generation)**](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/blob/main/labs/00-foundations/lab03-rag/README.md) - Implement semantic search over documents
4. [**Long-Term Memory**](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/blob/main/labs/00-foundations/lab04-longterm-memory/README.md) - Persist user preferences across sessions
5. [**Tools and Function Calling**](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/blob/main/labs/00-foundations/lab05-tools/README.md) - Enable agents to perform actions and call external APIs
6. [**Middleware**](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/blob/main/labs/00-foundations/lab06-middleware/README.md) - Add PII filtering and cross-cutting concerns
7. [**Skills**](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/blob/main/labs/00-foundations/lab07-skills/README.md) - Use file-based skills for progressive disclosure of capabilities
8. [**Hosting**](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/blob/main/labs/00-foundations/lab08-host/README.md) - Expose agents as web services with OpenAI-compatible endpoints

### Travel Assistant

The codebase for the workshop is organized into two main parts: the backend (.NET) and the frontend (React).

```text
src/
├── backend/    # .NET backend code
└── frontend/   # React frontend code
```

## Technologies You'll Use

- **Microsoft Agent Framework** - SDK for building intelligent, context-aware agents with built-in support for orchestration, memory management, and tool integration.

- **Azure AI Foundry** - Used for accessing Azure OpenAI models for inference and generating embeddings.
- **Azure Cosmos DB** - NoSQL database service used for storing agent memory and application data.
- **.NET/C#** - Backend development of the agent's logic and API.
- **OpenTelemetry** - A standard for observability, used for tracing and monitoring the agent's execution.
- **React** - Frontend development for the user interface.

---

## Let's Get Started

Head over to the [Environment Setup](./00-setup_instructions.md) page for instructions on setting up your workshop environment.

Happy coding!
