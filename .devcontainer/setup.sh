#!/bin/bash
set -e

echo "Setting up development environment..."

# Install dotnet interactive tools
echo "Installing dotnet-interactive..."
dotnet tool install -g Microsoft.dotnet-interactive 2>&1 || echo "dotnet-interactive already installed"

echo "Installing Jupyter kernel..."
dotnet interactive jupyter install 2>&1 || echo "Jupyter kernel already installed"

# Set up HTTPS development certificates
echo "Setting up HTTPS development certificates..."
dotnet dev-certs https 2>&1 || echo "Certificate setup completed with warnings"

echo "Development environment setup complete!"
