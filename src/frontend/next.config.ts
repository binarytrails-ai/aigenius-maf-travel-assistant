import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  serverExternalPackages: ["pino", "pino-pretty", "thread-stream"],
  // env: {
  //   BACKEND_AGENT_BASE_URL: process.env.BACKEND_AGENT_BASE_URL,
  // },
};

export default nextConfig;
