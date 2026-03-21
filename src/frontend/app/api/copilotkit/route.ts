import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";
import { NextRequest } from "next/server";

// 1. You can use any service adapter here for multi-agent support. We use
//    the empty adapter since we're only using one agent.
const serviceAdapter = new ExperimentalEmptyAdapter();

// 2. Create the CopilotRuntime instance and utilize the Microsoft Agent Framework
//    AG-UI integration to setup the connection.
const backendUrl = process.env.BACKEND_AGENT_BASE_URL || "http://localhost:5001";
console.log('[CopilotKit] Backend URL:', backendUrl);

const runtime = new CopilotRuntime({
  agents: {
    // The HttpAgent will receive messages and threadId from CopilotKit runtime
    // CopilotKit automatically maintains the conversation history and passes it
    // to the agent through the RunAgentInput which includes all messages
    contoso_agent: new HttpAgent({ 
      url: `${backendUrl}/agent/contoso_travel_bot`,
      debug: true // Enable detailed AG-UI protocol logs
    }),
  },
});

// 3. Build a Next.js API route that handles the CopilotKit runtime requests.
export const POST = async (req: NextRequest) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter,
    endpoint: "/api/copilotkit",
  });

  return handleRequest(req);
};

export const GET = async (req: NextRequest) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter,
    endpoint: "/api/copilotkit",
  });

  return handleRequest(req);
};
