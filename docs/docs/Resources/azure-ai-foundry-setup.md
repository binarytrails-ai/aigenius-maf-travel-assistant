# Setting Up Azure AI Foundry

In this guide, you will set up [Azure AI Foundry](https://learn.microsoft.com/azure/ai-foundry/) resources to interact with Large Language Models (LLMs).

Azure AI Foundry is a managed service that simplifies the development and deployment of AI applications by providing tools for model management, evaluation, and monitoring.

---

## Prerequisites ✅

1. **Azure Subscription**: Sign up for a [free Azure account](https://azure.microsoft.com/free/) if you don't have one.
2. **Azure Developer CLI (azd)**: The Azure Developer CLI will be used to provision and deploy resources. It should already be installed in your development environment (GitHub Codespaces or Dev Container).

    !!! Tip "Verify Azure Developer CLI"
        Run `azd version` in your terminal to verify the installation.

---

## Deploying Azure Resources 🚀

### 1. Authenticate with Azure

First, authenticate with your Azure account using the Azure Developer CLI:

```powershell
azd auth login --use-device-code
```

Follow the prompts to complete the authentication process in your browser.

### 2. Create and Configure Environment

Create a new environment for your Azure resources:

```powershell
azd env new dev
azd env select dev
azd env set AZURE_LOCATION australiaeast
```

!!! Note "Azure Location"
    You can change `australiaeast` to any Azure region that supports AI Foundry. Common options include: `eastus`, `westus2`, `westeurope`, `southeastasia`.

### 3. Provision and Deploy

Deploy all required Azure resources using a single command:

```powershell
azd up
```

This command will:

- Provision Azure AI Foundry resources (AI Hub, AI Project)
- Deploy AI models.
- Configure authentication and permissions

!!! Warning "Deployment Time"
    The deployment process may take 10-15 minutes to complete. Please be patient while Azure provisions all resources.

### 4. Configure Environment Variables

After deployment completes, copy the generated environment variables:

```powershell
cp .azure/dev/.env .env
```

This will create a `.env` file with all necessary configuration values for running your labs. 

---

## Verify Deployment ✅

1. Navigate to the [Azure Portal](https://portal.azure.com)
2. Look for a resource group named `rg-aiagent-ws-dev` (or similar, based on your environment name)
3. Verify the following resources are created:
    - Azure AI Hub
    - Azure AI Project
    - AI models (e.g., GPT-4)

---
