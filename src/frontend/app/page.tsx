"use client";

import React, { useState } from "react";
import { z } from "zod";
import { CopilotKit } from "@copilotkit/react-core"; // CopilotKit provider is shared between V1 and V2
import {
  CopilotChat,
  useAgentContext,
  useHumanInTheLoop,
  useFrontendTool,
  ToolCallStatus,
} from "@copilotkit/react-core/v2";
import { FlightResults } from "../src/components/FlightResults";

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
  status: ToolCallStatus;
}) => {
  const [isResponding, setIsResponding] = useState(false);

  // Only show when executing and respond function is available
  if (status !== ToolCallStatus.Executing || !respond) return null;

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
        className="bg-white rounded-xl border border-gray-200 shadow-xl p-8 max-w-md w-full animate-fadeIn"
      >
        {/* Header */}
        <div className="flex items-start gap-3 mb-6">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-gray-100 shrink-0">
            <svg
              className="w-5 h-5 text-gray-700"
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
            className="px-4 py-2 rounded-lg text-base font-semibold text-white bg-gray-700 hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-gray-500 disabled:opacity-50 disabled:cursor-not-allowed transition"
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
  useAgentContext({
    description: "Contoso Travel Agency customer",
    value: "User planning travel",
  });

  useFrontendTool({
    name: "display_flight_results",
    description: "Display flight search results to the user",
    parameters: z.object({
      flights: z
        .array(
          z.object({
            flightNumber: z.string().describe("Flight number"),
            airline: z.string().describe("Airline name"),
            price: z.number().describe("Price in AUD"),
            departureTime: z.string().describe("Departure time"),
            arrivalTime: z.string().describe("Arrival time"),
            origin: z.string().optional().describe("Origin airport/city"),
            destination: z
              .string()
              .optional()
              .describe("Destination airport/city"),
          }),
        )
        .describe("Array of flight results to display"),
      travelDate: z.string().optional().describe("Travel date for the flights"),
      origin: z.string().optional().describe("Search origin"),
      destination: z.string().optional().describe("Search destination"),
      maxBudget: z.number().optional().describe("Maximum budget filter"),
      userPreferences: z
        .string()
        .optional()
        .describe("User preferences used in search"),
    }),
    handler: async (params) => {
      // Convert flights array to the JSON string format expected by FlightResults
      const response = {
        success: true,
        searchCriteria: {
          travelDate: params.travelDate,
          origin: params.origin || "",
          destination: params.destination || "",
          maxBudget: params.maxBudget?.toString(),
          userPreferences: params.userPreferences,
        },
        totalResults: params.flights.length,
        flights: params.flights,
      };
      return JSON.stringify(response);
    },
    render: ({ status, args, result }) => {
      // Map CopilotKit status to FlightResults status
      const flightStatus =
        status === "inProgress" || status === "executing"
          ? "executing"
          : status === "complete"
            ? "complete"
            : "failed";

      return (
        <FlightResults
          status={flightStatus}
          result={result}
          args={{
            origin: args.origin,
            destination: args.destination,
            maxBudget: args.maxBudget,
            userPreferences: args.userPreferences,
          }}
        />
      );
    },
  });

  /**
   *  Human-in-the-loop approval tool
   * Based on: https://docs.copilotkit.ai/reference/v2/hooks/useHumanInTheLoop
   */
  useHumanInTheLoop({
    name: "request_approval", // This should match the tool name in the backend that requires approval
    description: "Request user approval before executing sensitive operations",
    parameters: z.object({
      request: z
        .string()
        .describe("The approval request containing function details"),
    }),
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
    <div className="min-h-screen w-full bg-gray-50">
      {/* Hero Section */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-6 py-5">
          <div className="text-center">
            <h1 className="text-3xl md:text-4xl font-bold mb-2 text-gray-900 animate-fadeIn tracking-tight">
              Contoso Travel Assistant
            </h1>
            <p className="text-base md:text-lg text-gray-600 max-w-2xl mx-auto">
              Your AI-powered travel planning companion
            </p>
          </div>
        </div>
      </div>

      {/* Main Content - Expanded Full Width Chat */}
      <div className="w-full px-3 py-6">
        <div className="max-w-[1920px] mx-auto">
          <CopilotChat
            className="h-[calc(100vh-200px)] custom-chat-messages"
            labels={{
              chatInputPlaceholder:
                "Ask about destinations, flights, or travel plans...",
            }}
            welcomeScreen={({ input, suggestionView }) => (
              <div className="flex flex-col items-center justify-center h-full bg-gradient-to-b from-gray-50 to-white px-6 py-12">
                {/* Hero Content */}
                <div className="max-w-2xl text-center space-y-6 mb-8">
                  {/* Icon */}
                  <div className="inline-flex items-center justify-center w-20 h-20 rounded-full bg-gray-100 mb-4">
                    <svg
                      className="w-10 h-10 text-gray-700"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>

                  {/* Title */}
                  {/* <h2 className="text-3xl md:text-4xl font-bold text-gray-900">
                      Welcome to Contoso Travel Agency
                    </h2> */}

                  {/* Description */}
                  {/* <p className="text-lg text-gray-600 leading-relaxed">
                      I&apos;m your AI travel companion. Tell me about your dream
                      destination and I&apos;ll help you find the perfect flights,
                      create personalized itineraries, and make your travel
                      planning effortless.
                    </p> */}

                  {/* Suggestions */}
                  <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
                    <p className="text-sm font-semibold text-gray-900 mb-3">
                      Try asking:
                    </p>
                    <div className="space-y-2.5 text-left">
                      <div className="flex items-start gap-2.5 text-gray-700">
                        <span className="text-gray-400 mt-1 text-sm">•</span>
                        <span>Can you help me plan a trip?</span>
                      </div>
                      <div className="flex items-start gap-2.5 text-gray-700">
                        <span className="text-gray-400 mt-1 text-sm">•</span>
                        <span>Find flights to Wellington next month</span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Suggestions from agent (if any) */}
                {suggestionView}

                {/* Input */}
                <div className="w-full max-w-2xl">{input}</div>
              </div>
            )}
          />
        </div>
      </div>

      {/* Floating New Chat Button */}
      <button
        onClick={handleNewChat}
        className="fixed bottom-6 right-6 px-5 py-3 bg-gray-800 hover:bg-gray-900 text-white rounded-full shadow-lg hover:shadow-xl transition-all duration-200 flex items-center gap-2 z-40 animate-fadeIn hover:scale-105 group"
        aria-label="Start new chat"
      >
        <svg
          className="w-5 h-5 group-hover:rotate-90 transition-transform duration-200"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M12 4v16m8-8H4"
          />
        </svg>
        <span className="font-semibold text-sm">New Chat</span>
      </button>
    </div>
  );
};
