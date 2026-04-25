import React from "react";

export interface Flight {
  flightNumber: string;
  airline: string;
  price: number;
  departureTime: string;
  arrivalTime: string;
  origin?: string;
  destination?: string;
  duration?: string;
  stops?: number;
  similarityScore?: number;
}

interface FlightCardProps {
  flight: Flight;
}

export const FlightCard: React.FC<FlightCardProps> = ({ flight }) => {

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat("en-AU", {
      style: "currency",
      currency: "AUD",
      minimumFractionDigits: 0,
    }).format(price);
  };

  return (
    <div className="relative bg-white rounded-xl border border-gray-200 transition-all duration-200 hover:shadow-lg hover:border-gray-300">
      <div className="p-6">
        {/* Airline Header */}
        <div className="mb-4">
          <h3 className="font-semibold text-gray-900 text-lg">
            {flight.airline}
          </h3>
          <p className="text-sm text-gray-500">{flight.flightNumber}</p>
        </div>

        {/* Route Information */}
        <div className="mb-4">
          <div className="flex items-center justify-between gap-4">
            <div className="flex-1">
              <p className="text-2xl font-bold text-gray-900">
                {flight.departureTime}
              </p>
              <p className="text-sm text-gray-600 mt-1">
                {flight.origin || "Departure"}
              </p>
            </div>

            <div className="flex-shrink-0 flex flex-col items-center px-4">
              <div className="flex items-center gap-2 mb-1">
                <div className="w-12 border-t-2 border-gray-300"></div>
                <svg
                  className="w-4 h-4 text-gray-400"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M14 5l7 7m0 0l-7 7m7-7H3"
                  />
                </svg>
              </div>
              <p className="text-xs text-gray-500 whitespace-nowrap">
                {flight.duration || ""}
              </p>
              {flight.stops !== undefined && (
                <p className="text-xs text-gray-500 mt-1">
                  {flight.stops === 0 ? "Direct" : `${flight.stops} stop(s)`}
                </p>
              )}
            </div>

            <div className="flex-1 text-right">
              <p className="text-2xl font-bold text-gray-900">
                {flight.arrivalTime}
              </p>
              <p className="text-sm text-gray-600 mt-1">
                {flight.destination || "Arrival"}
              </p>
            </div>
          </div>
        </div>

        {/* Price */}
        <div className="pt-4 border-t border-gray-100">
          <p className="text-3xl font-bold text-gray-900">
            {formatPrice(flight.price)}
          </p>
        </div>
      </div>
    </div>
  );
};
