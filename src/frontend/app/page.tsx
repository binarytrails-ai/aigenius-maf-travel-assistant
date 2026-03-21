"use client";

import React, { useState } from "react";
import "@copilotkit/react-ui/styles.css";
import {
  CopilotKit,
  useCopilotReadable,
  useHumanInTheLoop,
  useRenderToolCall,
} from "@copilotkit/react-core";
import { CopilotChat } from "@copilotkit/react-ui";

export default function Page() {
  const [chatKey, setChatKey] = React.useState(0);

  return (
    <CopilotKit
      key={chatKey}
      runtimeUrl="/api/copilotkit"
      showDevConsole={true}
      agent="contoso_agent"
    >
      <Chat chatKey={chatKey} setChatKey={setChatKey} />
    </CopilotKit>
  );
}

const ApprovalUI = ({
  args,
  respond,
  status,
}: {
  args: {
    approvalId?: string;
    toolName?: string;
  };
  respond?: (value: unknown) => void;
  status: string;
}) => {
  const [isResponding, setIsResponding] = useState(false);

  // Only show when executing and respond function is available
  if (status !== "executing" || !respond) return null;

  const handleRespond = async (approved: boolean) => {
    setIsResponding(true);
    // Return response object with approval_id and approved fields
    respond({
      approval_id: args.approvalId,
      approved: approved,
    });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    // Prevent keyboard shortcuts if already responding
    if (isResponding) return;

    if (e.key === "Escape") {
      e.preventDefault();
      handleRespond(false);
    } else if (e.key === "Enter") {
      e.preventDefault();
      handleRespond(true);
    }
  };

  return (
    <div
      className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4"
      role="dialog"
      aria-labelledby="approval-title"
      aria-describedby="approval-message"
      onKeyDown={handleKeyDown}
      tabIndex={-1}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="approval-title"
        aria-describedby="approval-message"
        className="bg-white rounded-2xl border border-gray-200 shadow-xl p-8 max-w-md w-full animate-fadeIn"
      >
        {/* Header */}
        <div className="flex items-start gap-3 mb-6">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-blue-50 shrink-0">
            <svg
              className="w-5 h-5 text-blue-600"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M12 9v3m0 4h.01M12 3a9 9 0 100 18 9 9 0 000-18z"
              />
            </svg>
          </div>

          <h3
            id="approval-title"
            className="text-xl font-semibold text-gray-900 mt-1"
          >
            Tool permission required
          </h3>
        </div>

        {/* Message */}
        <div
          id="approval-message"
          className="text-base text-gray-600 leading-relaxed text-center space-y-2 mb-8"
        >
          <p>
            <span className="font-medium text-gray-800">
              {args.toolName || "This tool"}
            </span>{" "}
            wants permission to access your data and perform actions on your
            behalf.
          </p>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={() => handleRespond(false)}
            disabled={isResponding}
            className="px-4 py-2 rounded-lg text-base font-medium text-gray-700 border border-gray-300 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-gray-300 disabled:opacity-50 disabled:cursor-not-allowed transition"
          >
            Deny
          </button>

          <button
            type="button"
            onClick={() => handleRespond(true)}
            disabled={isResponding}
            className="px-4 py-2 rounded-lg text-base font-semibold text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition"
          >
            {isResponding ? "Allowing…" : "Allow"}
          </button>
        </div>
      </div>
    </div>
  );
};

