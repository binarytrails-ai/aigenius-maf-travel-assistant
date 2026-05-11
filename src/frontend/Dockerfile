# Build stage
FROM cgr.dev/chainguard/node:latest-dev AS build
WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage
FROM cgr.dev/chainguard/node:latest AS runtime
WORKDIR /app

# Copy standalone output
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public

EXPOSE 80

ENV NODE_ENV=production
ENV PORT=80
ENV HOSTNAME=0.0.0.0

CMD ["node", "server.js"]
