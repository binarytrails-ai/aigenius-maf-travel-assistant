import React from "react";
import { FlightCard, type Flight } from "./FlightCard";

interface FlightResultsProps {
  status: "executing" | "complete" | "failed";
  result?: string;
  args?: {
    origin?: string;
    destination?: string;
    maxBudget?: number;
    userPreferences?: string;
  };
}

interface FlightSearchResponse {
  success?: boolean;
  searchCriteria?: {
    origin: string;
    destination: string;
    travelDate?: string;
    maxBudget?: string;
    userPreferences?: string;
    semanticSearchEnabled?: boolean;
  };
  totalResults?: number;
  flights?: Flight[];
  error?: string;
}

export const FlightResults: React.FC<FlightResultsProps> = ({
  status,
  result,
  args,
}) => {
  // Parse the JSON result
  const parsedResult: FlightSearchResponse = result
    ? (() => {
        try {
          return JSON.parse(result);
        } catch {
          return { error: "Failed to parse flight results" };
        }
      })()
    : {};

  const flights = parsedResult.flights || [];

  // Loading state
  if (status === "executing") {
    return (
      <div className="w-full bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="p-6 border-b border-gray-100 bg-gray-50">
          <div className="flex items-center gap-3">
            <div className="animate-spin rounded-full h-5 w-5 border-2 border-gray-300 border-t-gray-700"></div>
            <h3 className="text-lg font-semibold text-gray-900">
              Searching flights...
            </h3>
          </div>
          {args && (
            <p className="text-sm text-gray-600 mt-2">
              {args.origin} → {args.destination}
              {args.maxBudget && ` • Max budget: $${args.maxBudget}`}
            </p>
          )}
        </div>
        <div className="p-6 space-y-4">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className="animate-pulse bg-gray-100 rounded-xl h-48"
            ></div>
          ))}
        </div>
      </div>
    );
  }

  // Error state
  if (status === "failed" || parsedResult.error) {
    return (
      <div className="w-full bg-white rounded-xl border border-red-200 overflow-hidden">
        <div className="p-6 bg-red-50">
          <div className="flex items-start gap-3">
            <svg
              className="w-6 h-6 text-red-600 flex-shrink-0 mt-0.5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <div>
              <h3 className="text-lg font-semibold text-red-900">
                Flight search failed
              </h3>
              <p className="text-sm text-red-700 mt-1">
                {parsedResult.error || "An unexpected error occurred"}
              </p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Empty results state
  if (flights.length === 0) {
    return (
      <div className="w-full bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="p-8 text-center">
          <svg
            className="w-16 h-16 text-gray-400 mx-auto mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M12 21a9 9 0 100-18 9 9 0 000 18z"
            />
          </svg>
          <h3 className="text-lg font-semibold text-gray-900 mb-2">
            No flights found
          </h3>
          <p className="text-sm text-gray-600">
            Try adjusting your search criteria or budget
          </p>
        </div>
      </div>
    );
  }

  // Success state with results
  return (
    <div className="w-full space-y-4">
      {/* Header */}
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <div className="flex items-start justify-between">
          <div>
            <h3 className="text-xl font-bold text-gray-900 mb-2">
              Flight Options
            </h3>
            {parsedResult.searchCriteria && (
              <div className="flex flex-wrap items-center gap-2 text-sm text-gray-600">
                <span className="font-medium">
                  {parsedResult.searchCriteria.origin} →{" "}
                  {parsedResult.searchCriteria.destination}
                </span>
                {parsedResult.searchCriteria.travelDate &&
                  parsedResult.searchCriteria.travelDate !==
                    "Not specified" && (
                    <>
                      <span className="text-gray-400">•</span>
                      <span>{parsedResult.searchCriteria.travelDate}</span>
                    </>
                  )}
                {parsedResult.searchCriteria.maxBudget && (
                  <>
                    <span className="text-gray-400">•</span>
                    <span>Max: {parsedResult.searchCriteria.maxBudget}</span>
                  </>
                )}
              </div>
            )}
          </div>
          <div className="text-right">
            <p className="text-3xl font-bold text-gray-900">
              {parsedResult.totalResults || flights.length}
            </p>
            <p className="text-sm text-gray-600">
              {flights.length === 1 ? "flight" : "flights"} found
            </p>
          </div>
        </div>
      </div>

      {/* Flight Cards Grid */}
      <div className="grid grid-cols-1 gap-4">
        {flights.map((flight) => (
          <FlightCard key={flight.flightNumber} flight={flight} />
        ))}
      </div>
    </div>
  );
};