const Chat = ({
  setChatKey,
}: {
  chatKey: number;
  setChatKey: (fn: (prev: number) => number) => void;
}) => {
  /**
   * Provide readable context to the agent
   */
  useCopilotReadable({
    description: "Contoso Travel Agency customer",
    value: "User planning travel",
  });

  /**
   * Render all tool execution states in real-time
   * This shows when any tool is being called and displays their arguments
   * The "*" catches all tool calls from the backend
   */
  useRenderToolCall({
    name: "*", // Catch all tool calls
    render: ({ args, status }) => {
      console.log("[Tool Render] Status:", status, "Args:", args);
      
      // Only render during tool execution
      if (status === "inProgress") {
        return (
          <div className="p-4 mb-4 bg-blue-50 border border-blue-200 rounded-lg">
            <div className="flex items-center gap-2">
              <div className="animate-spin h-4 w-4 border-2 border-blue-600 border-t-transparent rounded-full"></div>
              <span className="text-sm font-medium text-blue-900">
                Executing tool...
              </span>
            </div>
            {args && Object.keys(args).length > 0 && (
              <pre className="mt-2 text-xs text-blue-700 overflow-auto max-h-32">
                {JSON.stringify(args, null, 2)}
              </pre>
            )}
          </div>
        );
      }
      // Must return a ReactElement, not null
      return <></>;
    },
  });

  /**
   *  Human-in-the-loop approval tool
   * Based on: https://docs.copilotkit.ai/reference/hooks/useHumanInTheLoop
   */
  useHumanInTheLoop({
    name: "request_approval", // This should match the tool name in the backend that requires approval
    parameters: [
      {
        name: "request",
        type: "string",
        description: "The approval request containing function details",
        required: true,
      },
    ],
    render: ({ args, respond, status }) => {
      // Parse the approval request from the wrapper
      let approvalData: {
        approvalId?: string;
        toolName?: string;
      } = {};

      if (args.request) {
        try {
          const parsed =
            typeof args.request === "string"
              ? JSON.parse(args.request)
              : args.request;
          console.log("Parsed approval request:", parsed);

          // Handle both snake_case (from backend) and PascalCase (legacy)
          const functionName = parsed.function_name || parsed.FunctionName;
          const approvalId = parsed.approval_id || parsed.ApprovalId;

          console.log("Function name:", functionName);
          console.log("Approval ID:", approvalId);

          approvalData = {
            toolName: functionName,
            approvalId: approvalId,
          };
          console.log("Extracted approval data:", approvalData);
        } catch (e) {
          console.error(
            "Failed to parse approval request:",
            e,
            "Raw args:",
            args,
          );
        }
      } else {
        console.warn("No request property in args:", args);
      }

      return (
        <ApprovalUI args={approvalData} respond={respond} status={status} />
      );
    },
  });

  const handleNewChat = () => {
    // Clear chat history by forcing remount
    setChatKey((prev) => prev + 1);
  };

  return (
    <div className="min-h-screen w-full bg-[#f7f9fc]">
      {/* Hero Section */}
      <div className="bg-white border-b-2 border-gray-200">
        <div className="max-w-7xl mx-auto px-6 py-4 text-center">
          <h1 className="text-3xl md:text-4xl font-bold mb-1 text-gray-900 animate-fadeIn tracking-tight">
            Contoso Travel Assistant
          </h1>
          <p className="text-base md:text-lg text-gray-600 max-w-2xl mx-auto font-medium">
            Your AI-powered travel planning companion
          </p>
        </div>
      </div>

      {/* Main Content - Centered Chat */}
      <div className="max-w-6xl mx-auto px-6 py-4">
        <div className="bg-white border border-gray-200 shadow-sm overflow-hidden animate-fadeIn">
          {/* Header with New Chat Button */}
          <div className="flex items-center justify-end px-6 py-4 border-b border-gray-200 bg-gray-50">
            <button
              onClick={handleNewChat}
              className="px-4 py-2 bg-purple-600 hover:bg-purple-700 text-white font-medium text-sm transition-colors duration-200 flex items-center gap-2 shadow-sm rounded"
            >
              <span>New Chat</span>
            </button>
          </div>

          {/* Chat Area */}
          <div className="h-[calc(100vh-280px)] max-h-[650px]">
            <CopilotChat
              className="h-full"
              labels={{
                title: "Contoso Travel Assistant",
                initial:
                  "Welcome to Contoso Travel Agency!\n\n" +
                  "I'm your AI travel companion at Contoso Travel Agency. Tell me about your dream destination and I'll help you find the perfect flights, create personalized itineraries, and make your travel planning effortless.\n\n" +
                  "**Try asking:**\n" +
                  "• Help me plan a trip to New Zealand under $4000\n" +
                  "• Find flights to Wellington next month.\n",
                placeholder:
                  "Ask about destinations, flights, or travel plans...",
              }}
            />
          </div>
        </div>
      </div>
    </div>
  );
};
