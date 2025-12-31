# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source files
COPY . .

# Build the application
RUN npm run build

# Runtime stage - serve with a simple HTTP server
FROM node:20-alpine AS runtime
WORKDIR /app

# Install a simple HTTP server
RUN npm install -g serve

# Copy built files from build stage
COPY --from=build /app/build ./build

EXPOSE 3000

CMD ["serve", "-s", "build", "-l", "3000"]
