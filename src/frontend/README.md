# Contoso Travel Agency

- Framework: Next.js (App Router) + TypeScript
- Integration: CopilotKit Runtime → AG-UI agent (backend)

## Run Locally

Prerequisites: Node.js 20+, backend agent running at http://localhost:5001 (see repository root README for backend steps and GitHub Models setup).

1. Install dependencies
```bash
npm install
```

2. Configure backend URL (optional if using the default)
```bash
# Create .env.local with backend base URL
# Windows PowerShell
"$env:BACKEND_AGENT_BASE_URL=http://localhost:5001" | Out-File -Encoding utf8 .env.local
# macOS/Linux
# echo "BACKEND_AGENT_BASE_URL=http://localhost:5001" > .env.local
```

3. Start the dev server
```bash
npm run dev
```

Open http://localhost:3000 in your browser.

Notes
- The API route is implemented at app/api/copilotkit/route.ts and forwards to the AG-UI agent endpoint exposed by the backend.
- Ensure the backend `GITHUB_TOKEN` is set so the agent can call GitHub Models.
